using Markdig.Renderers;
using Markdig.Syntax;
using System.IO;
using Hr = Markdig.Renderers.Html;
using Nr = Markdig.Renderers.Normalize;
using Jr = EhTagClient.MakdigExt.Json;
using Markdig;
using System.Linq;
using System.Text;

namespace EhTagClient.MakdigExt
{

    static class Renderer
    {
        public static StringBuilder TrimEnd(this StringBuilder sb)
        {
            if (sb is null || sb.Length == 0) return sb;

            var i = sb.Length - 1;
            for (; i >= 0; i--)
                if (!char.IsWhiteSpace(sb[i]))
                    break;

            if (i < sb.Length - 1)
                sb.Length = i + 1;

            return sb;
        }

        static Renderer()
        {
            _NormalizeOptions = new Nr.NormalizeOptions
            {
                ListItemCharacter = '-'
            };
            var builder = new MarkdownPipelineBuilder();
            builder.DisableHtml();
            builder.BlockParsers.RemoveAll(bp => !(bp is Markdig.Parsers.ParagraphBlockParser));
            _Pipeline = builder.Build();
        }

        private static readonly MarkdownPipeline _Pipeline;

        private static readonly Nr.NormalizeOptions _NormalizeOptions;

        public static MarkdownDocument Parse(string markdown)
            => Extension.Normalize(Markdown.Parse(markdown, _Pipeline));

        private static HtmlRenderer _CreateHtmlRenderer(TextWriter writer, bool enableHtml)
        {
            var htmlRenderer = new HtmlRenderer(writer)
            {
                UseNonAsciiNoEscape = true
            };
            if (enableHtml)
            {
            }
            else
            {
                htmlRenderer.EnableHtmlForBlock = false;
                htmlRenderer.EnableHtmlForInline = false;
            }
            htmlRenderer.ObjectRenderers.Replace<Hr.Inlines.LinkInlineRenderer>(new Html.EhLinkInlineRenderer());
            htmlRenderer.ObjectRenderers.Replace<Hr.Inlines.HtmlEntityInlineRenderer>(new Html.HtmlEntityInlineRenderer());
            htmlRenderer.ObjectRenderers.Find<Hr.Inlines.LineBreakInlineRenderer>().RenderAsHardlineBreak = true;
            _Pipeline.Setup(htmlRenderer);
            return htmlRenderer;
        }

        public static string ToPlainText(MarkdownDocument document)
        {
            using (var sw = new StringWriter())
            {
                var htmlRenderer = _CreateHtmlRenderer(sw, false);
                htmlRenderer.Render(document);
                sw.GetStringBuilder().TrimEnd();
                return sw.ToString();
            }
        }

        public static string ToHtml(MarkdownDocument document)
        {
            using (var sw = new StringWriter())
            {
                var htmlRenderer = _CreateHtmlRenderer(sw, true);
                htmlRenderer.Render(document);
                sw.GetStringBuilder().TrimEnd();
                return sw.ToString();
            }
        }

        public static string ToJson(MarkdownDocument document)
        {
            using (var sw = new StringWriter())
            {
                var jsonRenderer = new Jr.JsonRenderer(sw);
                _Pipeline.Setup(jsonRenderer);
                jsonRenderer.Render(document);
                return sw.ToString();
            }
        }

        public static string ToNormalizedMarkdown(MarkdownDocument document)
        {
            using (var sw = new StringWriter())
            {
                var normalizeRenderer = new Nr.NormalizeRenderer(sw, _NormalizeOptions)
                {
                };
                _Pipeline.Setup(normalizeRenderer);
                normalizeRenderer.Render(document);
                return sw.ToString();
            }
        }
    }
}
