using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Octokit;

namespace Syntaxlyn.Web.Models
{
    public static class GitHubService
    {
        private const string UserAgent = "Syntaxlyn";
        private static readonly GitHubClient client = new GitHubClient(new Connection(
            new Octokit.ProductHeaderValue(UserAgent),
            new GitHubHttpClient(
                ConfigurationManager.AppSettings["GitHubClientId"],
                ConfigurationManager.AppSettings["GitHubClientSecret"]
            )
        ));

        public static Task<GitHubCommit> GetLatestCommitOfRef(string owner, string repo, string sha)
        {
            return client.Repository.Commits.Get(owner, repo, sha);
        }

        public static async Task<GitHubCommit> GetLatestCommitOfRepository(string owner, string repo)
        {
            return (await client.Connection
                .Get<List<GitHubCommit>>(ApiUrls.RepositoryCommits(owner, repo), null, null)
                .ConfigureAwait(false)
            ).BodyAsObject[0];
        }

        public static async Task<ZipArchive> DownloadZipball(string owner, string repo, string sha)
        {
            var res = await client.Connection.Get<Stream>(
                new Uri(
                    string.Format(
                        "repos/{0}/{1}/zipball/{2}",
                        Uri.EscapeDataString(owner),
                        Uri.EscapeDataString(repo),
                        Uri.EscapeDataString(sha)
                    ),
                    UriKind.Relative
                ),
                null,
                null
            ).ConfigureAwait(false);
            return new ZipArchive(res.BodyAsObject, ZipArchiveMode.Read, false);
        }

        public static Tuple<string, string> GetOwnerAndRepoName(string htmlUrl)
        {
            var s = htmlUrl.Substring("https://github.com/".Length).Split('/');
            return Tuple.Create(s[0], s[1]);
        }

        public static async Task Build(string owner, string repo, string sha, HttpServerUtilityBase server)
        {
            var id = new CommitId("GitHub", owner, repo, sha);
            BuildProgressManager.SetProgress(id, new BuildProgress(BuildState.CheckingOut));

            try
            {
                var tmpDir = Directory.CreateDirectory(server.MapPath("~/App_Data/" + Guid.NewGuid().ToString()));
                using (var zip = await DownloadZipball(owner, repo, sha).ConfigureAwait(false))
                    zip.ExtractToDirectory(tmpDir.FullName);

                tmpDir = tmpDir.GetDirectories()[0];

                var info = new RepositioryInfo();
                BuildProgressManager.SetProgress(id, new BuildProgress(BuildState.SearchingSolutions));

                var workspace = MSBuildWorkspace.Create();
                info.Solutions = (await Task.WhenAll(
                    tmpDir.EnumerateFiles("*.sln", SearchOption.AllDirectories)
                        .Select(async f =>
                        {
                            var solution = await workspace.OpenSolutionAsync(f.FullName).ConfigureAwait(false);
                            return new SolutionInfo()
                            {
                                Id = solution.Id.Id.ToString(),
                                Text = Path.GetFileNameWithoutExtension(f.Name),
                                Projects = solution.ProjectIds.Select(p => p.Id.ToString()).ToArray()
                            };
                        })
                ).ConfigureAwait(false)).OrderBy(s => s.Text).ToArray();

                //TODO
            }
            catch (Exception ex)
            {
                await Storage.GitHub.UpdateBuildResult(
                    new BuildResultEntity("GitHub", owner, repo, sha, false, ex.ToString())
                ).ConfigureAwait(false);
            }

            BuildProgressManager.Done(id);
        }
    }
}