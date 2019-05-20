using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace DownloadWikiData2_Core
{
    class Program
    {
        //static XmlNamespaceManager get_all_namespaces(XmlDocument _xmlDocument)
        //{
        //    XmlNodeList _xmlNameSpaceList = _xmlDocument.SelectNodes(@"//namespace::*[not(. = ../../namespace::*)]");

        //    var _xmlNSmgr = new XmlNamespaceManager(_xmlDocument.NameTable);

        //    foreach (XmlNode nsNode in _xmlNameSpaceList)
        //    {
        //        _xmlNSmgr.AddNamespace(nsNode.LocalName, nsNode.Value);
        //    }
        //    return _xmlNSmgr;
        //}
        static string ignore_namespaces(string xpath_query)
        {
            return string.Join("/", xpath_query.Split('/').Select(n => n == "*" ? "*" : $"*[local-name()='{n}']"));
        }
        static XElement remove_all_namespaces(XElement doc)
        {
            if (!doc.HasElements)
            {
                XElement e = new XElement(doc.Name.LocalName);
                e.Value = doc.Value;
                return e;
            }
            return new XElement(doc.Name.LocalName, doc.Elements().Select(e => remove_all_namespaces(e)));
        }
        static void remove_all_namespaces(Stream istream,Stream ostream)
        {
            var e = XElement.Load(istream);
            remove_all_namespaces(e).Save(ostream);
        }
        static XmlDocument LoadXmlWithoutNamespaces()
        {
            const string
                tmp_file_name1 = "temporary_cache_file1",
                tmp_file_name2 = "temporary_cache_file2";
            try
            {
                Console.Error.Write("loading");
                using (var writer = new StreamWriter(tmp_file_name1, false, Encoding.UTF8))
                {
                    char[] buffer = new char[1 << 20];
                    for (int len; (len = Console.In.ReadBlock(buffer, 0, buffer.Length)) != 0;) writer.Write(buffer, 0, len);
                }
                Console.Error.Write(".");
                using (var istream = new FileStream(tmp_file_name1, FileMode.Open))
                {
                    using (var ostream = new FileStream(tmp_file_name2, FileMode.CreateNew))
                    {
                        remove_all_namespaces(istream, ostream);
                    }
                }
                Console.Error.Write(".");
                XmlDocument doc = new XmlDocument();
                doc.Load(tmp_file_name2);
                Console.Error.WriteLine("OK");
                return doc;
            }
            finally
            {
                File.Delete(tmp_file_name1);
                File.Delete(tmp_file_name2);
            }
        }
        static void Main(string[] args)
        {
            try
            {
                //if (args.Length != 2)
                //{
                //    Console.Error.WriteLine($"usage: program.exe [input file name] [output file name]");
                //    Console.WriteLine("use test args? [y]");
                //    if (Console.ReadLine() != "y") return;
                //    args = new string[2] { @"C:\Users\fsps6\Desktop\Burney\downloader-zh 2019-3-18 (sentence)\output.txt", "output.txt" };
                //    Console.WriteLine("test args: " + string.Join(", ", args));
                //}
                Console.Error.WriteLine("reading...");
                var doc = LoadXmlWithoutNamespaces();
                //while (true)
                //{
                //    try
                //    {
                //        var exp = Console.ReadLine();
                //        var texts = doc.SelectNodes(exp);
                //        Console.WriteLine($"len(nodes)={texts.Count}");
                //        Console.WriteLine(string.Join("\n", texts.Cast<XmlNode>().Take(100).Select(v => $"{v.LocalName}\t{string.Join("",v.InnerXml.Take(100))}")));
                //    }
                //    catch (Exception error) { Console.WriteLine(error.ToString()); }
                //}
                Console.Error.Write("selecting...");
                var texts = doc.SelectNodes(("mediawiki/page[ns='0']"));
                Console.Error.WriteLine("OK");
                Console.Error.WriteLine($"len(nodes)={texts.Count}");
                Console.Error.WriteLine("checking...");
                var is_single_text_node = new Func<XmlNodeList, bool>(xmllist =>
                {
                    if (xmllist.Count != 1) return false;
                    var xml = xmllist.Item(0);
                    if (xml.ChildNodes.Count > 1) return false;
                    if (xml.ChildNodes.Count != 0 && xml.FirstChild.GetType().Name.ToLower() != "xmltext") { Console.Error.WriteLine(xml.FirstChild.GetType().Name); return false; }
                    return true;
                });
                using (var writer = Console.Out)
                {
                    int progress = 0, total_progress = texts.Count;
                    foreach (var e in texts.Cast<XmlNode>())
                    {
                        if (!(is_single_text_node(e.SelectNodes("title")) && is_single_text_node(e.SelectNodes("revision/text"))))
                        {
                            var t = e.SelectSingleNode("title");
                            var r = e.SelectSingleNode("revision/text");
                            Console.Error.WriteLine($"{e.SelectNodes("title").Count}, {t.HasChildNodes}, {e.SelectNodes("revision/text").Count}, {r.HasChildNodes}");
                            if (t!=null)
                            {
                                Console.Error.WriteLine("title:");
                                Console.Error.WriteLine(t.FirstChild.GetType().Name);
                                Console.Error.WriteLine(t.InnerXml);
                            }
                            if (r != null)
                            {
                                Console.Error.WriteLine("revision/text:");
                                Console.Error.WriteLine(r.FirstChild.GetType().Name);
                                Console.Error.WriteLine(r.InnerXml);
                            }
                            throw new Exception("title isn't text, or revision/text isn't text");
                        }
                        Console.Error.Write($"progress: {++progress} / {total_progress}\r");
                        writer.WriteLine();
                        writer.WriteLine(new string('=', 20) + e.SelectSingleNode("title").InnerXml + new string('=', 20));
                        writer.WriteLine(e.SelectSingleNode("revision/text").InnerXml);
                    }
                }
                Console.Error.WriteLine($"OK.");
                //for (int i = 0; i < texts.Count && Console.ReadLine() != null; i++)
                //{
                //    var e = texts.Item(i);
                //    Console.WriteLine($"title: {e.SelectSingleNode("title").InnerXml}");
                //    Console.WriteLine(e.SelectSingleNode("revision/text").InnerXml);
                //    Console.WriteLine("press ENTER to continue...");
                //}
            }
            catch(Exception error)
            {
                Console.Error.WriteLine($"Exception:\n{error}");
                throw error;
            }
        }
    }
}
