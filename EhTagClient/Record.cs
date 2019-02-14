using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EhTagClient
{
    public class AcceptableTranslationAttribute : ValidationAttribute
    {
        public override bool RequiresValidationContext => true;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!(value is Record record))
                return new ValidationResult($"The value '{value}' is not valid.");
            var failedFields = new List<string>(2);
            if (string.IsNullOrEmpty(record.Name.Text))
                failedFields.Add(validationContext.MemberName + ".name.text");
            if (failedFields.Count != 0)
                return new ValidationResult($"Field should not be empty.", failedFields);
            return ValidationResult.Success;
        }
    }
    public class AcceptableRawAttribute : RegularExpressionAttribute
    {
        public AcceptableRawAttribute() : base("^[a-zA-Z0-9][a-zA-Z0-9 ]*[a-zA-Z0-9]$")
        {
        }

        public override string FormatErrorMessage(string name)
            => "Must be a valid tag name.";
    }

    public class Record
    {
        private static readonly string _Raw = "_Raw";

        private static readonly Regex _LineRegex = new Regex(
            $@"
            ^\s*(?<!\\)\|?\s*
            (?<{nameof(_Raw)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Name)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Intro)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Links)}>.*?)
		    \s*(?<!\\)\|?\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

        internal static KeyValuePair<string, Record> TryParse(string line)
        {
            var match = _LineRegex.Match(line);
            if (!match.Success)
            {
                return default;
            }

            var raw = match.Groups[nameof(_Raw)].Value.Trim().ToLower();
            var name = match.Groups[nameof(Name)].Value;
            var intro = match.Groups[nameof(Intro)].Value;
            var links = match.Groups[nameof(Links)].Value;
            return KeyValuePair.Create(raw, new Record(_Unescape(name), _Unescape(intro), _Unescape(links)));
        }

        private static string _Unescape(string value)
        {
            return value
                .Replace("<br>", "\n");
        }

        private static string _Escape(string value)
        {
            return value
                .Replace("\r\n", "<br>")
                .Replace("\n", "<br>")
                .Replace("\r", "<br>");
        }

        [JsonConstructor]
        public Record(string name, string intro, string links)
        {
            Name = new MarkdownText(name);
            Intro = new MarkdownText(intro);
            Links = new MarkdownText(links);
        }

        public MarkdownText Name { get; }

        public MarkdownText Intro { get; }

        public MarkdownText Links { get; }

        public string ToString(string raw)
            => $"| {raw} | {_Escape(Name.Raw)} | {_Escape(Intro.Raw)} | {_Escape(Links.Raw)} |";
    }
}
