using System.Collections.Concurrent;
using Microsoft.AspNet.SignalR;
using Syntaxlyn.Web.Hubs;

namespace Syntaxlyn.Web.Models
{
    public enum BuildState
    {
        StartingBuilding,
        CheckingOut,
        SearchingSolutions,
        ReadingDocuments,
        PreparingToShow
    }

    public struct BuildProgress
    {
        public BuildProgress(BuildState state)
            : this(state, null, 0, 0)
        { }

        public BuildProgress(BuildState state, string workingProjectName, int currentProjectNumber, int projectCount)
        {
            this.state = state;
            this.workingProjectName = workingProjectName;
            this.currentProjectNumber = currentProjectNumber;
            this.projectCount = projectCount;
        }

        private readonly BuildState state;
        private readonly string workingProjectName;
        private readonly int currentProjectNumber;
        private readonly int projectCount;

        public BuildState State { get { return this.state; } }
        public string WorkingProjectName { get { return this.workingProjectName; } }
        public int CurrentProjectNumber { get { return this.currentProjectNumber; } }
        public int ProjectCount { get { return this.projectCount; } }

        public void SendTo(IBuildProgressHubClient signalrClient)
        {
            switch (this.state)
            {
                case BuildState.StartingBuilding:
                    signalrClient.OnTextChanged("Starting building...");
                    break;
                case BuildState.CheckingOut:
                    signalrClient.OnTextChanged("Checking out the repository...");
                    break;
                case BuildState.SearchingSolutions:
                    signalrClient.OnTextChanged("Searching solutions and projects...");
                case BuildState.ReadingDocuments:
                    signalrClient.OnProgressChanged(
                        "Building: " + this.workingProjectName,
                        this.currentProjectNumber,
                        this.projectCount
                    );
                    break;
                case BuildState.PreparingToShow:
                    signalrClient.OnTextChanged("Preparing to show you built pages...");
                    break;
            }
        }
    }

    public static class BuildProgressManager
    {
        private static readonly ConcurrentDictionary<CommitId, BuildProgress> dic = new ConcurrentDictionary<CommitId, BuildProgress>();

        private static IBuildProgressHubClient GetSignalRGroup(CommitId target)
        {
            return GlobalHost.ConnectionManager
                .GetHubContext<BuildProgressHub, IBuildProgressHubClient>()
                .Clients.Group(target.ToString());
        }

        public static void SetProgress(CommitId target, BuildProgress progress)
        {
            dic.AddOrUpdate(target, progress, (_, __) => progress);

            progress.SendTo(GetSignalRGroup(target));
        }

        public static void Done(CommitId target)
        {
            BuildProgress _;
            dic.TryRemove(target, out _);

            GetSignalRGroup(target).OnCompleted();
        }

        public static BuildProgress? GetProgress(CommitId target)
        {
            BuildProgress p;
            return dic.TryGetValue(target, out p) ? p : new BuildProgress?();
        }
    }
}