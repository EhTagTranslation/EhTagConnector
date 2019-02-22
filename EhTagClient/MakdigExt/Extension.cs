using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Linq;
using System.Text.RegularExpressions;

namespace EhTagClient.MakdigExt
{
    internal static class Extension
    {
        public static MarkdownDocument Normalize(MarkdownDocument doc)
        {
            foreach (var link in doc.Descendants().OfType<LinkInline>())
            {
                var url = link.GetDynamicUrl?.Invoke() ?? link.Url;
                var title = link.Title;
                var (furl, nsfw) = _FormatUrl(url);
                if (link.IsImage && nsfw)
                {
                    link.Title = furl;
                    link.Url = "#";
                }
                else if (link.IsImage && url == "#" && !string.IsNullOrEmpty(title))
                {
                    (link.Title, _) = _FormatUrl(title);
                    link.Url = "#";
                }
                else
                {
                    link.Url = furl;
                }
            }

            return doc;
        }

        private static readonly Regex _ThumbUriRegex = new Regex(@"^(http|https)://(ehgt\.org(/t|)|exhentai\.org/t|ul\.ehgt\.org(/t|))/(.+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private static (string formatted, bool isNsfw) _FormatUrl(string url)
        {
            var thumbMatch = _ThumbUriRegex.Match(url);
            if (!thumbMatch.Success)
                return (url, false);

            var tail = thumbMatch.Groups[5].Value;
            var domain = thumbMatch.Groups[2].Value;

            var isNsfw = domain.StartsWith("exhentai");
            return ("https://ul.ehgt.org/" + tail, isNsfw);
        }

        public static (string url, string title, bool isNsfw) GetData(this LinkInline link)
        {
            var url = link.GetDynamicUrl?.Invoke() ?? link.Url;

            if (link.IsImage && url == "#" && !string.IsNullOrEmpty(link.Title))
                return (link.Title, "", true);

            return (url, link.Title, false);
        }
    }
}
