using System.Collections.Generic;
using Newtonsoft.Json;

namespace Syntaxlyn.Web.Models
{
    [JsonObject]
    public class RepositioryInfo
    {
        [JsonProperty("solutions")]
        public IReadOnlyList<SolutionInfo> Solutions { get; set; }

        [JsonProperty("projects")]
        public IReadOnlyList<ProjectInfo> Projects { get; set; }

        [JsonProperty("files")]
        public IReadOnlyList<FileInfo> Files { get; set; }

        /// <summary>
        /// key: Document ID, Value: Path
        /// </summary>
        [JsonProperty("documents")]
        public IReadOnlyDictionary<string, string> Documents { get; set; }
    }

    [JsonObject]
    public class SolutionInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("projects")]
        public IReadOnlyList<string> Projects { get; set; }
    }

    public enum ProjectItemType
    {
        CSharp, VisualBasic, Folder
    }

    [JsonObject]
    public class ProjectInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("language")]
        public ProjectItemType Language { get; set; }

        [JsonProperty("chileren")]
        public IReadOnlyList<ProjectItem> Children { get; set; }
    }

    [JsonObject]
    public class ProjectItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("type")]
        public ProjectItemType Type { get; set; }

        [JsonProperty("project")]
        public string Project { get; set; }

        [JsonProperty("document")]
        public string Document { get; set; }

        [JsonProperty("chileren")]
        public IReadOnlyList<ProjectItem> Children { get; set; }
    }

    [JsonObject]
    public class FileInfo
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("isFolder")]
        public bool IsFolder { get; set; }

        [JsonProperty("documents")]
        public IReadOnlyList<string> Documents { get; set; }

        [JsonProperty("children")]
        public IReadOnlyList<FileInfo> Children { get; set; }
    }
}