using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Text;

namespace EhTagClient.MakdigExt.Html
{
    public class HtmlEntityInlineRenderer : HtmlObjectRenderer<HtmlEntityInline>
    {
        protected override void Write(HtmlRenderer renderer, HtmlEntityInline obj)
        {
            if (renderer.EnableHtmlForInline)
                renderer.WriteEscape(obj.Transcoded);
            else
                renderer.Write(obj.Transcoded);
        }
    }

}
