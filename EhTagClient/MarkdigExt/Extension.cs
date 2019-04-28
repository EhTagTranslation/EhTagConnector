using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace EhTagClient.MarkdigExt
{
    internal static class Extension
    {
        private static readonly byte[] _HexChars = "0123456789ABCDEF".Select(c => (byte)c).ToArray();
        private const int UTF8_MAX_LEN = 6;

        public static string NormalizeUri(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "";
            url = url.Trim();
            var uri = url.AsSpan();
            var enc = System.Text.Encoding.UTF8;
            using (var buf = new MemoryStream(uri.Length * 2))
            {
                for (var i = 0; i < uri.Length; i++)
                {
                    var ch = uri[i];
                    if ("()".IndexOf(ch) >= 0 || char.IsWhiteSpace(ch) || char.IsControl(ch))
                    {
                        var b = (Span<byte>)stackalloc byte[3 * UTF8_MAX_LEN];
                        var l = encodeChar(uri.Slice(i, 1), b);
                        buf.Write(b.Slice(0, l));
                    }
                    else if (ch == '%' && i + 2 < uri.Length && isHexChar(uri[i + 1]) && isHexChar(uri[i + 2]))
                    {
                        var bc = byte.Parse(uri.Slice(i + 1, 2), System.Globalization.NumberStyles.HexNumber);
                        if (bc < 128 &&
                            ("\\\"!*'();:@&=+$,/?#[]".IndexOf((char)bc) >= 0
                            || char.IsControl((char)bc)
                            || char.IsWhiteSpace((char)bc)))
                        {
                            var b = (Span<byte>)stackalloc byte[3 * UTF8_MAX_LEN];
                            var l = enc.GetBytes(uri.Slice(i, 3), b);
                            buf.Write(b.Slice(0, l));
                        }
                        else
                        {
                            buf.WriteByte(bc);
                        }
                        i += 2;
                    }
                    else
                    {
                        var b = (Span<byte>)stackalloc byte[UTF8_MAX_LEN];
                        var l = enc.GetBytes(uri.Slice(i, 1), b);
                        buf.Write(b.Slice(0, l));
                    }
                }
                return enc.GetString(buf.GetBuffer(), 0, (int)buf.Length);
            }

            bool isHexChar(char ch)
            {
                return ('0' <= ch && ch <= '9')
                    || ('A' <= ch && ch <= 'F')
                    || ('a' <= ch && ch <= 'f');
            }

            int encodeChar(ReadOnlySpan<char> chars, Span<byte> bytes)
            {
                var chbytes = (Span<byte>)stackalloc byte[UTF8_MAX_LEN];
                var chlen = enc.GetBytes(chars, chbytes);
                for (var i = 0; i < chlen; i++)
                {
                    var b = chbytes[i];
                    bytes[3 * i] = (byte)'%';
                    bytes[3 * i + 1] = _HexChars[b >> 4];
                    bytes[3 * i + 2] = _HexChars[b & 0b00001111];
                }
                return chlen * 3;
            }
        }

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
                return (NormalizeUri(url), false);

            var tail = thumbMatch.Groups[5].Value;
            var domain = thumbMatch.Groups[2].Value;

            var isNsfw = domain.StartsWith("exhentai");
            return ("https://ehgt.org/" + tail, isNsfw);
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
