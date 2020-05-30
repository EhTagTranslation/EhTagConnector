using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System;

namespace EhTagClient.MarkdigExt.Html
{
    class CodeInlineRenderer : HtmlObjectRenderer<CodeInline>
    {
        protected override void Write(HtmlRenderer renderer, CodeInline obj)
        {
            var content = obj.Content;
            var tag = Extension.GetTagName(content);
            if (renderer.EnableHtmlForInline)
            {
                if (tag != null)
                {
                    renderer.Write("<abbr title=\"");
                    renderer.WriteEscape(content);
                    renderer.Write("\">");
                    renderer.WriteEscape(tag);
                }
                else
                {
                    renderer.Write("<abbr>");
                    renderer.WriteEscape(content);
                }
                renderer.Write("</abbr>");
            }
            else
            {
                if (tag != null)
                {
                    renderer.WriteEscape(tag);
                    renderer.Write('(');
                    renderer.WriteEscape(content);
                    renderer.Write(')');
                }
                else
                {
                    renderer.WriteEscape(content);
                }
            }
        }
    }
}
