namespace Syntaxlyn.Web.Models
{
    public struct CommitId
    {
        public CommitId(string service, string user, string repo, string sha)
        {
            this.service = service;
            this.user = user;
            this.repo = repo;
            this.sha = sha;
        }

        private readonly string service;
        private readonly string user;
        private readonly string repo;
        private readonly string sha;

        public string Service { get { return this.service; } }
        public string User { get { return this.user; } }
        public string Repo { get { return this.repo; } }
        public string Sha { get { return this.sha; } }

        public override string ToString()
        {
            return string.Format("{0}/{1}/{2}/{3}", this.service, this.user, this.repo, this.sha);
        }
    }
}