using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LearningOnSteroids.Storage
{
    public interface ILearningVideoStorage
    {
        Task<CloudBlockBlob> UploadVideoAsync(byte[] videoByteArray, string blobname, string title, string description);
        Task<bool> CheckIfBlobExistsAsync(string blobname);

        Task<IEnumerable<CloudBlockBlob>> ListVideoBlobsAsync(string prefix = null);

        Task<CloudBlockBlob> GetCloudBlockBlobAsync(string blobname);

        Task DownloadVideoAsync(CloudBlockBlob cloudBlockBlob, Stream targetStream);
        Task OverwriteVideoAsync(CloudBlockBlob cloudBlockBlob, byte[] videoByteArray);
        Task DeleteVideoAsync(CloudBlockBlob cloudBlockBlob);

        Task UpdateMetadataAsync(CloudBlockBlob cloudBlockBlob, string title, string description);
        Task ReloadMetadataAsync(CloudBlockBlob cloudBlockBlob);
        (string title, string description) GetBlobMetadata(CloudBlockBlob cloudBlockBlob);

        string GetBlobUriWithSasToken(CloudBlockBlob cloudBlockBlob);

        Task ArchiveVideoAsync(CloudBlockBlob cloudBlockBlob);
    }
}
