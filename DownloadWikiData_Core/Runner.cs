﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace DownloadWikiData_Core
{
    partial class Program
    {
        public const string DateTimeFormatString = "yyyy-MM-dd HH-mm-ss.FFFFFFF";
    }
    class Runner
    {
        class Tag
        {
            public string name;
            public Dictionary<string, HashSet<string>> properties = new Dictionary<string, HashSet<string>>();
        }
        string webContent;
        int contentIndex;
        StringBuilder result;
        char Read() { return webContent[contentIndex++]; }
        List<Tag> tags = new List<Tag>();
        static object syncRootWarningFile = new object();
        static System.IO.StreamWriter warningWriter = null;
        void TagEnd()
        {
            char c;
            string tagName = "";
            while ((c = Read()) != '>') tagName += c;
            tagName = tagName.Trim();
            //Console.WriteLine($"{string.Concat(Enumerable.Repeat("| ", Math.Max(0, tags.Count - 1)))}TagEnd:   {tagName} ({contentIndex})");
            for (int i = tags.Count - 1; ; i--)
            {
                if (i == -1)
                {
                    lock (syncRootWarningFile)
                    {
                        if (warningWriter == null) warningWriter = new System.IO.StreamWriter("warning.txt", true, Encoding.UTF8);
                        warningWriter.WriteLine();
                        warningWriter.WriteLine("".PadRight(50, '|'));
                        warningWriter.WriteLine();
                        warningWriter.WriteLine(webContent);
                        warningWriter.WriteLine("==================================");
                        warningWriter.WriteLine($"Extra TagEnd: {tagName}");
                        warningWriter.WriteLine("==================================");
                        warningWriter.WriteLine($"len={webContent.Length},idx={contentIndex}");
                        warningWriter.WriteLine("==================================");
                        warningWriter.WriteLine(TrimLength(webContent.Remove(contentIndex + 1), 100, false));
                        warningWriter.WriteLine("==================================");
                        warningWriter.WriteLine(TrimLength(webContent.Substring(contentIndex), 100, true));
                        warningWriter.WriteLine("==================================");
                        warningWriter.Flush();
                    }
                    return;
                }
                if (tags[i].name == tagName)
                {
                    tags.RemoveRange(i, tags.Count - i);
                    return;
                }
            }
        }
        /// <summary>
        /// Read a double-quoted string and return HashSet(s.split(' '))
        /// </summary>
        /// <returns></returns>
        HashSet<string> ReadStringDQ()
        {
            string s = "";
            while (true)
            {
                char c = Read();
                if (c == '\\')
                {
                    s += c;
                    //s += (c = Read());//in HTML should use &quot;
                }
                else if (c == '"')
                {
                    var ans = new HashSet<string>(s.Split(' '));
                    ans.Remove("");
                    return ans;
                }
                else s += c;
            }
        }
        HashSet<string> ReadStringQ()
        {
            string s = "";
            while (true)
            {
                char c = Read();
                if (c == '\\')
                {
                    s += c;
                    //s += (c = Read());//in HTML should use &quot;
                }
                else if (c == '\'')
                {
                    var ans = new HashSet<string>(s.Split(' '));
                    ans.Remove("");
                    return ans;
                }
                else s += c;
            }
        }
        HashSet<string> ReadString()
        {
            char c = Read();
            if (c == '"') return ReadStringDQ();
            else if (c == '\'') return ReadStringQ();
            string s = "" + c;
            while ((c = Read()) != ' ') s += c;
            contentIndex--;
            return new HashSet<string> { s };
        }
        void TagStart()
        {
            char c;
            string tagName = "";
            while (!new char[] { ' ', '>' }.Contains(c = Read())) tagName += c;
            contentIndex--;
            Tag tag = new Tag();
            tag.name = tagName = tagName.Trim();
            //Console.WriteLine($"{string.Concat(Enumerable.Repeat("| ", tags.Count))}TagStart: {tag.name} ({contentIndex})");
            while (true)
            {
                c = Read();
                if (c == '>')
                {
                    tags.Add(tag);
                    return;
                }
                else
                {
                    contentIndex--;
                    string pName = "";
                    while (!new char[] { '=', '/', '>' }.Contains(c = Read()))
                    {
                        pName += c;
                        if (c == '"')
                        {
                            do { pName += (c = Read()); } while (c != '"');
                            break;
                        }
                    }
                    pName = pName.Trim();
                    if (!string.IsNullOrWhiteSpace(pName))
                    {
                        tag.properties[pName] = (c == '=' ? ReadString() : null);
                        if (c == '/')
                        {
                            //Console.WriteLine("Self-ended");
                            Trace.Assert((c = Read()) == '>');
                            if (c != '>')
                            {
                                Console.WriteLine($"tagName={tagName}");
                                throw new Exception();
                            }
                            //if (tag.name == "br") result.Append("\r\n");
                            return;
                        }
                    }
                    if (c == '>') contentIndex--;
                }
            }
        }
        void Comment()
        {
            int dashed = 0;
            char c;
            while (true)
            {
                c = Read();
                if (c == '-') dashed++;
                else
                {
                    if (c == '>' && dashed >= 2) return;
                    dashed = 0;
                }
            }
        }
        void InTag()
        {
            char c = Read();
            if (c == '/') TagEnd();
            else if (c == '!')
            {
                c = Read();
                if (c == 'D')
                {
                    contentIndex--;
                    TagStart();
                }
                else
                {
                    Trace.Assert(c == '-');
                    Trace.Assert((c = Read()) == '-');
                    Comment();
                }
            }
            else
            {
                contentIndex--;
                TagStart();
            }
        }
        string TrimLength(string s, int maxLength, bool begin)
        {
            if (s.Length <= maxLength) return s;
            if (begin) return s.Remove(maxLength);
            return s.Substring(s.Length - maxLength);
        }
        List<Tag> Tags(string name)
        {
            List<Tag> ans = new List<Tag>();
            foreach (var t in tags) if (t.name == name) ans.Add(t);
            return ans;
        }
        HashSet<string> Attribute(List<Tag> ts, string attributeName)
        {
            HashSet<string> ans = new HashSet<string>();
            foreach (var t in ts) if (t.properties.ContainsKey(attributeName)) foreach (var v in t.properties[attributeName]) ans.Add(v);
            return ans;
        }
        /// <summary>
        /// Internal use. Whether to terminate immediately
        /// </summary>
        private bool cutOff = false;
        bool Contains<T>(HashSet<T> s, IEnumerable<T> t)
        {
            foreach (var v in t) if (s.Contains(v)) return true;
            return false;
        }
        static HashSet<string> SpanIdBlockList = new HashSet<string>
        {
            "参考资料和註釋","參考資料及註釋","註記和參考資料",
            "参考文獻","参考文献","參考文献", "參考文獻",
            "参考资料","参考資料", "參考資料",
            "参考书目", "参考网址","参考註釋","参考注释",
            "參考來源","参考来源",
            "資料來源",
            "參考","参考",
            "参见", "參見","参看",
            "外部連結", "外部链接",
            "扩展阅读", "延伸阅读",
            "註釋","注釋", "注释",
            "脚注","腳註","注腳","註腳",
            "註記","註解",
            "文獻","備註",
            "Notes"
        };
        bool Accepted()
        {
            var divClasses = Attribute(Tags("div"), "class");
            if (!divClasses.Contains("mw-parser-output")) return false;
            if (Contains(Attribute(Tags("span"), "id"), SpanIdBlockList))
            {
                cutOff = true;
                return false;
            }
            if (Contains(divClasses, new string[] { "notice", "toc", "rellink", "relarticle", "mainarticle" })) return false;
            if (Contains(Attribute(tags, "class"), new string[] { "plainlinks", "citation", "wikicite" })) return false;
            if (Contains(Attribute(Tags("ol"), "class"), new string[] { "references" })) return false;
            if (Tags("h1").Count > 0) return false;
            if (Tags("h2").Count > 0) return false;
            if (Tags("h3").Count > 0) return false;
            if (Tags("h4").Count > 0) return false;
            if (Tags("h5").Count > 0) return false;
            if (Tags("h6").Count > 0) return false;
            if (Contains(Attribute(Tags("ul"), "class"), new string[] { "gallery", "mw-gallery-traditional" })) return false;
            if (Contains(Attribute(Tags("table"), "class"), new string[] { "vertical-navbox", "navbox", "plainlinks" })) return false;
            //if (Tags("script").Count > 0) return false;
            if (Tags("title").Count > 0) return false;
            if (Tags("style").Count > 0) return false;
            if (Tags("table").Count > 0) return false;
            return true;
            //if (parentTags["script"].Count > 0) return false;
            //if (ContainsAttribute(parentTags["table"], "class", "navbox")) return false;
            //if (ContainsAttribute(parentTags["div"], "class", "thumbcaption")) return true;
            //return true;
        }
        bool StartWith(string pattern)
        {
            if (contentIndex + pattern.Length > webContent.Length) return false;
            return string.CompareOrdinal(webContent, contentIndex, pattern, 0, pattern.Length) == 0;
        }
        void InLatex()
        {
            int depth = 1;
            while (true)
            {
                char c = Read();
                if (c == '\\')
                {
                    c = Read();
                }
                else if (c == '{') depth++;
                else if (c == '}')
                {
                    if (--depth == 0) return;
                }
            }
        }
        void Dfs()
        {
            try
            {
                while (contentIndex < webContent.Length)
                {
                    char c = Read();
                    if (c == '<') InTag();
                    else if (c == '{' && StartWith("\\displaystyle"))
                    {
                        contentIndex += "\\displaystyle".Length;
                        InLatex();
                    }
                    else
                    {
                        if (Accepted()) result.Append(c);
                        if (cutOff) return;
                    }
                }
            }
            catch (Exception error)
            {
                var directoryName = DateTime.Now.ToString(Program.DateTimeFormatString);
                while (System.IO.Directory.Exists(directoryName)) directoryName += "-";
                System.IO.Directory.CreateDirectory(directoryName);
                using (var w = new System.IO.StreamWriter(directoryName + "\\content.html", true, Encoding.UTF8))
                {
                    w.WriteLine(webContent);
                }
                using (var w = new System.IO.StreamWriter(directoryName + "\\error.txt", true, Encoding.UTF8))
                {
                    w.WriteLine(error.ToString());
                    w.WriteLine("==================================");
                    w.WriteLine($"len={webContent.Length},idx={contentIndex}");
                    w.WriteLine("==================================");
                    if (0 <= contentIndex && contentIndex < webContent.Length)
                    {
                        w.WriteLine(TrimLength(contentIndex + 1 == webContent.Length ? webContent : webContent.Remove(contentIndex + 1), 100, false));
                        w.WriteLine("==================================");
                        w.WriteLine(TrimLength(webContent.Substring(contentIndex), 100, true));
                        w.WriteLine("==================================");
                    }
                    //Console.ReadLine();
                }
            }
        }
        bool IsEmpty(char c) { return c == ' ' || c == '\n' || c == '\r'; }
        string RemoveExtraEmpties(string s)
        {
            StringBuilder ans = new StringBuilder();
            bool isempty = false;
            foreach (char c in s)
            {
                bool e = IsEmpty(c);
                if (!e || !isempty) ans.Append(e ? ' ' : c);
                isempty = e;
            }
            return ans.ToString();
        }
        string ForceNewlineOnTheEndOfParagraph(string s, string indicator)
        {
            return s
                .Replace("</p>", $"{indicator}</p>")
                .Replace("<br", $"{indicator}<br")
                .Replace("</li>", $"{indicator}</li>")
                .Replace("</div>", $"{indicator}</div>")
                .Replace("</dt>", $"{indicator}</dt>")
                .Replace("</dd>", $"{indicator}</dd>");
        }
        bool IsCiteNote(string s, ref int i)
        {
            if (i + 1 >= s.Length || s[i] != '[' ||
                (!char.IsLetterOrDigit(s[i + 1]) && s[i + 1] != '參' && s[i + 1] != '註')) return false;
            int j = i + 2;
            while (char.IsWhiteSpace(s[j])) j++;
            while (char.IsDigit(s[j])) j++;
            if (s[j] == ']')
            {
                i = j;
                return true;
            }
            else return false;
        }
        string RemoveCiteNotes(string s)
        {
            StringBuilder ans = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (!IsCiteNote(s, ref i)) ans.Append(s[i]);
            }
            return ans.ToString();
        }
        string RemoveScripts(string s)
        {
            StringBuilder ans = new StringBuilder();
            s = s.Replace("</flowprogressivescript>", "</script>");
            int pre = 0;
            while (true)
            {
                int i = s.IndexOf("<script", pre);
                if (i == -1) break;
                ans.Append(s.Substring(pre, i - pre));
                int j = s.IndexOf("</script>", i);
                Trace.Assert(j != -1);
                pre = j + "</script>".Length;
            }
            ans.Append(s.Substring(pre, s.Length - pre));
            return ans.ToString();
        }
        string Process(string s)
        {
            s = System.Net.WebUtility.HtmlDecode(s);
            s = RemoveExtraEmpties(s);
            s = RemoveCiteNotes(s);
            return s;
        }
        string RemoveEmptyLines(string s)
        {
            string ans = string.Join("\n", s.Split('\n').Select(v => v.Trim()));
            do
            {
                s = ans;
                ans = s.Replace("\n\n", "\n");
            } while (ans != s);
            return ans;
        }
        public string Run(string _webContent)
        {
            const string newLineIndicator = "AAAAAAAAAAZZZZZZZZZZ";
            webContent = _webContent;
            webContent = RemoveScripts(webContent);
            webContent = ForceNewlineOnTheEndOfParagraph(webContent, newLineIndicator);
            contentIndex = 0;
            result = new StringBuilder();
            cutOff = false;
            Dfs();
            //Console.WriteLine($"cutoff={cutOff}");
            return RemoveEmptyLines(Process(result.ToString()).Replace(newLineIndicator, "\n"));
        }
    }
}
