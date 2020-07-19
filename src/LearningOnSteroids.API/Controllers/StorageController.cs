using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using LearningOnSteroids.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LearningOnSteroids.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class StorageController : ControllerBase
    {
        private ILearningVideoStorage _learningVideoStorage;
        public StorageController(ILearningVideoStorage learningVideoStorage)
        {
            _learningVideoStorage = learningVideoStorage;
        }

        [HttpPost("{blobname}")]
        public async Task<ActionResult> UploadVideoAsync(string blobname, string title, string description)
        {
            byte[] videoByteArray = FileToByteArray("C:\\StorageTest\\AUSM014ABN20.pdf");

            var exists = await _learningVideoStorage.CheckIfBlobExistsAsync(blobname);

            if (exists)
                return Ok();

            var cloudBlockBlob = await _learningVideoStorage.UploadVideoAsync(videoByteArray, blobname, title, description);

            return Ok();
        }

        [HttpGet()]
        public async Task<ActionResult> ListVideoBlobsAsync(string prefix)
        {
            var vm = await _learningVideoStorage.ListVideoBlobsAsync(prefix);

            return Ok(vm.ToList().Select(x => new 
            { 
                x.Name, 
                x.Uri, 
                Title = x.Metadata.ContainsKey("title") ? x.Metadata["title"] : "", 
                Description = x.Metadata.ContainsKey("description") ? x.Metadata["description"] : "" 
            }));
        }

        [HttpGet("{blobname}")]
        public async Task<ActionResult> DownloadVideoAsync(string blobname)
        {
            var cloudBlockBlob = await _learningVideoStorage.GetCloudBlockBlobAsync(blobname);

            var streamToWrite = new MemoryStream();
            await _learningVideoStorage.DownloadVideoAsync(cloudBlockBlob, streamToWrite);

            //await _learningVideoStorage.UploadVideoAsync(streamToWrite.ToArray(), "test/test.pdf");

            return Ok();
        }

        [HttpGet("{blobname}")]
        public async Task<ActionResult> GetBlobMetadata(string blobname)
        {
            var cloudBlockBlob = await _learningVideoStorage.GetCloudBlockBlobAsync(blobname);

            await _learningVideoStorage.ReloadMetadataAsync(cloudBlockBlob);

            var (title, description) = _learningVideoStorage.GetBlobMetadata(cloudBlockBlob);

            return Ok(new { Title = title, Description = description });
        }

        [HttpDelete("{blobname}")]
        public async Task<ActionResult> DeleteVideoAsync(string blobname)
        {
            var cloudBlockBlob = await _learningVideoStorage.GetCloudBlockBlobAsync(blobname);

            await _learningVideoStorage.DeleteVideoAsync(cloudBlockBlob);

            return Ok();
        }

        [HttpPost("{blobname}")]
        public async Task<ActionResult> UpdateMetadataAsync(string blobname, string title, string description)
        {
            var cloudBlockBlob = await _learningVideoStorage.GetCloudBlockBlobAsync(blobname);

            await _learningVideoStorage.UpdateMetadataAsync(cloudBlockBlob, title, description);

            return Ok();
        }

        [HttpGet("{blobname}")]
        public async Task<ActionResult> GetBlobUriWithSasToken(string blobname)
        {
            var cloudBlockBlob = await _learningVideoStorage.GetCloudBlockBlobAsync(blobname);

            var url = _learningVideoStorage.GetBlobUriWithSasToken(cloudBlockBlob);

            return Ok(url);
        }

        [HttpPost("{blobname}")]
        public async Task<ActionResult> ArchiveVideoAsync(string blobname)
        {
            var cloudBlockBlob = await _learningVideoStorage.GetCloudBlockBlobAsync(blobname);

            await _learningVideoStorage.ArchiveVideoAsync(cloudBlockBlob);

            return Ok();
        }

        private byte[] FileToByteArray(string fileName)
        {
            byte[] buff = null;
            FileStream fs = new FileStream(fileName,
                                           FileMode.Open,
                                           FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            long numBytes = new FileInfo(fileName).Length;
            buff = br.ReadBytes((int)numBytes);
            return buff;
        }
    }
}