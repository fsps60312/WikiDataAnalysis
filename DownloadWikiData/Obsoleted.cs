using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;

namespace DownloadWikiData
{
    class Obsoleted
    {
        const string titleListUrl = "https://dumps.wikimedia.org/zhwiki/20180501/zhwiki-20180501-pages-articles-multistream-index.txt.bz2";
        const string curlUrl = "https://zh.wikipedia.org/zh-tw/数学";
        static async void Run()
        {
            HttpClient client = new HttpClient();
            Console.WriteLine("Downloading...");
            string webContent = await client.GetStringAsync(curlUrl);
            //webContent = webContent.Substring(15);
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter("origin.html", false, Encoding.UTF8))
            {
                writer.WriteLine(webContent);
                writer.Close();
            }
            Console.WriteLine("Processing...");
            //webContent = RemoveTag(webContent, "table", false, "navbox");
            //webContent = RemoveTag(webContent, "script", false);
            //webContent = RemoveTag(webContent, "a", true);
            //webContent = RemoveTag(webContent, "img/", true);
            webContent = new Runner().Run(webContent);
            Console.WriteLine("Writing...");
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter("output.txt", false, Encoding.UTF8))
            {
                writer.WriteLine(webContent);
                writer.Close();
            }
            Console.WriteLine("Done");
            //await Task.Delay(1000);
            //webContent = SelectTag(webContent, new List<Tuple<string, string, bool>>
            //{
            //    new Tuple<string, string, bool>("table","navbox",false),
            //    new Tuple<string, string, bool>("p",null,true),
            //    new Tuple<string, string, bool>("div","thumbcaption",true),
            //    new Tuple<string, string, bool>("a",null,true),
            //    new Tuple<string, string, bool>("h1",null,false),
            //    new Tuple<string, string, bool>("h2",null,false),
            //    new Tuple<string, string, bool>("h3",null,false),
            //    new Tuple<string, string, bool>("h4",null,false),
            //    //new Tuple<string, string, bool>("img",null,false)
            //}, false);
            Console.WriteLine(webContent);
        }
        static bool TagContainsClass(string s, string cla)
        {
            Trace.Assert(s.StartsWith("<") && s.EndsWith(">"));
            return s.IndexOf($"class=\"{cla}\"") != -1;
        }
        static bool FindTag(string s, int i, string tag, ref int tagStartIndex, ref int tagEndIndex, ref string tagContent, string cla = null)
        {
            string tagStart = $"<{tag}", tagEnd = $"</{tag}>";
            if (tag.EndsWith("/"))
            {
                tagStart = $"<{tag}";
                tagEnd = "/>";
            }
            int j;
            if ((tagStartIndex = s.IndexOf(tagStart, i)) == -1 ||
                (j = s.IndexOf('>', tagStartIndex + tagStart.Length)) == -1 ||
                (tagEndIndex = s.IndexOf(tagEnd, j)) == -1) return false;
            j++;
            if (cla != null && !TagContainsClass(s.Substring(tagStartIndex, j - tagStartIndex), tag))
            {
                return FindTag(s, j, tag, ref tagStartIndex, ref tagEndIndex, ref tagContent, cla);
            }
            tagContent = s.Substring(j, tagEndIndex - j);
            tagEndIndex += tagEnd.Length;
            return true;
        }
        static int IndexOf(string s, IEnumerable<string> targets, ref string matched, int startIndex = 0)
        {
            for (int i = startIndex; i < s.Length; i++)
            {
                foreach (var t in targets)
                {
                    if (string.CompareOrdinal(s, i, t, 0, t.Length) == 0)
                    {
                        matched = t;
                        return i;
                    }
                }
            }
            return -1;
        }
        static Tuple<string, string, bool> FindTag(string s, int i, List<Tuple<string, string, bool>> tags, ref int tagStartIndex, ref int tagEndIndex, ref string tagContent)
        {
            int j;
            string matchedTag = null, tagEnd = null, foo = null;
            if ((tagStartIndex = IndexOf(s, tags.Select(v => $"<{v.Item1}"), ref matchedTag, i)) == -1 ||
                (j = s.IndexOf('>', tagStartIndex + matchedTag.Length)) == -1 ||
                (tagEndIndex = IndexOf(s, new List<string> { (tagEnd = $"</{matchedTag.Substring(1)}>"), $"<{matchedTag.Substring(1)}>" }, ref foo, j)) == -1) return null;
            j++;
            var tag = tags.Where(v => v.Item1 == matchedTag.Substring(1)).First();
            if (tag.Item2 != null && !TagContainsClass(s.Substring(tagStartIndex, j - tagStartIndex), tag.Item2))
            {
                return FindTag(s, j, tags, ref tagStartIndex, ref tagEndIndex, ref tagContent);
            }
            tagContent = s.Substring(j, tagEndIndex - j);
            tagEndIndex += tagEnd.Length;
            return tag;
        }
        static string SelectTag(string s, List<Tuple<string, string, bool>> tags, bool retainContent)
        {
            string ans = "";
            int pre = 0, tagStartIndex = -1, tagEndIndex = -1;
            string tagContent = null;
            for (Tuple<string, string, bool> result; (result = FindTag(s, pre, tags, ref tagStartIndex, ref tagEndIndex, ref tagContent)) != null;)
            {
                if (retainContent) ans += s.Substring(pre, tagStartIndex - pre);
                if (result.Item3) ans += SelectTag(tagContent, tags, true);
                pre = tagEndIndex;
            }
            if (retainContent) ans += s.Substring(pre, s.Length - pre);
            return ans;
        }
        static string RemoveTag(string s, string tag, bool retainContent, string cla = null)
        {
            string ans = "";
            int pre = 0, tagStartIndex = -1, tagEndIndex = -1;
            string tagContent = null;
            for (; FindTag(s, pre, tag, ref tagStartIndex, ref tagEndIndex, ref tagContent, cla);)
            {
                ans += s.Substring(pre, tagStartIndex - pre);
                if (retainContent) ans += RemoveTag(tagContent, tag, retainContent, cla);
                pre = tagEndIndex;
            }
            ans += s.Substring(pre, s.Length - pre);
            return ans;
        }
        /*
            curl https://zh.wikipedia.org/zh-tw/数学
            -<table class="navbox">
            +<p>
            +<div class="thumbcaption">
        */
    }
}
