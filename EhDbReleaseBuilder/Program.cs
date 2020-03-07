using EhTagClient;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EhDbReleaseBuilder
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">Source directory, the git repo of database.</param>
        /// <param name="target">Target directory, where the releases will be saved.</param>
        /// <param name="checkTags">Use e-hentai api to check tags in given namespace.</param>
        /// <returns></returns>
        public static async Task Main(string source, string target, Namespace checkTags = 0)
        {
            Console.WriteLine($@"EhDbReleaseBuilder started.
  Source: {source}
  Target: {target}
  Check tags: {(checkTags > Namespace.Reclass ? checkTags.ToString() : "<Disabled>")}
");
            var client = new GitHubApiClient(source, target);
            if (checkTags > Namespace.Reclass)
                await client.CheckAsync(checkTags);
            client.Normalize();
            client.Publish();
        }
    }
}
