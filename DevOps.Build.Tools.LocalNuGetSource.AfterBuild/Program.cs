﻿using Microsoft.WindowsAzure.Storage;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DevOps.ContentDelivery.Functions.UploadAzureBlob.BlobUploader;
using static System.Environment;
using static System.IO.Compression.ZipFile;
using static System.IO.Directory;
using static System.IO.File;
using static System.IO.Path;

namespace DevOps.Build.Tools.LocalNuGetSource.AfterBuild
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var token = args.FirstOrDefault() ?? throw new ArgumentNullException("NamespacePrefix");
            var containerName = args[1] ?? throw new ArgumentNullException("ContainerName");
            var tmpDir = args[2] ?? throw new ArgumentNullException("TempDirectory");

            var account = CloudStorageAccount.Parse(
                GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"));
            var container = account.CreateCloudBlobClient().GetContainerReference(containerName);

            // Upload each nupkg to CDN
            var dir = GetEnvironmentVariable("APPVEYOR_BUILD_FOLDER");
            var files = EnumerateFiles(dir, "*.nupkg", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var name = file.Split('/').Last();
                var zip = GetTempFileName();
                Console.WriteLine($"Uploading {zip}");
                Delete(tmpDir, true);
                CreateDirectory(tmpDir);
                Copy(file, Combine(tmpDir, name));
                CreateFromDirectory(tmpDir, zip);
                await Upload(container, name, zip);
                File.Delete(zip);
            }
        }
    }
}
