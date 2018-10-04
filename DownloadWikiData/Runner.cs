using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DownloadWikiData
{
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
        void TagEnd()
        {
            char c;
            string tagName = "";
            while ((c = Read()) != '>') tagName += c;
            tagName = tagName.Trim();
            //Console.WriteLine($"TagEnd:   {tagName}");
            for (int i = tags.Count - 1; ; i--)
            {
                if(i==-1)
                {
                    using (var w = new System.IO.StreamWriter("warning.txt", true, Encoding.UTF8))
                    {
                        w.WriteLine();
                        w.WriteLine("".PadRight(50, '|'));
                        w.WriteLine();
                        w.WriteLine(webContent);
                        w.WriteLine("==================================");
                        w.WriteLine($"Extra TagEnd: {tagName}");
                        w.WriteLine("==================================");
                        w.WriteLine($"len={webContent.Length},idx={contentIndex}");
                        w.WriteLine("==================================");
                        w.WriteLine(TrimLength(webContent.Remove(contentIndex + 1), 100, false));
                        w.WriteLine("==================================");
                        w.WriteLine(TrimLength(webContent.Substring(contentIndex), 100, true));
                        w.WriteLine("==================================");
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
        HashSet<string>ReadString()
        {
            char c = Read();
            if (c == '"') return ReadStringDQ();
            else if (c == '\'') return ReadStringQ();
            string s = "" + c;
            while ((c = Read()) != ' ') s += c;
            contentIndex--;
            return new HashSet<string>{ s };
        }
        void TagStart()
        {
            char c;
            string tagName = "";
            while (!new char[]{' ','>' }.Contains(c = Read())) tagName += c;
            contentIndex--;
            Tag tag = new Tag();
            tag.name = tagName = tagName.Trim();
            //Console.WriteLine($"TagStart: {tag.name}");
            while(true)
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
                    while (!new char[] {'=','/','>' }.Contains(c = Read())) pName += c;
                    pName = pName.Trim();
                    if (!string.IsNullOrWhiteSpace(pName))
                    {
                        tag.properties[pName] = (c == '=' ? ReadString() : null);
                        if(c=='/')
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
            while(true)
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
        List<Tag>Tags(string name)
        {
            List<Tag> ans = new List<Tag>();
            foreach (var t in tags) if (t.name == name) ans.Add(t);
            return ans;
        }
        HashSet<string>Attribute(List<Tag> ts,string attributeName)
        {
            HashSet<string> ans = new HashSet<string>();
            foreach (var t in ts) if (t.properties.ContainsKey(attributeName)) foreach (var v in t.properties[attributeName]) ans.Add(v);
            return ans;
        }
        bool cutOff = false;
        bool Contains<T>(HashSet<T>s,IEnumerable<T>t)
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
                else if(c=='}')
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
                    else if(c == '{' && StartWith("\\displaystyle"))
                    {
                        contentIndex += "\\displaystyle".Length;
                        InLatex();
                    }
                    else
                    {
                        if(Accepted())result.Append(c);
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
                    w.WriteLine(TrimLength(webContent.Remove(contentIndex + 1), 100, false));
                    w.WriteLine("==================================");
                    w.WriteLine(TrimLength(webContent.Substring(contentIndex), 100, true));
                    w.WriteLine("==================================");
                    //Console.ReadLine();
                }
            }
        }
        bool IsEmpty(char c) { return c == ' ' || c == '\n' || c == '\r'; }
        string RemoveExtraEmpties(string s)
        {
            StringBuilder ans = new StringBuilder();
            bool isempty = false;
            foreach(char c in s)
            {
                bool e = IsEmpty(c);
                if (!e || !isempty) ans.Append(e ? ' ' : c);
                isempty = e;
            }
            return ans.ToString();
        }
        bool IsCiteNote(string s,ref int i)
        {
            if (i + 1 >= s.Length||s[i] != '['||
                (!char.IsLetterOrDigit(s[i+1])&&s[i+1]!= '參'&&s[i+1]!= '註')) return false;
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
            for(int i=0;i<s.Length;i++)
            {
                if (!IsCiteNote(s, ref i)) ans.Append(s[i]);
            }
            return ans.ToString();
        }
        string Process(string s)
        {
            s = System.Net.WebUtility.HtmlDecode(s);
            s = RemoveExtraEmpties(s);
            s = RemoveCiteNotes(s);
            return s;
        }
        string RemoveScripts(string s)
        {
            StringBuilder ans = new StringBuilder();
            s = s.Replace("</flowprogressivescript>", "</script>");
            int pre = 0;
            while(true)
            {
                int i = s.IndexOf("<script>", pre);
                if (i == -1) break;
                ans.Append(s.Substring(pre, i - pre));
                int j = s.IndexOf("</script>", i);
                Trace.Assert(j != -1);
                pre = j + "</script>".Length;
            }
            ans.Append(s.Substring(pre, s.Length - pre));
            return ans.ToString();
        }
        public string Run(string _webContent)
        {
            webContent = RemoveScripts(_webContent);
            contentIndex = 0;
            result =new StringBuilder();
            cutOff = false;
            Dfs();
            //Console.WriteLine($"cutoff={cutOff}");
            return Process(result.ToString());
        }
    }
    class Runner1
    {
        bool Compare(string webContent,int i,string s)
        {
            return string.CompareOrdinal(webContent, i, s, 0, s.Length) == 0;
        }
        string IdentifyTagStart(string webContent,int i, Dictionary<string, List<Dictionary<string, string>>> parentTags)
        {
            foreach (var tag in parentTags)
            {
                if (Compare(webContent, i, $"<{tag.Key.TrimEnd('/')} "))
                {
                    return tag.Key;
                }
            }
            return null;
        }
        void Pop<T>(List<T> list) { list.RemoveAt(list.Count - 1); }
        bool IdentifyTagEnd(string webContent,ref int i,ref Dictionary<string, List<Dictionary<string, string>>> parentTags)
        {
            foreach (var tag in parentTags)
            {
                if (tag.Key.EndsWith("/")) continue;
                string symbol = $"<{tag.Key} />";
                if(Compare(webContent,i,symbol))
                {
                    i += tag.Key.Length;
                    i--;
                    Pop(parentTags[tag.Key]);
                    return true;
                }
            }
            return false;
        }
        bool ContainsAttribute(List<Dictionary<string, string>>tagsProperties,string key,string value)
        {
            foreach(var ps in tagsProperties)
            {
                if (ps.ContainsKey(key) && ps[key] == value) return true;
            }
            return false;
        }
        bool IsWanted(Dictionary<string, List<Dictionary<string, string>>> parentTags)
        {
            if (parentTags["script"].Count > 0) return false;
            if (ContainsAttribute(parentTags["table"], "class", "navbox")) return false;
            if (ContainsAttribute(parentTags["div"], "class", "thumbcaption")) return true;
            return true;
        }
        public string Run(string webContent)
        {
            Dictionary<string, List<Dictionary<string,string>>> parentTags = new Dictionary<string, List<Dictionary<string, string>>>
            {
                { "a", new List<Dictionary<string, string>>() },
                {"h1", new List<Dictionary<string, string>>() },
                {"h2", new List<Dictionary<string, string>>() },
                {"h3", new List<Dictionary<string, string>>() },
                {"h4", new List<Dictionary<string, string>>()},
                {"script", new List<Dictionary<string, string>>() },
                {"table", new List<Dictionary<string, string>>() },
                {"p", new List<Dictionary<string, string>>() },
                {"div", new List<Dictionary<string, string>>() },
                {"img/", new List<Dictionary<string, string>>() }
            };
            StringBuilder ans = new StringBuilder();
            for (int i=0;i<webContent.Length;i++)
            {
                if (!IdentifyTagEnd(webContent, ref i, ref parentTags))
                {
                    string tag = IdentifyTagStart(webContent, i, parentTags);
                    if (tag == null)
                    {
                        if (IsWanted(parentTags)) ans.Append(webContent[i]);
                    }
                    else
                    {

                    }
                }
            }
            return ans.ToString();
        }
    }
}
