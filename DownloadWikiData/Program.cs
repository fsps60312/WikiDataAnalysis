using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.BZip2;
using System.IO;
using System.Threading;

namespace DownloadWikiData
{
    partial class Program
    {
        const string titleListUrl = "https://dumps.wikimedia.org/zhwiki/latest/zhwiki-latest-pages-articles-multistream-index.txt.bz2";
        const string curlUrl = "https://zh.wikipedia.org/zh-tw/";//+"数学";
        //const string titleListUrl = "https://dumps.wikimedia.org/enwiki/latest/enwiki-latest-pages-articles-multistream-index.txt.bz2";
        //const string curlUrl = "https://en.wikipedia.org/wiki/";//+"数学";
        static async Task<List<string>>GetTitleList()
        {
            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(30);
                Console.WriteLine("Downloading...");
                var stream = new MemoryStream();
                BZip2.Decompress(await client.GetStreamAsync(titleListUrl), stream, true);
                Console.WriteLine("Done");
                using (var f = new FileStream("tmp.txt", FileMode.Create))
                {
                    var b = stream.ToArray();
                    f.Write(b, 0, b.Length);
                    f.Close();
                }
                var s = new StreamReader(new MemoryStream(stream.ToArray()), Encoding.UTF8).ReadToEnd().Split('\n').Select(v =>
                 {
                     return v.Substring(v.IndexOf(':', v.IndexOf(':') + 1) + 1);
                 }).ToArray();
                var blackList = new string[]
                {
                "Wikipedia:删除纪录/档案馆/2004年3月"
                };
                List<string> ans = new List<string>();
                foreach (var v in s) if (!blackList.Contains(v)) ans.Add(v);
                return ans;
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
                return null;
            }
        }
        static Random rand = new Random();
        static async void Run()
        {
            var retryList = await GetTitleList();
            using (StreamWriter writer = new StreamWriter("output.txt", false, Encoding.UTF8))
            {
                using (StreamWriter log = new StreamWriter("log.txt", false, Encoding.UTF8) {AutoFlush=true })
                {
                    object syncRootErrorWriter = new object();
                    int progress = 0;
                    int total_progress = retryList.Count;
                    Console.WriteLine($"Url Prefix: {curlUrl}");
                    while (retryList.Count > 0)
                    {
                        var s = retryList.ToList();
                        retryList.Clear();
                        int iterationCount = s.Count;// 1000;
                                                     //System.Threading.SemaphoreSlim semaphore = new System.Threading.SemaphoreSlim(10, 10);
                        Console.WriteLine($"Iteration Count: {iterationCount}");
                        int parallelism = 10;
                        SemaphoreSlim semaphoreSlim = new SemaphoreSlim(parallelism);
                        bool hasError = false;
                        bool finishing = false;
                        var parallelismAdjuster = new Thread(() =>
                         {
                             int goodCount = 0;
                             while (!finishing)
                             {
                                 Thread.Sleep(500);
                                 if (hasError)
                                 {
                                     hasError = false;
                                     goodCount = 0;
                                     if (parallelism > 1)
                                     {
                                         --parallelism;
                                         semaphoreSlim.Wait();
                                     }
                                 }
                                 else
                                 {
                                     ++goodCount;
                                     if (goodCount >= 4)
                                     {
                                         goodCount = 0;
                                         ++parallelism;
                                         lock (semaphoreSlim) semaphoreSlim.Release();
                                     }
                                 }
                             }
                             while(parallelism>0)
                             {
                                 --parallelism;
                                 semaphoreSlim.Wait();
                             }
                             Console.WriteLine("All jobs finished. Exiting parallelismAdjuster...");
                         });
                        parallelismAdjuster.Start();
                        Parallel.For(0, iterationCount,new ParallelOptions { MaxDegreeOfParallelism = 50 }, _i =>
                        {
                            //Console.WriteLine($"Url Prefix: {curlUrl}");
                            //int i = rand.Next(s.Count);
                            int i = _i;
                            var titleName = System.Net.WebUtility.UrlEncode(s[i].Replace(' ', '_'));
                            var url = curlUrl + titleName;
                            int paddingstringlength = iterationCount.ToString().Length * 2 + 1 + 3;
                            var GetPaddingString = new Func<string>(() => $"{iterationCount} {progress + retryList.Count}/{retryList.Count}-{parallelism}".PadRight(paddingstringlength) + " ");
                            int padding = 20;
                            {
                                var p = System.Threading.Interlocked.Increment(ref progress);
                                var __ = (GetPaddingString() + titleName.PadLeft(Console.WindowWidth - 1 - paddingstringlength));
                                if (__.Length > Console.WindowWidth - 1) __ = __.Remove(Console.WindowWidth - 1);
                                Console.Write(__ + "\r");
                                lock (log) log.WriteLine($"{DateTime.Now.ToString(DateTimeFormatString)}\t{p}/{iterationCount}\t{url}");
                            }
                            Console.Write(GetPaddingString() + $"Downloading...".PadRight(padding) + "\r");
                            try
                            {
                                //semaphore.Wait();
                                string webContent;
                                using (HttpClient client = new HttpClient())
                                {
                                    semaphoreSlim.Wait();
                                    try { webContent = client.GetStringAsync(url).Result; }
                                    finally { lock (semaphoreSlim) semaphoreSlim.Release(); }
                                }
                                Console.Write(GetPaddingString() + $"Processing... ({webContent.Length})".PadRight(padding) + "\r");
                                webContent = ResolveWebContent(webContent.Replace("<", " <"));
                                Console.Write(GetPaddingString() + $"Writing... ({webContent.Length})".PadRight(padding) + "\r");
                                lock (writer)
                                {
                                    writer.WriteLine($"\r\n===================={s[i]}====================\r\n");
                                    writer.WriteLine(webContent);
                                }
                                Console.Write(GetPaddingString() + $"Done ({webContent.Length})".PadRight(padding) + "\r");
                            }
                            catch (Exception _error)
                            {
                                hasError = true;
                                var GetStatusCode = new Func<string, int>(o =>
                                    {
                                        const string target = "Response status code does not indicate success: ";
                                        var _ = o.IndexOf(target);
                                        if (_ == -1) return -1;
                                        _ += target.Length;
                                        var __ = o.IndexOf(" ", _);
                                        if (__ != _ + 3) return -2;
                                        int ans;
                                        if (!int.TryParse(o.Substring(_, 3), out ans)) return -3;
                                        return ans;
                                    });
                                var RecordError = new Action<Exception>(o =>
                                {
                                    Console.WriteLine(url.PadRight(Console.WindowWidth - 1));
                                    Console.WriteLine(o);
                                    lock (syncRootErrorWriter)
                                    {
                                        using (StreamWriter w = new StreamWriter("error.txt", true, Encoding.UTF8))
                                        {
                                            w.WriteLine();
                                            w.WriteLine(s[i]);
                                            w.WriteLine(url);
                                            w.WriteLine(o);
                                            w.Close();
                                        }
                                    }
                                });
                                var errors = new List<Exception>();
                                Action<Exception> dfs = null;
                                dfs = new Action<Exception>(e =>
                                {
                                    if (e == null) return;
                                    dfs(e.InnerException);
                                    if (e is AggregateException)
                                    {
                                        foreach (var o in (e as AggregateException).InnerExceptions) dfs(o);
                                        return;
                                    }
                                    if (e != null) errors.Add(e);
                                });
                                dfs(_error);
                                foreach (var e in errors)
                                {
                                    if (e is HttpRequestException)
                                    {
                                        if (e.Message == "An error occurred while sending the request.")
                                        {
                                            System.Threading.Interlocked.Decrement(ref progress);
                                            lock (retryList) retryList.Add(s[i]);
                                            return;
                                        }
                                        var code = GetStatusCode(e.Message);
                                        if (code != 404)
                                        {
                                            if (code != 429 && code != 400 && code != 500)
                                            {
                                                Console.WriteLine($"Http Status Code: {code}".PadRight(Console.WindowWidth));
                                                //RecordError(e);
                                                RecordError(_error);
                                            }
                                            System.Threading.Interlocked.Decrement(ref progress);
                                            //System.Threading.Thread.Sleep(10000);
                                            lock (retryList) retryList.Add(s[i]);
                                            return;
                                        }
                                        else return;
                                    }
                                    else if (e is TaskCanceledException)
                                    {
                                        if (e.Message == "A task was canceled.")
                                        {
                                            System.Threading.Interlocked.Decrement(ref progress);
                                            lock (retryList) retryList.Add(s[i]);
                                            return;
                                        }
                                    }
                                }
                                RecordError(_error);
                            }
                            //finally { lock (semaphore) semaphore.Release(); }
                            //await Task.Delay(1000);
                        });
                        finishing = true;
                        parallelismAdjuster.Join();
                    }
                    log.Close();
                }
                writer.Close();
            }
            Console.WriteLine("All Done.");
        }
        static async void Run1()
        {
            HttpClient client = new HttpClient();
            Console.WriteLine("Downloading...");
            string webContent = await client.GetStringAsync(curlUrl);
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter("origin.html", false, Encoding.UTF8))
            {
                writer.WriteLine(webContent);
                writer.Close();
            }
            Console.WriteLine("Processing...");
            webContent = new Runner().Run(webContent);
            Console.WriteLine("Writing...");
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter("output.txt", false, Encoding.UTF8))
            {
                writer.WriteLine(webContent);
                writer.Close();
            }
            Console.WriteLine("Done");
            Console.WriteLine(webContent);
        }
        static async void Run2()
        {
            BEMSmodel bs = new BEMSmodel();
            await bs.DownloadDictionaryAsync();
            Console.ReadLine();
        }
        static Func<string, string> ResolveWebContent = new Func<string, string>(webContent => new Runner().Run(webContent));
        static void RefreshResolvingMethodFromSource(string code)
        {
            Console.WriteLine($"{DateTime.Now}\tCompiling Customized Runner Code...");
            var referenceAssemblies = new string[]
            {
                    "Microsoft.CSharp",
                    "System",
                    "System.Core",
                    "System.Data",
                    "System.Data.DataSetExtensions",
                    "System.Net.Http",
                    "System.Xml",
                    "System.Xml.Linq",
                    "Newtonsoft.Json"
            };
            System.Reflection.MethodInfo method = null;
            try { method = DynamicCompile.GetMethod(code, "DownloadWikiData", "StaticRunner", "Run", referenceAssemblies); }
            catch (Exception error) { Console.WriteLine(error.ToString()); return; }
            ResolveWebContent = new Func<string, string>(webContent => (string)method.Invoke(null, new object[] { webContent }));
            Console.WriteLine($"{DateTime.Now}\tCompilation OK");
        }
        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToString(DateTimeFormatString));
            const string runnerSourceCodeName = "Runner.cs";
            if (new FileInfo(runnerSourceCodeName).Exists)
            {
                Console.WriteLine($"Found {runnerSourceCodeName}. Start monitoring code changes... (Period: 10 secs)");
                var datetime = new FileInfo(runnerSourceCodeName).LastWriteTime;
                using (var reader = new StreamReader(runnerSourceCodeName)) RefreshResolvingMethodFromSource(reader.ReadToEnd());
                new Thread(() =>
                {
                    while(true)
                    {
                        Thread.Sleep(10000);
                        try
                        {
                            if (new FileInfo(runnerSourceCodeName).LastWriteTime != datetime)
                            {
                                Console.WriteLine($"Changes in {runnerSourceCodeName} detected.");
                                datetime = new FileInfo(runnerSourceCodeName).LastWriteTime;
                                using (var reader = new StreamReader(runnerSourceCodeName)) RefreshResolvingMethodFromSource(reader.ReadToEnd());
                            }
                        }
                        catch(Exception error)
                        {
                            using (var writer = new StreamWriter("monitor-log.txt",true,Encoding.UTF8))
                            {
                                writer.WriteLine($"=========={DateTime.Now.ToString(DateTimeFormatString)}==========");
                                writer.WriteLine(error.ToString());
                            }
                        }
                    }
                }).Start();
            }
            if (new FileInfo("tmp.html").Exists)
            {
                Console.WriteLine("Found test html file: tmp.html");
                string webContent;
                using (var reader = new StreamReader("tmp.html")) webContent = reader.ReadToEnd();
                webContent = ResolveWebContent(webContent.Replace("<", " <"));
                //webContent = new Runner().Run(webContent.Replace("<", " <"));
                Console.WriteLine("Extracted Content:");
                Console.WriteLine(webContent);
                Console.WriteLine("-----Suspended-----");
                Console.ReadLine();
            }
            try
            {
                Run();
            }
            catch(Exception error)
            {
                Console.WriteLine($"Unexpected Fatal Error, cannot recover\n{error}");
            }
            Console.ReadLine();
        }
    }
}
