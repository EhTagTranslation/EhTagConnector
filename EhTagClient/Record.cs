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
            if (string.IsNullOrEmpty(record.Original))
                failedFields.Add(validationContext.MemberName + ".original");
            if (string.IsNullOrEmpty(record.TranslatedRaw))
                failedFields.Add(validationContext.MemberName + ".translated");
            if (failedFields.Count != 0)
                return new ValidationResult($"Field should not be empty.", failedFields);
            return ValidationResult.Success;
        }
    }

    [DebuggerDisplay(@"\{{Original} => {Translated.RawString}\}")]
    public class Record
    {
        private static Regex lineRegex = new Regex(
            $@"
            ^\s*(?<!\\)\|?\s*
            (?<{nameof(Original)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Translated)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Introduction)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(ExternalLinks)}>.*?)
		    \s*(?<!\\)\|?\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

        internal static Record TryParse(string line)
        {
            var match = lineRegex.Match(line);
            if (!match.Success)
            {
                return null;
            }

            var ori = match.Groups[nameof(Original)].Value;
            var tra = match.Groups[nameof(Translated)].Value;
            var intro = match.Groups[nameof(Introduction)].Value;
            var link = match.Groups[nameof(ExternalLinks)].Value;
            return new Record(unescape(ori), unescape(tra), unescape(intro), unescape(link));
        }

        private static string unescape(string value)
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

        private static string escape(string value)
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
            if (r1.Original != r2.Original)
            {
                throw new InvalidOperationException();
            }

            string translated, intro;
            if (r1.TranslatedRaw == r2.TranslatedRaw)
            {
                translated = r1.TranslatedRaw;
            }
            else
            {
                translated = $@"{r1.TranslatedRaw} | {r2.TranslatedRaw}";
            }
            if (string.IsNullOrWhiteSpace(r1.IntroductionRaw))
            {
                intro = r2.IntroductionRaw;
            }
            else if (string.IsNullOrWhiteSpace(r2.IntroductionRaw))
            {
                intro = r1.IntroductionRaw;
            }
            else
            {
                intro = $"{r1.IntroductionRaw}<hr>{r2.IntroductionRaw}";
            }

            return new Record(r1.Original, translated, intro, $"{r1.ExternalLinksRaw} {r2.ExternalLinksRaw}");
        }

        [JsonConstructor]
        public Record(string original, string translated, string introduction, string externalLinks)
        {
            this.Original = original.Trim().ToLower();
            this.TranslatedRaw = translated.Trim();
            this.IntroductionRaw = introduction.Trim();
            this.ExternalLinksRaw = externalLinks.Trim();
        }

        public string Original { get; }

        [JsonProperty("translated")]
        public string TranslatedRaw { get; }

        [JsonIgnore]
        public MarkdownText Translated => new MarkdownText(TranslatedRaw);

        [JsonProperty("introduction")]
        public string IntroductionRaw { get; }

        [JsonIgnore]
        public MarkdownText Introduction => new MarkdownText(IntroductionRaw);

        [JsonProperty("externalLinks")]
        public string ExternalLinksRaw { get; }

        [JsonIgnore]
        public IEnumerable<MarkdownLink> ExternalLinks => new MarkdownText(ExternalLinksRaw).Tokens.OfType<MarkdownLink>();

        public override string ToString()
        {
            return $"| {escape(Original)} | {escape(TranslatedRaw)} | {escape(IntroductionRaw)} | {escape(ExternalLinksRaw)} |";
        }
    }
}
