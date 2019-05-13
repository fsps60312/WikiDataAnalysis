using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace DownloadWikiData2
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
            if(!doc.HasElements)
            {
                XElement e = new XElement(doc.Name.LocalName);
                e.Value = doc.Value;
                return e;
            }
            return new XElement(doc.Name.LocalName, doc.Elements().Select(e => remove_all_namespaces(e)));
        }
        static string remove_all_namespaces(string xml)
        {
            var e = XElement.Parse(xml);
            return remove_all_namespaces(e).ToString(SaveOptions.None);
        }
        static XmlDocument LoadXmlWithoutNamespaces(string file_path)
        {
            string xmlText = null;
            Console.Write("loading");
            using (var reader = new System.IO.StreamReader(file_path, Encoding.UTF8)) xmlText = reader.ReadToEnd();
            Console.Write(".");
            xmlText = remove_all_namespaces(xmlText);
            Console.Write(".");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlText);
            Console.WriteLine("OK");
            return doc;
        }
        static void Main(string[] args)
        {
            if(args.Length!=2)
            {
                Console.Error.WriteLine($"usage: program.exe [input file name] [output file name]");
                Console.WriteLine("use test args? [y]");
                if (Console.ReadLine() != "y") return;
                args = new string[2] { @"C:\Users\fsps6\Desktop\Burney\downloader-zh 2019-3-18 (sentence)\output.txt", "output.txt" };
                Console.WriteLine("test args: " + string.Join(", ", args));
            }
            var doc = LoadXmlWithoutNamespaces(args[0]);
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
            Console.Write("selecting...");
            var texts = doc.SelectNodes(("mediawiki/page[ns='0']"));
            Console.WriteLine("OK");
            Console.WriteLine($"len(nodes)={texts.Count}");
            Console.WriteLine("checking...");
            var is_single_text_node = new Func<XmlNodeList, bool>(xmllist =>
               {
                   if (xmllist.Count != 1) return false;
                   var xml = xmllist.Item(0);
                   if (xml.ChildNodes.Count != 1) return false;
                   if (xml.FirstChild.GetType().Name.ToLower() != "xmltext") { Console.WriteLine(xml.FirstChild.GetType().Name); return false; }
                   return true;
               });
            using (var writer = new System.IO.StreamWriter(args[1], false, Encoding.UTF8))
            {
                int progress = 0, total_progress = texts.Count;
                foreach (var e in texts.Cast<XmlNode>())
                {
                    if (!(is_single_text_node(e.SelectNodes("title")) && is_single_text_node(e.SelectNodes("revision/text"))))
                    {
                        Console.WriteLine($"{e.SelectNodes("title").Count}, {e.SelectSingleNode("title").HasChildNodes}, {e.SelectNodes("revision/text").Count}, {e.SelectSingleNode("revision/text").HasChildNodes}");
                        Console.ReadLine();
                    }
                    Console.Write($"progress: {++progress} / {total_progress}\r");
                    writer.WriteLine();
                    writer.WriteLine(new string('=', 20) + e.SelectSingleNode("title").InnerXml + new string('=', 20));
                    writer.WriteLine(e.SelectSingleNode("revision/text").InnerXml);
                }
            }
            Console.WriteLine($"OK. Saved as {args[1]}.");
            for (int i = 0; i < texts.Count && Console.ReadLine() != null; i++)
            {
                var e = texts.Item(i);
                Console.WriteLine($"title: {e.SelectSingleNode("title").InnerXml}");
                Console.WriteLine(e.SelectSingleNode("revision/text").InnerXml);
                Console.WriteLine("press ENTER to continue...");
            }
        }
    }
}
