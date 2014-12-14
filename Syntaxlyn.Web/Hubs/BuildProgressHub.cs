using Microsoft.AspNet.SignalR;
using Syntaxlyn.Web.Models;

namespace Syntaxlyn.Web.Hubs
{
    public interface IBuildProgressHubClient
    {
        void OnTextChanged(string text);
        void OnProgressChanged(string text, int current, int maximum);
        void OnCompleted();
    }

    public class BuildProgressHub : Hub<IBuildProgressHubClient>
    {
        public void Register(string service, string user, string repo, string sha)
        {
            var id = new CommitId(service, user, repo, sha);
            this.Groups.Add(this.Context.ConnectionId, id.ToString());

            var p = BuildProgressManager.GetProgress(id);
            if (p.HasValue)
                p.Value.SendTo(this.Clients.Caller);
            else
                this.Clients.Caller.OnCompleted();
        }
    }
}