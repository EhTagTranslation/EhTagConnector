using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;

namespace EhTagClient
{
    public class RepoClient
    {
        public RepoClient(string workingDirectory) => Repo = new Repository(workingDirectory);

        public RepoClient() => Init();

        public string RemotePath => $"https://github.com/{Consts.OWNER}/{Consts.REPO}.git";
        public string LocalPath => _Repo.Info.WorkingDirectory;

        private string _GitPath;
        private Repository _Repo;

        public Commit Head => Repo.Commits.First();

        public Repository Repo
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

        public void Init()
        {
            if (Directory.Exists(Consts.REPO_PATH))
            {
                _GitPath = Repository.Discover(Consts.REPO_PATH);
                Repo = new Repository(_GitPath);
                Pull();
            }
            else
            {
                _GitPath = Repository.Clone(RemotePath, Consts.REPO_PATH, new CloneOptions
                {
                    CredentialsProvider = CredentialsProvider
                });
                Repo = new Repository(_GitPath);
            }
        }

        private readonly LibGit2Sharp.Handlers.CredentialsHandler CredentialsProvider
            = (string url, string usernameFromUrl, SupportedCredentialTypes types) =>
            {
                return new UsernamePasswordCredentials
                {
                    Username = Consts.Username,
                    Password = Consts.Password,
                };
            };

        public Identity CommandIdentity => new Identity(Consts.Username, Consts.Email);

        public string CurrentSha => Repo.Commits.First().Sha;

        public void Pull()
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

        public void Commit(string message, Identity author)
        {
            Commands.Stage(Repo, "*");
            Repo.Commit(message, new Signature(author, DateTimeOffset.Now), new Signature(CommandIdentity, DateTimeOffset.Now));
        }

        public void Push()
        {
            Repo.Network.Push(Repo.Branches["master"], new PushOptions
            {
                CredentialsProvider = CredentialsProvider
            });
        }

    }
}
