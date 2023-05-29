﻿using MessengerServer.Services.Interfaces;

namespace MessengerServer.Services
{
    public class FilesProvider : IFilesProvider
    {
        private readonly IConfiguration configuration;

        private string filesDirectory;

        public FilesProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
            filesDirectory = configuration.GetValue<string>("Files:Directory")!;
        }

        public Task<Stream?> GetFileAsync(string relativePath)
        {
            var fullPath = Path.Combine(filesDirectory, relativePath);

            if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);

            var stream = File.OpenRead(fullPath);
            return Task.FromResult<Stream?>(stream);
        }

        public async Task UpdateFileAsync(Stream file, string relativePath)
        {
            var fullPath = Path.Combine(filesDirectory, relativePath);

            FileStream fileStream;
            if (!File.Exists(fullPath)) fileStream = File.Create(fullPath);
            else fileStream = File.Open(fullPath, FileMode.Truncate);

            file.Position = 0;
            await file.CopyToAsync(fileStream);

            fileStream.Flush();
            fileStream.Dispose();
        }
    }
}
