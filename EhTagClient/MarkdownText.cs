using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;

namespace EhTagClient
{

    [System.Diagnostics.DebuggerDisplay(@"MD{Raw}")]
    public sealed class MarkdownText
    {

        public MarkdownText(string rawString)
        {
            Raw = (rawString ?? "").Trim();
            using (var tsw = new StringWriter())
            using (var nsw = new StringWriter())
            {
                var ast = MakdigExt.Renderer.Parse(Raw);
                Text = MakdigExt.Renderer.ToPlainText(ast);
                Raw = MakdigExt.Renderer.ToNormalizedMarkdown(ast);
                Html = MakdigExt.Renderer.ToHtml(ast);
                Ast = new Newtonsoft.Json.Linq.JRaw(MakdigExt.Renderer.ToJson(ast));
            }
        }


        public string Raw { get; }
        public string Text { get; }
        public string Html { get; }
        public Newtonsoft.Json.Linq.JRaw Ast { get; }
    }
}
