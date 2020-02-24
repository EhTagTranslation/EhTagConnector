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
        private static readonly byte[] _HexChars = "0123456789ABCDEF".Select(c => (byte)c).ToArray();
        private const int UTF8_MAX_LEN = 6;
        private readonly static System.Text.Encoding _Encoding = new System.Text.UTF8Encoding(false, false);

        public static string NormalizeUri(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "";
            url = url.Trim();
            var uri = url.AsSpan();
            var bufroot = uri.Length * UTF8_MAX_LEN > 4096
                ? new byte[uri.Length * UTF8_MAX_LEN]
                : stackalloc byte[uri.Length * UTF8_MAX_LEN];
            var bufrem = bufroot;
            for (var i = 0; i < uri.Length; i++)
            {
                var ch = uri[i];
                if ("()".IndexOf(ch) >= 0 || char.IsWhiteSpace(ch) || char.IsControl(ch))
                {
                    // encode special chars
                    var l = encodeChar(uri.Slice(i, 1), bufrem);
                    bufrem = bufrem.Slice(l);
                }
                else if (ch == '%' && i + 2 < uri.Length && isHexChar(uri[i + 1]) && isHexChar(uri[i + 2]))
                {
                    // %xx format
                    var bc = byte.Parse(uri.Slice(i + 1, 2), System.Globalization.NumberStyles.HexNumber);
                    if (bc < 128 &&
                        ("\\\"!*'();:@&=+$,/?#[]".IndexOf((char)bc) >= 0
                        || char.IsControl((char)bc)
                        || char.IsWhiteSpace((char)bc)))
                    {
                        // DO NOT decode special chars, write its %xx format
                        var l = _Encoding.GetBytes(uri.Slice(i, 3), bufrem);
                        bufrem = bufrem.Slice(l);
                    }
                    else
                    {
                        // decode
                        bufrem[0] = bc;
                        bufrem = bufrem.Slice(1);
                    }
                    i += 2;
                }
                else
                {
                    var l = _Encoding.GetBytes(uri.Slice(i, 1), bufrem);
                    bufrem = bufrem.Slice(l);
                }
            }
            return _Encoding.GetString(bufroot.Slice(0, bufroot.Length - bufrem.Length));

            bool isHexChar(char ch)
            {
                return ('0' <= ch && ch <= '9')
                    || ('A' <= ch && ch <= 'F')
                    || ('a' <= ch && ch <= 'f');
            }

            int encodeChar(ReadOnlySpan<char> chars, Span<byte> bytes)
            {
                var chbytes = (Span<byte>)stackalloc byte[UTF8_MAX_LEN];
                var chlen = _Encoding.GetBytes(chars, chbytes);
                for (var i = 0; i < chlen; i++)
                {
                    var b = chbytes[i];
                    bytes[3 * i] = (byte)'%';
                    bytes[3 * i + 1] = _HexChars[b >> 4];
                    bytes[3 * i + 2] = _HexChars[b & 0x0F];
                }
                return chlen * 3;
            }
        }

        public static MarkdownDocument Normalize(MarkdownDocument doc)
        {
            foreach (var link in doc.Descendants().OfType<LinkInline>())
            {
                _NormalizeLink(link);
            }

            return doc;
        }

        private static readonly Dictionary<string, string> _KnownHosts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["moegirl.org"] = "萌娘百科",
            ["wikipedia.org"] = "维基百科",
            ["pixiv.net"] = "pixiv",
            ["instagram.com"] = "Instagram",
            ["facebook.com"] = "脸书",
            ["twitter.com"] = "Twitter",
            ["weibo.com"] = "微博",
        };

        private static void _NormalizeLink(LinkInline link)
        {
            var url = link.GetDynamicUrl?.Invoke() ?? link.Url;
            var title = link.Title;
            var nsfwmark = default(string);

            if (url != null && url.StartsWith("#") && !string.IsNullOrWhiteSpace(title))
            {
                // nsfw link
                nsfwmark = url;
                url = title;
            }

            var (furl, nsfw) = _FormatUrl(url);
            if (nsfw && nsfwmark == null)
            {
                nsfwmark = "#";
            }

            if (link.IsImage)
            {
                if (nsfwmark == null)
                {
                    link.Url = furl;
                }
                else
                {
                    link.Title = furl;
                    link.Url = nsfwmark;
                }
            }
            else
            {
                if (link.IsAutoLink
                    && link.FirstChild == link.LastChild && link.FirstChild is LiteralInline content
                    && Uri.TryCreate(url, UriKind.Absolute, out var purl))
                {
                    foreach (var item in _KnownHosts)
                    {
                        if (purl.Host.EndsWith(item.Key))
                        {
                            content.Content = new Markdig.Helpers.StringSlice(item.Value);
                        }
                    }
                }
                link.Url = furl;
            }
        }

        private static readonly Regex _ThumbUriRegex = new Regex(@"^(http|https)://(ehgt\.org(/t|)|exhentai\.org/t|ul\.ehgt\.org(/t|))/(.+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private static (string formatted, bool isNsfw) _FormatUrl(string url)
        {
            var thumbMatch = _ThumbUriRegex.Match(url);
            if (!thumbMatch.Success)
                return (NormalizeUri(url), false);

            var tail = thumbMatch.Groups[5].Value;
            var domain = thumbMatch.Groups[2].Value;

            var isNsfw = domain.StartsWith("exhentai");
            return ("https://ehgt.org/" + tail, isNsfw);
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
