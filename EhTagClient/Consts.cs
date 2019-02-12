using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EhTagClient
{
    public static class Consts
    {
        internal const string REPO_PATH = "./Db";
        internal const string OWNER = "ehtagtranslation";
        internal const string REPO = "Database";

        public static string Username { get; }
        public static string Password { get; }
        public static string Email { get; }
        public static string Token { get; }

        public static JsonSerializerSettings SerializerSettings { get; } = new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy= new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy(),
            },
            Converters =
            {
                new Newtonsoft.Json.Converters.StringEnumConverter(new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy())
            },
            MaxDepth = 32,
        };

        static Consts()
        {
            Username = _GetString("GitHub:Username");
            Password = _GetString("GitHub:Password");
            Email = _GetString("GitHub:Email");
            Token = _GetString("GitHub:Token");
        }

        private static string _GetString(string key)
        {
            var ev = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process)
                ?? Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Machine);

            if (string.IsNullOrEmpty(ev))
                throw new ArgumentException($"Environment variable `{key}` not found.");
            return ev;
        }
    }
}
