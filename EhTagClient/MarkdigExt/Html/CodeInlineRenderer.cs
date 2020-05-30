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
                renderer.Write("<ruby>");
            if (tag != null)
            {
                renderer.WriteEscape(tag);
                if (renderer.EnableHtmlForInline)
                {
                    renderer.Write("<rp>(</rp><rt>");
                    renderer.WriteEscape(content);
                    renderer.Write("</rt><rp>)</rp>");
                }
                else
                {
                    renderer.Write('(');
                    renderer.WriteEscape(content);
                    renderer.Write(')');
                }
            }
            else
            {
                renderer.WriteEscape(content);
            }
            if (renderer.EnableHtmlForInline)
                renderer.Write("</ruby>");
        }
    }
}
