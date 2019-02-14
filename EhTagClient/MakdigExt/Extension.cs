using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Linq;

namespace EhTagClient.MakdigExt
{
    static class Extension
    {
        public static MarkdownDocument Normalize(MarkdownDocument doc)
        {
            foreach (var link in doc.Descendants().OfType<LinkInline>())
            {
                var url = formatUrl(link.GetDynamicUrl?.Invoke() ?? link.Url);
                if (link.IsImage && !isSafeUrl(url))
                {
                    link.Title = url;
                    link.Url = "#";
                }
                else
                    link.Url = url;
            }

            return doc;

            string formatUrl(string u)
            {
                if (!u.StartsWith("http://"))
                    return u;

                if (u.StartsWith("http://exhentai.org"))
                    return u.Insert(4, "s");
                if (u.StartsWith("http://e-hentai.org"))
                    return u.Insert(4, "s");
                if (u.StartsWith("http://ul.ehgt.org"))
                    return u.Insert(4, "s");
                if (u.StartsWith("http://ehgt.org"))
                    return u.Insert(4, "s");
                return u;
            }

            bool isSafeUrl(string u)
            {
                if (u.StartsWith("https://exhentai.org"))
                    return false;

                return true;
            }
        }

        public static (string url, string title, bool isNsfw) GetData(this LinkInline link)
        {
            var url = formatUrl(link.GetDynamicUrl?.Invoke() ?? link.Url);

            if (link.IsImage && url == "#" && !string.IsNullOrEmpty(link.Title))
                return (link.Title, "", true);
            if (link.IsImage)
                return (url, link.Title, !isSafeUrl(url));

            return (url, link.Title, false);

            string formatUrl(string u)
            {
                if (!u.StartsWith("http://"))
                    return u;

                if (u.StartsWith("http://exhentai.org"))
                    return u.Insert(4, "s");
                if (u.StartsWith("http://e-hentai.org"))
                    return u.Insert(4, "s");
                if (u.StartsWith("http://ul.ehgt.org"))
                    return u.Insert(4, "s");
                if (u.StartsWith("http://ehgt.org"))
                    return u.Insert(4, "s");
                return u;
            }

            bool isSafeUrl(string u)
            {
                if (u.StartsWith("https://exhentai.org"))
                    return false;

                return true;
            }
        }
    }
}
