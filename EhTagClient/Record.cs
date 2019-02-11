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
            if (string.IsNullOrEmpty(record.Raw))
                failedFields.Add(validationContext.MemberName + ".raw");
            if (string.IsNullOrEmpty(record.NameRaw))
                failedFields.Add(validationContext.MemberName + ".name");
            if (failedFields.Count != 0)
                return new ValidationResult($"Field should not be empty.", failedFields);
            return ValidationResult.Success;
        }
    }

    [DebuggerDisplay(@"\{{Raw} => {Name.RawString}\}")]
    public class Record
    {
        private static readonly Regex _LineRegex = new Regex(
            $@"
            ^\s*(?<!\\)\|?\s*
            (?<{nameof(Raw)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Name)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Intro)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Links)}>.*?)
		    \s*(?<!\\)\|?\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

        internal static Record TryParse(string line)
        {
            var match = _LineRegex.Match(line);
            if (!match.Success)
            {
                return null;
            }

            var raw = match.Groups[nameof(Raw)].Value;
            var name = match.Groups[nameof(Name)].Value;
            var intro = match.Groups[nameof(Intro)].Value;
            var links = match.Groups[nameof(Links)].Value;
            return new Record(_Unescape(raw), _Unescape(name), _Unescape(intro), _Unescape(links));
        }

        private static string _Unescape(string value)
        {
            if (value.Contains("<br>") || value.Contains(@"\"))
            {
                var sb = new StringBuilder(value);
                sb.Replace(@"\|", @"|");
                sb.Replace(@"\~", @"~");
                sb.Replace(@"\*", @"*");
                sb.Replace(@"\\", @"\");
                sb.Replace("<br>", "\n");
                return sb.ToString();
            }
            return value;
        }

        private static string _Escape(string value)
        {
            return value
                .Replace(@"\", @"\\")
                .Replace(@"|", @"\|")
                .Replace(@"~", @"\~")
                .Replace(@"*", @"\*")
                .Replace("\r\n", "<br>")
                .Replace("\n", "<br>")
                .Replace("\r", "<br>");
        }

        internal static Record Combine(Record r1, Record r2)
        {
            if (r1.Raw != r2.Raw)
            {
                throw new InvalidOperationException();
            }

            string name, intro;
            if (r1.NameRaw == r2.NameRaw)
            {
                name = r1.NameRaw;
            }
            else
            {
                name = $@"{r1.NameRaw} | {r2.NameRaw}";
            }
            if (string.IsNullOrWhiteSpace(r1.IntroRaw))
            {
                intro = r2.IntroRaw;
            }
            else if (string.IsNullOrWhiteSpace(r2.IntroRaw))
            {
                intro = r1.IntroRaw;
            }
            else
            {
                intro = $"{r1.IntroRaw}<hr>{r2.IntroRaw}";
            }

            return new Record(r1.Raw, name, intro, $"{r1.LinksRaw} {r2.LinksRaw}");
        }

        [JsonConstructor]
        public Record(string raw, string name, string intro, string links)
        {
            Raw = (raw ?? "").Trim().ToLower();
            NameRaw = (name ?? "").Trim();
            IntroRaw = (intro ?? "").Trim();
            LinksRaw = (links ?? "").Trim();
        }

        public string Raw { get; }

        [JsonProperty("name")]
        public string NameRaw { get; }

        [JsonIgnore]
        public MarkdownText Name => new MarkdownText(NameRaw);

        [JsonProperty("intro")]
        public string IntroRaw { get; }

        [JsonIgnore]
        public MarkdownText Intro => new MarkdownText(IntroRaw);

        [JsonProperty("links")]
        public string LinksRaw { get; }

        [JsonIgnore]
        public IEnumerable<MarkdownLink> Links => new MarkdownText(LinksRaw).Tokens.OfType<MarkdownLink>();

        public override string ToString() 
            => $"| {_Escape(Raw)} | {_Escape(NameRaw)} | {_Escape(IntroRaw)} | {_Escape(LinksRaw)} |";
    }
}
