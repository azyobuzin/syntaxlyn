using System.Reflection;
using Microsoft.WindowsAzure.Storage.Table;

namespace Syntaxlyn.Web.Models
{
    public class BuildResultEntity : TableEntity
    {
        public static string CreateRowKey(string user, string repo, string sha)
        {
            return string.Format("{0}${1}${2}", user, repo, sha);
        }

        public BuildResultEntity() { }

        public BuildResultEntity(string service, string user, string repo, string sha, bool isBuildSuccess, string error)
        {
            this.PartitionKey = service;
            this.RowKey = CreateRowKey(user, repo, sha);
            this.IsBuildSuccess = isBuildSuccess;
            this.Error = error;
            this.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public bool IsBuildSuccess { get; set; }
        public string Error { get; set; }
        public string Version { get; set; }       
    }
}