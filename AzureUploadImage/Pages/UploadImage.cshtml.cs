using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureUploadImage.Pages
{
    public class UploadImageModel : PageModel
    {
        private CloudBlobClient _blobClient;
        private readonly IConfiguration _config;
        
        public UploadImageModel(IConfiguration config)
        {
            _config = config;

            //var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var appSettingSection = _config.GetSection("AppSettings");
            var storageAccount = CloudStorageAccount.Parse(appSettingSection["UploadStorageConnectionString"]);
            _blobClient = storageAccount.CreateCloudBlobClient();


        }
        public async Task OnGet()
        {
            var containerRef = _blobClient.GetContainerReference("images");
            await containerRef.CreateIfNotExistsAsync();
            await containerRef.SetPermissionsAsync(new BlobContainerPermissions() { PublicAccess = BlobContainerPublicAccessType.Blob });

            BlobContinuationToken blobContinuationToken = null;
            List<IListBlobItem> listBlobItems = new List<IListBlobItem>();

            do
            {
                var response = await containerRef.ListBlobsSegmentedAsync(blobContinuationToken);
                blobContinuationToken = response.ContinuationToken;
                listBlobItems.AddRange(response.Results);
            }
            while (blobContinuationToken != null);

            ViewData["ImgList"] = listBlobItems;
        }

        public async Task<IActionResult> OnPost(IFormFile imageFile)
        {
            string blobName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);

            var containerRef = _blobClient.GetContainerReference("images");
            await containerRef.CreateIfNotExistsAsync();
            await containerRef.SetPermissionsAsync(new BlobContainerPermissions() { PublicAccess = BlobContainerPublicAccessType.Blob });
            CloudBlockBlob blob = containerRef.GetBlockBlobReference(blobName);
            //await blob.UploadFromFileAsync(imageFile.FileName);
            using (Stream stream = imageFile.OpenReadStream())
            {
                //MemoryStream memoryStream = new MemoryStream();
                //await memoryStream.WriteAsync(image.ToByteArray(), 0, image.ToByteArray().Count());
                await blob.UploadFromStreamAsync(stream);
            }

            return new RedirectToPageResult("UploadImage");

        }
    }
}