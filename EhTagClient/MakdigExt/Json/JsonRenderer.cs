using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            {
                JsonWriter.WriteEndArray();
                Debug.Assert(!_IsWritingText);
            }
            return r;
        }

        public void WriteContentProperty(ContainerInline containerInline)
        {
            JsonWriter.WritePropertyName("content");
            JsonWriter.WriteStartArray();
            WriteChildren(containerInline);
            WriteTextEnd();
            JsonWriter.WriteEndArray();
        }

        public void WriteProperty(string propertyName, object value)
        {
            JsonWriter.WritePropertyName(propertyName);
            if (value is Markdig.Helpers.StringSlice slice)
                value = slice.ToString();
            JsonWriter.WriteValue(value);
        }

        private bool _IsWritingText;
        private readonly StringBuilder _PendingText = new StringBuilder();

        public void WriteTextContent(string content)
        {
            if (!_IsWritingText)
            {
                Debug.Assert(_PendingText.Length == 0);
                _IsWritingText = true;
                JsonWriter.WriteStartObject();
                WriteProperty("type", "text");
                JsonWriter.WritePropertyName("text");
            }
            _PendingText.Append(content);
        }
        public void WriteTextEnd()
        {
            if (_IsWritingText)
            {
                _IsWritingText = false;
                JsonWriter.WriteValue(_PendingText.ToString());
                _PendingText.Clear();
                JsonWriter.WriteEndObject();
            }
        }

    }

    internal abstract class JsonObjectRenderer<TObject> : MarkdownObjectRenderer<JsonRenderer, TObject> where TObject : MarkdownObject
    {
        protected override void Write(JsonRenderer renderer, TObject obj)
        {
            renderer.WriteTextEnd();
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

    internal abstract class JsonTextRender<TObject> : JsonObjectRenderer<TObject> where TObject : MarkdownObject
    {
        protected override void Write(JsonRenderer renderer, TObject obj)
        {
            renderer.WriteTextContent(GetText(renderer, obj));
        }

        protected sealed override string GetType(JsonRenderer renderer, TObject obj) => "text";

        protected override void WriteData(JsonRenderer renderer, TObject obj) { }

        protected abstract string GetText(JsonRenderer renderer, TObject obj);
    }


    sealed class ParagraphRenderer : JsonObjectRenderer<ParagraphBlock>
    {
        protected override string GetType(JsonRenderer renderer, ParagraphBlock obj) => "paragraph";

        protected override void WriteContent(JsonRenderer renderer, ParagraphBlock obj) => renderer.WriteContentProperty(obj.Inline);
    }

    sealed class AutolinkInlineRenderer : JsonObjectRenderer<AutolinkInline>
    {
        protected override string GetType(JsonRenderer renderer, AutolinkInline obj) => "link";

        protected override void WriteData(JsonRenderer renderer, AutolinkInline obj)
        {
            renderer.WriteProperty("title", obj.Url);
            renderer.WriteProperty("url", obj.IsEmail ? "mailto:" + obj.Url : obj.Url);
        }
    }

    sealed class CodeInlineRenderer : JsonObjectRenderer<CodeInline>
    {
        protected override string GetType(JsonRenderer renderer, CodeInline obj) => "code";

        protected override void WriteContent(JsonRenderer renderer, CodeInline obj) => renderer.WriteProperty("text", obj.Content);
    }

    sealed class DelimiterInlineRenderer : JsonTextRender<DelimiterInline>
    {
        protected override string GetText(JsonRenderer renderer, DelimiterInline obj) => obj.ToLiteral();

        protected override void Write(JsonRenderer renderer, DelimiterInline obj)
        {
            base.Write(renderer, obj);
            renderer.WriteChildren(obj);
        }
    }

    sealed class EmphasisInlineRenderer : JsonObjectRenderer<EmphasisInline>
    {
        protected override string GetType(JsonRenderer renderer, EmphasisInline obj) => obj.DelimiterCount > 1 ? "strong" : "emphasis";
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
    sealed class LiteralInlineRenderer : JsonTextRender<LiteralInline>
    {
        protected override string GetText(JsonRenderer renderer, LiteralInline obj) => obj.Content.ToString();
    }

    sealed class NormalizeHtmlEntityInlineRenderer : JsonTextRender<HtmlEntityInline>
    {
        protected override string GetText(JsonRenderer renderer, HtmlEntityInline obj) => obj.Transcoded.ToString();
    }

}
