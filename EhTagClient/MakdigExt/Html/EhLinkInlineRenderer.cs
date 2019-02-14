using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Text;

namespace EhTagClient.MakdigExt.Html
{

    class EhLinkInlineRenderer : HtmlObjectRenderer<LinkInline>
    {
        protected override void Write(HtmlRenderer renderer, LinkInline link)
        {
            var (url, title, nsfw) = link.GetData();
            if (renderer.EnableHtmlForInline)
            {
                renderer.Write(link.IsImage ? "<img src=\"" : "<a href=\"");
                renderer.WriteEscapeUrl(url);
                renderer.Write("\"");
                renderer.WriteAttributes(link);
            }
            if (link.IsImage)
            {
                if (renderer.EnableHtmlForInline)
                {
                    renderer.Write(" alt=\"");
                    renderer.EnableHtmlForInline = false;
                    renderer.WriteChildren(link);
                    renderer.EnableHtmlForInline = true;
                    renderer.Write("\"");
                    if(nsfw)
                        renderer.Write(" nsfw");
                }
            }
            if (renderer.EnableHtmlForInline && !string.IsNullOrEmpty(title))
            {
                renderer.Write(" title=\"");
                renderer.WriteEscape(title);
                renderer.Write("\"");
            }
            if (link.IsImage)
            {
                if (renderer.EnableHtmlForInline)
                {
                    renderer.Write(" />");
                }
                return;
            }
            if (renderer.EnableHtmlForInline)
            {
                renderer.Write(" rel=\"nofollow\"");
                renderer.Write(">");
            }
            renderer.WriteChildren(link);
            if (renderer.EnableHtmlForInline)
            {
                renderer.Write("</a>");
            }
        }
    }
}
