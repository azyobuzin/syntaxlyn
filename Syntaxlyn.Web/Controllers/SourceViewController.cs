using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Octokit;
using Syntaxlyn.Web.Models;

namespace Syntaxlyn.Web.Controllers
{
    public class SourceViewController : Controller
    {
        [Route("SourceView/SourceView")]
        public ActionResult SourceView()
        {
            return this.View();
        }

        [Route("SourceView/Pending")]
        public ActionResult Pending()
        {
            return this.View();
        }

        private static bool IsCommit(string s) =>
            s.Length == 40 && s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

        private ActionResult CreateViewResult(string user, string repo, string path, BuildResultEntity info)
        {
            if (info.IsBuildSuccess)
            {
                return this.View("SourceView", new SourceViewViewModel());
            }
            else
            {
                return this.View("BuildFailed", info);
            }
        }

        private ActionResult RedirectToGitHubPermanent(string user, string repo, string[] elms) =>
            this.RedirectToActionPermanent("GitHub", new
            {
                user = user,
                repo = repo,
                path = string.Join("/", elms)
            });


        [Route("github/{user}/{repo}/{*path}")]
        public async Task<ActionResult> GitHub(string user, string repo, string path)
        {
            GitHubCommit commit;

            if (string.IsNullOrEmpty(path))
            {
                commit = await GitHubService.GetLatestCommitOfRepository(user, repo);
                var t = GitHubService.GetOwnerAndRepoName(commit.HtmlUrl);
                return this.RedirectToAction("GitHub", new { user = t.Item1, repo = t.Item2, path = commit.Sha });
            }

            var elms = path.Split('/');
            var sha = elms[0];

            if (!IsCommit(sha))
            {
                commit = await GitHubService.GetLatestCommitOfRef(user, repo, sha);
                elms[0] = commit.Sha;
                var t = GitHubService.GetOwnerAndRepoName(commit.HtmlUrl);
                return this.RedirectToGitHubPermanent(t.Item1, t.Item2, elms);
            }

            if (BuildProgressManager.GetProgress(new CommitId("GitHub", user, repo, sha)).HasValue)
                return this.View("Pending", new PendingViewModel("GitHub", user, repo, sha));

            var result = await Storage.GitHub.GetBuildResult(user, repo, sha);
            if (result != null)
                return this.CreateViewResult(user, repo, path, result);

            try
            {
                commit = await GitHubService.GetLatestCommitOfRef(user, repo, sha);
            }
            catch (NotFoundException)
            {
                return this.HttpNotFound();
            }

            var nameAndRepo = GitHubService.GetOwnerAndRepoName(commit.HtmlUrl);
            var isArgsWrong = user != nameAndRepo.Item1 || repo != nameAndRepo.Item2 || sha != commit.Sha;
            if (isArgsWrong)
            {
                user = nameAndRepo.Item1;
                repo = nameAndRepo.Item2;
                elms[0] = sha = commit.Sha;

                if (BuildProgressManager.GetProgress(new CommitId("GitHub", user, repo, sha)).HasValue
                    || await Storage.GitHub.GetBuildResult(user, repo, sha) != null)
                    return this.RedirectToGitHubPermanent(user, repo, elms);
            }

            BuildProgressManager.SetProgress(new CommitId("GitHub", user, repo, sha), new BuildProgress(BuildState.StartingBuilding));
            var _ = GitHubService.Build(user, repo, sha, this.Server);

            return isArgsWrong
                ? this.RedirectToGitHubPermanent(user, repo, elms)
                : this.View("Pending", new PendingViewModel("GitHub", user, repo, sha));
        }

        [Route("rebuild/github/{user}/{repo}/{*path}")]
        public async Task<ActionResult> RebuildGitHub(string user, string repo, string path)
        {
            var sha = path.Split('/')[0];
            var result = await Storage.GitHub.GetBuildResult(user, repo, sha);
            if ( result != null)
            {
                await Task.WhenAll(
                    Storage.GitHub.RemoveBuildResult(result),
                    Storage.GitHub.RemoveInfo(user, repo, sha)
                );
            }
            return this.RedirectToAction("GitHub", new { user = user, repo = repo, path = path });
        }
    }
}