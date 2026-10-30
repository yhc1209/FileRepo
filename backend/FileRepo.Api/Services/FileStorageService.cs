using System.Security.Cryptography;
using FileRepo.Api.Data;
using FileRepo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FileRepo.Api.Services;

public class FileStorageService : IFileStorageService
{
    private readonly FileDbContext _dbContext;
    private readonly string _storageFolder;

    public FileStorageService(FileDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _storageFolder = configuration["StoragePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(_storageFolder))
        {
            Directory.CreateDirectory(_storageFolder);
        }
    }

    public async Task<FileMetadata> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var extension = Path.GetExtension(file.FileName);
        var storageFileName = $"{id}{extension}";
        var physicalPath = Path.Combine(_storageFolder, storageFileName);

        string hashHex;
        using (var sha256 = SHA256.Create())
        using (var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var readStream = file.OpenReadStream())
        {
            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await readStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            hashHex = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
        }

        var metadata = new FileMetadata
        {
            Id = id,
            FileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FileSizeBytes = file.Length,
            Sha256Hash = hashHex,
            UploadDate = DateTime.UtcNow,
            StoragePath = physicalPath
        };

        _dbContext.Files.Add(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return metadata;
    }

    public async Task<IEnumerable<FileMetadata>> GetAllFilesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Files.OrderByDescending(f => f.UploadDate).ToListAsync(cancellationToken);
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<bool> DeleteFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metadata = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (metadata == null) return false;

        if (File.Exists(metadata.StoragePath))
        {
            File.Delete(metadata.StoragePath);
        }

        _dbContext.Files.Remove(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public string GetPhysicalPath(FileMetadata metadata) => metadata.StoragePath;
}
