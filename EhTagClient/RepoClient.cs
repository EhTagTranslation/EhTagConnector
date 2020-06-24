using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            if (Directory.Exists(Path.Join(Consts.REPO_PATH, ".git")))
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

        public Identity CommandIdentity => new Identity(Consts.CommitterName, Consts.CommitterEmail);

        public string CurrentSha => Repo.Commits.First().Sha;

        public void Pull()
        {
            var remote = Repo.Network.Remotes["origin"];
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            Commands.Fetch(Repo, remote.Name, refSpecs, new FetchOptions
            {
                CredentialsProvider = CredentialsProvider
            }, "");
            var originMaster = Repo.Branches["origin/master"];
            Repo.Reset(ResetMode.Hard, originMaster.Tip);
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
