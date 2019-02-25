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
    class Program
    {
        const string titleListUrl = "https://dumps.wikimedia.org/zhwiki/latest/zhwiki-latest-pages-articles-multistream-index.txt.bz2";
        const string curlUrl = "https://zh.wikipedia.org/zh-tw/";//+"数学";
        //const string titleListUrl = "https://dumps.wikimedia.org/enwiki/20190201/enwiki-20190201-pages-articles-multistream-index.txt.bz2";
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
                        int goodCount = 0;
                        const int parallelIncThreshHold = 20;
                        for(int _i=0;_i<iterationCount;_i++)
                        {
                            //Console.WriteLine($"Url Prefix: {curlUrl}");
                            //int i = rand.Next(s.Count);
                            int i = _i;
                            await semaphoreSlim.WaitAsync();
                            new Thread(() =>
                            {
                                try
                                {
                                    var titleName = System.Net.WebUtility.UrlEncode(s[i].Replace(' ', '_'));
                                    var url = curlUrl + titleName;
                                    int paddingstringlength = iterationCount.ToString().Length * 2 + 1+3;
                                    var GetPaddingString = new Func<string>(() => $"{progress}/{retryList.Count}-{parallelism}".PadRight(paddingstringlength) + " ");
                                    int padding = 20;
                                    {
                                        var p = System.Threading.Interlocked.Increment(ref progress);
                                        var __ = (GetPaddingString() + titleName.PadLeft(Console.WindowWidth - 1 - paddingstringlength));
                                        if (__.Length > Console.WindowWidth - 1) __ = __.Remove(Console.WindowWidth - 1);
                                        Console.Write(__ + "\r");
                                        lock (log) log.WriteLine($"{p}/{iterationCount}\t{url}");
                                    }
                                    Console.Write(GetPaddingString() + $"Downloading...".PadRight(padding) + "\r");
                                    try
                                    {
                                        //semaphore.Wait();
                                        string webContent;
                                        using (HttpClient client = new HttpClient())
                                        {
                                            webContent = client.GetStringAsync(url).Result;
                                        }
                                        Console.Write(GetPaddingString() + $"Processing... ({webContent.Length})".PadRight(padding) + "\r");
                                        webContent = new Runner().Run(webContent.Replace("<", " <"));
                                        Console.Write(GetPaddingString() + $"Writing... ({webContent.Length})".PadRight(padding) + "\r");
                                        lock (writer)
                                        {
                                            writer.WriteLine($"\r\n===================={s[i]}====================\r\n");
                                            writer.WriteLine(webContent);
                                        }
                                        Console.Write(GetPaddingString() + $"Done ({webContent.Length})".PadRight(padding) + "\r");
                                        // seems OK, increase parallelism
                                        if (Interlocked.Increment(ref goodCount) >= parallelIncThreshHold)
                                        {
                                            goodCount = 0;
                                            if (parallelism < 50)
                                            {
                                                Interlocked.Increment(ref parallelism);
                                                lock (semaphoreSlim) semaphoreSlim.Release();
                                            }
                                        }
                                    }
                                    catch (Exception _error)
                                    {
                                        {//decrease parallelism
                                            goodCount = 0;
                                            if (Interlocked.Decrement(ref parallelism) <= 0) Interlocked.Increment(ref parallelism);
                                            else semaphoreSlim.Wait();
                                        }
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
                                              using (StreamWriter w = new StreamWriter("error.txt", true, Encoding.UTF8))
                                              {
                                                  w.WriteLine();
                                                  w.WriteLine(s[i]);
                                                  w.WriteLine(url);
                                                  w.WriteLine(o);
                                                  w.Close();
                                              }
                                          });
                                        var errors = new[] { _error };
                                        while (errors.Any(e => e is AggregateException))
                                        {
                                            errors = errors.SelectMany(e => e is AggregateException ? (e as AggregateException).InnerExceptions.ToArray() : new[] { e }).ToArray();
                                        }
                                        foreach (var e in errors)
                                        {
                                            if (e is HttpRequestException)
                                            {
                                                if (e.Message == "An error occurred while sending the request.")
                                                {
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
                                            }
                                            else if (e is TaskCanceledException)
                                            {
                                                if (e.Message == "A task was canceled.")
                                                {
                                                    lock (retryList) retryList.Add(s[i]);
                                                    return;
                                                }
                                            }
                                        }
                                        RecordError(_error);
                                    }
                                }
                                finally { lock (semaphoreSlim) semaphoreSlim.Release(); }
                            }).Start();
                            //finally { lock (semaphore) semaphore.Release(); }
                            //await Task.Delay(1000);
                        }
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
        public const string DateTimeFormatString = "yyyy-MM-dd HH-mm-ss.FFFFFFF";
        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToString(DateTimeFormatString));
            {
                //var webContent = new StreamReader("content.html").ReadToEnd();
                //webContent = new Runner().Run(webContent.Replace("<", " <"));
                //Console.WriteLine(webContent);
                //Console.WriteLine("-----Suspended-----");
                //Console.ReadLine();
            }
            Run();
            Console.ReadLine();
        }
    }
}
