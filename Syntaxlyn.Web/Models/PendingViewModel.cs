namespace Syntaxlyn.Web.Models
{
    public class PendingViewModel
    {
        public PendingViewModel(string service, string user, string repo, string sha)
        {
            this.Service = service;
            this.User = user;
            this.Repo = repo;
            this.Sha = sha;
        }

        public string Service { get; private set; }
        public string User { get; private set; }
        public string Repo { get; private set; }
        public string Sha { get; private set; }
    }
}