using Markdig.Renderers;
using Markdig.Syntax;
using System.IO;
using Hr = Markdig.Renderers.Html;
using Nr = Markdig.Renderers.Normalize;
using Jr = EhTagClient.MakdigExt.Json;
using Markdig;
using System.Linq;

namespace EhTagClient.MakdigExt
{

    static class Renderer
    {
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

        public static string ToPlainText(MarkdownDocument document)
        {
            using (var sw = new StringWriter())
            {
                var htmlRenderer = new HtmlRenderer(sw)
                {
                    EnableHtmlForBlock = false,
                    EnableHtmlForInline = false,
                };
                htmlRenderer.ObjectRenderers.Replace<Hr.Inlines.LinkInlineRenderer>(new Html.EhLinkInlineRenderer());
                _Pipeline.Setup(htmlRenderer);
                htmlRenderer.Render(document);
                return System.Web.HttpUtility.HtmlDecode(sw.ToString().Trim());
            }
        }

        public static string ToHtml(MarkdownDocument document)
        {
            using (var sw = new StringWriter())
            {
                var htmlRenderer = new HtmlRenderer(sw)
                {
                    UseNonAsciiNoEscape = true,
                };
                htmlRenderer.ObjectRenderers.Replace<Hr.Inlines.LinkInlineRenderer>(new Html.EhLinkInlineRenderer());
                htmlRenderer.ObjectRenderers.Find<Hr.Inlines.LineBreakInlineRenderer>().RenderAsHardlineBreak = true;
                _Pipeline.Setup(htmlRenderer);
                htmlRenderer.Render(document);
                return sw.ToString().Trim();
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
