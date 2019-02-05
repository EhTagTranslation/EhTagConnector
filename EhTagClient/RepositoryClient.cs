using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EhTagClient
{
    public static class RepositoryClient
    {
        internal const string REPO_PATH = "./Db";
        private const string REMOTE_PATH = "https://github.com/ehtagtranslation/Database.git";

        private static string _GitPath;

        public static string Username { get; set; }
        public static string Password { get; set; }
        public static string Email { get; set; }

        private static readonly LibGit2Sharp.Handlers.CredentialsHandler CredentialsProvider
            = (string url, string usernameFromUrl, SupportedCredentialTypes types) =>
            {
                return new UsernamePasswordCredentials
                {
                    Username = Username,
                    Password = Password,
                };
            };

        public static Identity CommandIdentity => new Identity(Username, Email);

        public static void Init()
        {
            if (Directory.Exists(REPO_PATH))
            {
                _GitPath = Repository.Discover(REPO_PATH);
                Pull();
            }
            else
                _GitPath = Repository.Clone(REMOTE_PATH, REPO_PATH, new CloneOptions
                {
                    CredentialsProvider = CredentialsProvider
                });
        }

        public static void Pull()
        {
            using (var repo = Get())
            {
                Commands.Pull(repo, new Signature(CommandIdentity, DateTimeOffset.Now), new PullOptions
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = CredentialsProvider
                    },
                    MergeOptions = new MergeOptions
                    {
                    }
                });
            }
        }

        public static void Commit(string message, Identity author)
        {
            using (var repo = Get())
            {
                Commands.Stage(repo, "*");
                repo.Commit(message, new Signature(author, DateTimeOffset.Now), new Signature(CommandIdentity, DateTimeOffset.Now));
            }
        }

        public static void Push()
        {
            using (var repo = Get())
            {
                repo.Network.Push(repo.Branches["master"], new PushOptions
                {
                    CredentialsProvider = CredentialsProvider
                });
            }
        }

        public static Repository Get()
        {
            if (_GitPath == null)
                Init();
            return new Repository(_GitPath);
        }

    }
}
