
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EhTagClient
{
    [Flags]
    public enum Namespace
    {
        Unknown = 0,

        Reclass = 1,
        Language = 2,
        Parody = 4,
        Character = 8,
        Group = 16,
        Artist = 32,
        Male = 64,
        Female = 128,
        Misc = 256
    }

    public class SingleNamespaceAttribute : ValidationAttribute
    {
        public override string FormatErrorMessage(string name) => $"{name} is not valid namespace.";

        public override bool RequiresValidationContext => true;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!(value is Namespace ns))
                return new ValidationResult($"The value '{value}' is not valid.");
            switch (ns)
            {
            case Namespace.Unknown:
            default:
                return new ValidationResult($"The value '{value}' is not valid.");
            case Namespace.Reclass:
            case Namespace.Language:
            case Namespace.Parody:
            case Namespace.Character:
            case Namespace.Group:
            case Namespace.Artist:
            case Namespace.Male:
            case Namespace.Female:
            case Namespace.Misc:
                return ValidationResult.Success;
            }
        }
    }

    public static class NamespaceExtention
    {
        private static readonly Dictionary<string, Namespace> parsingDic
            = new Dictionary<string, Namespace>(StringComparer.OrdinalIgnoreCase)
            {
                ["R"] = Namespace.Reclass,
                ["Reclass"] = Namespace.Reclass,
                ["L"] = Namespace.Language,
                ["Language"] = Namespace.Language,
                ["Lang"] = Namespace.Language,
                ["P"] = Namespace.Parody,
                ["Parody"] = Namespace.Parody,
                ["Series"] = Namespace.Parody,
                ["C"] = Namespace.Character,
                ["Char"] = Namespace.Character,
                ["Character"] = Namespace.Character,
                ["G"] = Namespace.Group,
                ["Group"] = Namespace.Group,
                ["Creator"] = Namespace.Group,
                ["Circle"] = Namespace.Group,
                ["A"] = Namespace.Artist,
                ["Artist"] = Namespace.Artist,
                ["M"] = Namespace.Male,
                ["Male"] = Namespace.Male,
                ["F"] = Namespace.Female,
                ["Female"] = Namespace.Female
            };

        private static readonly Dictionary<Namespace, string> searchDic
            = new Dictionary<Namespace, string>()
            {
                [Namespace.Reclass] = "reclass",
                [Namespace.Language] = "language",
                [Namespace.Parody] = "parody",
                [Namespace.Character] = "character",
                [Namespace.Group] = "group",
                [Namespace.Artist] = "artist",
                [Namespace.Male] = "male",
                [Namespace.Female] = "female"
            };

        public static string ToSearchString(this Namespace that)
        {
            if (searchDic.TryGetValue(that, out var r))
                return r;
            return null;
        }

        public static string ToShortString(this Namespace that)
        {
            if (searchDic.TryGetValue(that, out var r))
                return r.Substring(0, 1);
            return null;
        }

        public static Namespace Parse(string str)
        {
            if (TryParse(str, out var r))
                return r;
            throw new FormatException("Invalid namespace");
        }

        public static bool TryParse(string str, out Namespace result)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                result = Namespace.Misc;
                return true;
            }
            str = str.Trim();
            if (parsingDic.TryGetValue(str, out result))
                return true;
            var f = str.FirstOrDefault(char.IsLetter);
            if (f == default(char))
                return false;
            if (parsingDic.TryGetValue(f.ToString(), out result))
                return true;
            return false;
        }
    }
}