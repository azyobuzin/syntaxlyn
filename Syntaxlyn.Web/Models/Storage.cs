using System.Configuration;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Syntaxlyn.Web.Models
{
    public static class Storage
    {
        static Storage()
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageConnection"].ConnectionString);
            GitHub = new StorageWrapper(storageAccount, "GitHub");
            Bitbucket = new StorageWrapper(storageAccount, "Bitbucket");
        }

        public static StorageWrapper GitHub { get; private set; }
        public static StorageWrapper Bitbucket { get; private set; }
    }

    public class StorageWrapper
    {
        public StorageWrapper(CloudStorageAccount storageAccount, string service)
        {
            this.blobContainer = storageAccount.CreateCloudBlobClient()
                .GetContainerReference(service.ToLowerInvariant());
            this.service = service;
            this.table = storageAccount.CreateCloudTableClient().GetTableReference("repos");
        }

        private readonly CloudBlobContainer blobContainer;
        private readonly string service;
        private readonly CloudTable table;
        private bool initialized;

        private async Task Initialize()
        {
            if (!this.initialized)
            {
                this.initialized = true;
                await this.blobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, null, null)
                    .ConfigureAwait(false);
                await this.table.CreateIfNotExistsAsync().ConfigureAwait(false);
            }
        }

        private static string GetPath(string user, string repo, string sha, string name)
        {
            return "\{user}/\{repo}/\{sha}/\{name}";
        }

        public async Task<BuildResultEntity> GetBuildResult(string user, string repo, string sha)
        {
            await this.Initialize().ConfigureAwait(false);

            var result = await this.table.ExecuteAsync(
                TableOperation.Retrieve<BuildResultEntity>(
                    this.service,
                    BuildResultEntity.CreateRowKey(user, repo, sha)
                )
            ).ConfigureAwait(false);
            return result.Result as BuildResultEntity;
        }

        public async Task UpdateBuildResult(BuildResultEntity entity)
        {
            await this.Initialize().ConfigureAwait(false);
            await this.table.ExecuteAsync(TableOperation.InsertOrReplace(entity)).ConfigureAwait(false);
        }

        public async Task RemoveBuildResult(BuildResultEntity entity)
        {
            await this.Initialize().ConfigureAwait(false);
            await this.table.ExecuteAsync(TableOperation.Delete(entity)).ConfigureAwait(false);
        }

        public async Task<RepositioryInfo> GetInfo(string user, string repo, string sha)
        {
            await this.Initialize().ConfigureAwait(false);

            var blob = this.blobContainer.GetBlockBlobReference(GetPath(user, repo, sha, "info.json"));
            if (await blob.ExistsAsync().ConfigureAwait(false))
            {
                return JsonConvert.DeserializeObject<RepositioryInfo>(
                    await blob.DownloadTextAsync().ConfigureAwait(false));
            }

            return null;
        }

        public async Task UploadInfo(string user, string repo, string sha, RepositioryInfo info)
        {
            await this.Initialize().ConfigureAwait(false);

            var blob = this.blobContainer.GetBlockBlobReference(GetPath(user, repo, sha, "info.json"));
            await blob.UploadTextAsync(JsonConvert.SerializeObject(info)).ConfigureAwait(false);
        }

        public async Task RemoveInfo(string user, string repo, string sha)
        {
            await this.Initialize().ConfigureAwait(false);

            var blob = this.blobContainer.GetBlockBlobReference(GetPath(user, repo, sha, "info.json"));
            await blob.DeleteIfExistsAsync().ConfigureAwait(false);
        }
    }
}