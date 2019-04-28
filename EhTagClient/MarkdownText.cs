using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;
using MS = Markdig.Syntax;
using MSI = Markdig.Syntax.Inlines;

namespace EhTagClient
{

    [System.Diagnostics.DebuggerDisplay(@"MD{Raw}")]
    public sealed class MarkdownText
    {
        public MarkdownText(string rawString)
        {
            Raw = (rawString ?? "").Trim();
            var ast = MarkdigExt.Renderer.Parse(Raw);
            Text = MarkdigExt.Renderer.ToPlainText(ast);
            Raw = MarkdigExt.Renderer.ToNormalizedMarkdown(ast);
            Html = MarkdigExt.Renderer.ToHtml(ast);
            Ast = new Newtonsoft.Json.Linq.JRaw(MarkdigExt.Renderer.ToJson(ast));
        }

        public string Raw { get; }
        public string Text { get; }
        public string Html { get; }
        public Newtonsoft.Json.Linq.JRaw Ast { get; }
    }
}
