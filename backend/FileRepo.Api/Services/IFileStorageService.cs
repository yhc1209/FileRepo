using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FileRepo.Api.Models;

namespace FileRepo.Api.Services;

public interface IFileStorageService
{
    Task<FileMetadata> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<IEnumerable<FileMetadata>> GetAllFilesAsync(CancellationToken cancellationToken = default);
    Task<FileMetadata?> GetFileMetadataAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(Guid id, CancellationToken cancellationToken = default);
    string GetPhysicalPath(FileMetadata metadata);
}
