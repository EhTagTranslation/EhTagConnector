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
    }
}
