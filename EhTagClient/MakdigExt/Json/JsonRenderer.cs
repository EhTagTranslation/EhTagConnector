using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EhTagClient.MakdigExt.Json
{
    internal sealed class JsonRenderer : TextRendererBase<JsonRenderer>
    {
        public JsonWriter JsonWriter { get; }

        public JsonRenderer(TextWriter writer) : base(writer)
        {
            JsonWriter = new JsonTextWriter(writer);
            ObjectRenderers.Add(new ParagraphRenderer());
            ObjectRenderers.Add(new AutolinkInlineRenderer());
            ObjectRenderers.Add(new CodeInlineRenderer());
            ObjectRenderers.Add(new DelimiterInlineRenderer());
            ObjectRenderers.Add(new EmphasisInlineRenderer());
            ObjectRenderers.Add(new LineBreakInlineRenderer());
            ObjectRenderers.Add(new NormalizeHtmlEntityInlineRenderer());
            ObjectRenderers.Add(new LinkInlineRenderer());
            ObjectRenderers.Add(new LiteralInlineRenderer());
        }

        public override object Render(MarkdownObject markdownObject)
        {
            var isDoc = markdownObject is MarkdownDocument;
            if (isDoc)
                JsonWriter.WriteStartArray();
            var r = base.Render(markdownObject);
            if (isDoc)
                JsonWriter.WriteEndArray();
            return r;
        }

        public void WriteContentProperty(ContainerInline containerInline)
        {
            JsonWriter.WritePropertyName("content");
            JsonWriter.WriteStartArray();
            WriteChildren(containerInline);
            JsonWriter.WriteEndArray();
        }

        public void WriteProperty(string propertyName, object value)
        {
            JsonWriter.WritePropertyName(propertyName);
            if (value is Markdig.Helpers.StringSlice slice)
                value = slice.ToString();
            JsonWriter.WriteValue(value);
        }
    }

    internal abstract class JsonObjectRenderer<TObject> : MarkdownObjectRenderer<JsonRenderer, TObject> where TObject : MarkdownObject
    {
        protected override void Write(JsonRenderer renderer, TObject obj)
        {
            renderer.JsonWriter.WriteStartObject();
            renderer.WriteProperty("type", GetType(renderer, obj));
            WriteData(renderer, obj);
            WriteContent(renderer, obj);
            renderer.JsonWriter.WriteEndObject();
        }

        protected abstract string GetType(JsonRenderer renderer, TObject obj);

        protected virtual void WriteData(JsonRenderer renderer, TObject obj) { }

        protected virtual void WriteContent(JsonRenderer renderer, TObject obj)
        {
            if (obj is ContainerInline containerInline)
            {
                renderer.WriteContentProperty(containerInline);
            }
        }
    }


    sealed class ParagraphRenderer : JsonObjectRenderer<ParagraphBlock>
    {
        protected override string GetType(JsonRenderer renderer, ParagraphBlock obj) => "paragraph";

        protected override void WriteContent(JsonRenderer renderer, ParagraphBlock obj) => renderer.WriteContentProperty(obj.Inline);
    }

    sealed class AutolinkInlineRenderer : JsonObjectRenderer<AutolinkInline>
    {
        protected override string GetType(JsonRenderer renderer, AutolinkInline obj) => "autolink";


        protected override void WriteData(JsonRenderer renderer, AutolinkInline obj) => renderer.WriteProperty("url", obj.Url);
    }

    sealed class CodeInlineRenderer : JsonObjectRenderer<CodeInline>
    {
        protected override string GetType(JsonRenderer renderer, CodeInline obj) => "code";

        protected override void WriteContent(JsonRenderer renderer, CodeInline obj) => renderer.WriteProperty("content", obj.Content);
    }

    sealed class DelimiterInlineRenderer : JsonObjectRenderer<DelimiterInline>
    {
        protected override string GetType(JsonRenderer renderer, DelimiterInline obj) => "text";

        protected override void Write(JsonRenderer renderer, DelimiterInline obj)
        {
            renderer.JsonWriter.WriteStartObject();
            renderer.WriteProperty("type", GetType(renderer, obj));
            renderer.WriteProperty("text", obj.ToLiteral());
            renderer.JsonWriter.WriteEndObject();
            renderer.WriteChildren(obj);
        }
    }

    sealed class EmphasisInlineRenderer : JsonObjectRenderer<EmphasisInline>
    {

        protected override string GetType(JsonRenderer renderer, EmphasisInline obj) => obj.IsDouble ? "strong" : "emphasis";
    }
    sealed class LineBreakInlineRenderer : JsonObjectRenderer<LineBreakInline>
    {
        protected override string GetType(JsonRenderer renderer, LineBreakInline obj) => "br";

        //protected override void WriteData(JsonRenderer renderer, LineBreakInline obj) => renderer.WriteProperty("hard", obj.IsHard);
    }

    sealed class LinkInlineRenderer : JsonObjectRenderer<LinkInline>
    {
        protected override string GetType(JsonRenderer renderer, LinkInline obj) => obj.IsImage ? "image" : "link";

        protected override void WriteData(JsonRenderer renderer, LinkInline obj)
        {
            var (url, title, nsfw) = obj.GetData();
            renderer.WriteProperty("title", title);
            renderer.WriteProperty("url", url);
            if (obj.IsImage)
            {
                renderer.WriteProperty("nsfw", nsfw);
            }
        }
    }
    sealed class LiteralInlineRenderer : JsonObjectRenderer<LiteralInline>
    {
        protected override string GetType(JsonRenderer renderer, LiteralInline obj) => "text";
        protected override void WriteData(JsonRenderer renderer, LiteralInline obj)
            => renderer.WriteProperty("text", obj.Content);
    }

    sealed class NormalizeHtmlEntityInlineRenderer : JsonObjectRenderer<HtmlEntityInline>
    {
        protected override string GetType(JsonRenderer renderer, HtmlEntityInline obj) => "text";
        protected override void WriteData(JsonRenderer renderer, HtmlEntityInline obj)
            => renderer.WriteProperty("text", obj.Transcoded);
    }

}
