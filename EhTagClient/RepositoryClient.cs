using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EhTagClient
{
    public static class RepositoryClient
    {
        internal const string REPO_PATH = "./Db";
        private const string REMOTE_PATH = "https://github.com/ehtagtranslation/Database.git";

        private static string _GitPath;
        private static Repository _Repo;

        public static string Username { get; set; }
        public static string Password { get; set; }
        public static string Email { get; set; }

        public static Repository Repo
        {
            get
            {
                if (_Repo is null)
                    Init();
                return _Repo;
            }
            private set
            {
                var old = Interlocked.Exchange(ref _Repo, value);
                old?.Dispose();
            }
        }

        public static void Init()
        {
            if (Directory.Exists(REPO_PATH))
            {
                _GitPath = Repository.Discover(REPO_PATH);
                Repo = new Repository(_GitPath);
                Pull();
            }
            else
            {
                _GitPath = Repository.Clone(REMOTE_PATH, REPO_PATH, new CloneOptions
                {
                    CredentialsProvider = CredentialsProvider
                });
                Repo = new Repository(_GitPath);
            }
        }

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

        public static string CurrentSha => Repo.Commits.First().Sha;

        public static void Pull()
        {
            Commands.Pull(Repo, new Signature(CommandIdentity, DateTimeOffset.Now), new PullOptions
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

        public static void Commit(string message, Identity author)
        {
            Commands.Stage(Repo, "*");
            Repo.Commit(message, new Signature(author, DateTimeOffset.Now), new Signature(CommandIdentity, DateTimeOffset.Now));
        }

        public static void Push()
        {
            Repo.Network.Push(Repo.Branches["master"], new PushOptions
            {
                CredentialsProvider = CredentialsProvider
            });
        }

    }
}
