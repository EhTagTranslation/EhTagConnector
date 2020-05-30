using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace EhTagClient.MarkdigExt
{
    internal static class Extension
    {

        public static MarkdownDocument Normalize(MarkdownDocument doc)
        {
            foreach (var link in doc.Descendants().OfType<LinkInline>())
            {
                Normailze.LinkNormailizer.Normalize(link);
            }

            return doc;
        }

        public static (string url, string title, string isNsfw) GetData(this LinkInline link)
        {
            var url = (link.GetDynamicUrl?.Invoke() ?? link.Url).Trim();
            var title = (link.Title ?? "").Trim();

            if (link.IsImage && url.StartsWith("#") && !string.IsNullOrEmpty(title))
            {
                if (url == "#")
                    return (title, "", "R18");
                else if (url == "##")
                    return (title, "", "R18G");
                else
                    return (title, "", url.Substring(1).Trim());
            }

            return (url, title, null);
        }
        public static string GetTagName(string tag, Namespace ns)
        {
            var table = Context.Database[ns];
            var record = table.Find(tag, true);
            if (record == null)
                return null;
            if (record.Name.Text == null)
                record.Name.Render();
            return record.Name.Text;
        }

        public static string GetTagName(string tag)
        {
            var record = GetTagName(tag, Context.Namespace);
            if (record != null) return record;
            foreach (var item in Context.Database.Keys)
            {
                if (item != Context.Namespace)
                {
                    record = GetTagName(tag, item);
                    if (record != null) return record;
                }
            }
            Console.WriteLine($"Invalid tag ref {tag}");
            return null;
        }
    }
}
