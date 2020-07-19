using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LearningOnSteroids.Storage
{
    public class LearningVideoStorage : ILearningVideoStorage
    {
        private readonly string _ContainerNameVideos = "learningvideos";
        private readonly string _ContainerNameVideosArchive = "learningvideos-archive";
        private readonly string _connectionString;
        private readonly string _metadataKeyTitle = "title";
        private readonly string _metadataKeyDescription = "description";

        public LearningVideoStorage(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<CloudBlockBlob> UploadVideoAsync(byte[] videoByteArray, string blobname, string title, string description)
        {
            var cloudBlobContainer = await GetLearningVideosContainerAsync();

            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobname);
            //loudBlockBlob.Properties.ContentType = "video/mp4";

            SetMetadata(cloudBlockBlob, _metadataKeyTitle, title);
            SetMetadata(cloudBlockBlob, _metadataKeyDescription, description);

            await cloudBlockBlob.UploadFromByteArrayAsync(videoByteArray, 0, videoByteArray.Length);

            return cloudBlockBlob;
        }

        public async Task<bool> CheckIfBlobExistsAsync(string blobname)
        {
            var cloudBlobContainer = await GetLearningVideosContainerAsync();

            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobname);

            return await cloudBlockBlob.ExistsAsync();
        }

        public async Task<IEnumerable<CloudBlockBlob>> ListVideoBlobsAsync(string prefix = null)
        {
            var cloudBlockBlobs = new List<CloudBlockBlob>();
            var cloudBlobContainer = await GetLearningVideosContainerAsync();

            BlobContinuationToken token = null;
            do
            {
                //var blobResultSegment = await cloudBlobContainer.ListBlobsSegmentedAsync(prefix, token);
                var blobResultSegment = await cloudBlobContainer.ListBlobsSegmentedAsync(prefix, true, BlobListingDetails.Metadata, null, token, null, null);
                token = blobResultSegment.ContinuationToken;
                cloudBlockBlobs.AddRange(blobResultSegment.Results.OfType<CloudBlockBlob>());
            }
            while (token != null);

            return cloudBlockBlobs;
        }

        public async Task<CloudBlockBlob> GetCloudBlockBlobAsync(string blobname)
        {
            var cloudBlobContainer = await GetLearningVideosContainerAsync();

            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobname);

            return cloudBlockBlob;
        }

        public async Task DownloadVideoAsync(CloudBlockBlob cloudBlockBlob, Stream targetStream)
        {
            await cloudBlockBlob.DownloadToStreamAsync(targetStream);
        }

        public async Task OverwriteVideoAsync(CloudBlockBlob cloudBlockBlob, byte[] videoByteArray)
        {
            var accessCondition = new AccessCondition
            {
                IfMatchETag = cloudBlockBlob.Properties.ETag
            };

            await cloudBlockBlob.UploadFromByteArrayAsync(videoByteArray, 0, videoByteArray.Length, accessCondition, null, null);
        }

        public async Task DeleteVideoAsync(CloudBlockBlob cloudBlockBlob)
        {
            var accessCondition = new AccessCondition
            {
                IfMatchETag = cloudBlockBlob.Properties.ETag
            };

            //await cloudBlockBlob.DeleteAsync();
            await cloudBlockBlob.DeleteAsync(DeleteSnapshotsOption.None, accessCondition, null, null);
        }

        public async Task UpdateMetadataAsync(CloudBlockBlob cloudBlockBlob, string title, string description)
        {
            SetMetadata(cloudBlockBlob, _metadataKeyTitle, title);
            SetMetadata(cloudBlockBlob, _metadataKeyDescription, description);

            var accessCondition = new AccessCondition
            {
                IfMatchETag = cloudBlockBlob.Properties.ETag
            };

            //await cloudBlockBlob.SetMetadataAsync();
            await cloudBlockBlob.SetMetadataAsync(accessCondition, null, null);
        }

        public async Task ReloadMetadataAsync(CloudBlockBlob cloudBlockBlob)
        {
            await cloudBlockBlob.FetchAttributesAsync();
        }

        public (string title, string description) GetBlobMetadata(CloudBlockBlob cloudBlockBlob)
        {
            return 
                (
                    cloudBlockBlob.Metadata.ContainsKey(_metadataKeyTitle) ? cloudBlockBlob.Metadata[_metadataKeyTitle] : "",
                    cloudBlockBlob.Metadata.ContainsKey(_metadataKeyDescription) ? cloudBlockBlob.Metadata[_metadataKeyDescription] : ""
                );
        }

        public string GetBlobUriWithSasToken(CloudBlockBlob cloudBlockBlob)
        {
            var sharedAccessBlobPolicy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.Now.AddDays(1)
            };

            var sasToken = cloudBlockBlob.GetSharedAccessSignature(sharedAccessBlobPolicy);

            return cloudBlockBlob.Uri + sasToken;
        }

        public async Task ArchiveVideoAsync(CloudBlockBlob cloudBlockBlob)
        {
            var archiveCloudBlobContainer = await GetLearningVideosArchiveContainerAsync();

            var archiveCloudBlockBlob = archiveCloudBlobContainer.GetBlockBlobReference(cloudBlockBlob.Name);

            await archiveCloudBlockBlob.StartCopyAsync(cloudBlockBlob);

            // await WaitForCopyToCompleteAsync(cloudBlockBlob);
        }

        private async Task<CloudBlobContainer> GetLearningVideosContainerAsync()
        {
            return await GetContainerAsync(_ContainerNameVideos);
        }

        private async Task<CloudBlobContainer> GetLearningVideosArchiveContainerAsync()
        {
            return await GetContainerAsync(_ContainerNameVideosArchive);
        }

        private async Task<CloudBlobContainer> GetContainerAsync(string containerName)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(_connectionString);

            var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

            var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
            await cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, null, null);
            return cloudBlobContainer;
        }

        private static void SetMetadata(CloudBlockBlob cloudBlockBlob, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (cloudBlockBlob.Metadata.ContainsKey(key))
                {
                    cloudBlockBlob.Metadata.Remove(key);
                }
            }
            else
            {
                cloudBlockBlob.Metadata[key] = value;
            }
        }

        private static async Task WaitForCopyToCompleteAsync(CloudBlockBlob cloudBlockBlob)
        {
            var copyInProgress = true;

            while (copyInProgress)
            {
                //await Task.Delay(500);
                await cloudBlockBlob.FetchAttributesAsync();
                copyInProgress = cloudBlockBlob.CopyState.Status == CopyStatus.Pending;
            }
        }
    }
}
