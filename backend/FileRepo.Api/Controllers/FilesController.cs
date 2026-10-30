using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FileRepo.Api.Models;
using FileRepo.Api.Services;

namespace FileRepo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _storageService;

    public FilesController(IFileStorageService storageService)
    {
        _storageService = storageService;
    }

    /// <summary>
    /// GUI 檔案上傳
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(1073741824)] // 1GB
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "請選擇有效的檔案進行上傳。" });
        }

        var metadata = await _storageService.SaveFileAsync(file, ct);
        return Ok(metadata);
    }

    /// <summary>
    /// 擴充功能 1：枚舉 (GET) - 查詢 Server 有的檔案相關資訊列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> Enumeration(CancellationToken ct)
    {
        var files = await _storageService.GetAllFilesAsync(ct);
        return Ok(files);
    }

    /// <summary>
    /// 擴充功能 2：下載資訊 (POST) - 取得指定檔案的下載資訊與連結
    /// </summary>
    [HttpPost("download-info")]
    public async Task<ActionResult<DownloadInfoResponse>> GetDownloadInfo([FromBody] DownloadInfoRequest request, CancellationToken ct)
    {
        if (request == null || request.FileId == Guid.Empty)
        {
            return BadRequest(new { message = "請提供有效的 FileId。" });
        }

        var metadata = await _storageService.GetFileMetadataAsync(request.FileId, ct);
        if (metadata == null)
        {
            return NotFound(new { message = "找不到指定的檔案。" });
        }

        var requestScheme = Request.Scheme;
        var requestHost = Request.Host.ToUriComponent();
        var downloadUrl = $"{requestScheme}://{requestHost}/api/files/{metadata.Id}/download";

        var response = new DownloadInfoResponse
        {
            Id = metadata.Id,
            FileName = metadata.FileName,
            ContentType = metadata.ContentType,
            FileSizeBytes = metadata.FileSizeBytes,
            Sha256Hash = metadata.Sha256Hash,
            UploadDate = metadata.UploadDate,
            DownloadUrl = downloadUrl
        };

        return Ok(response);
    }

    /// <summary>
    /// 擴充功能 3：下載檔案 (GET / HEAD) - 支援斷點續傳 (HTTP Range processing)
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [HttpHead("{id:guid}/download")]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken ct)
    {
        var metadata = await _storageService.GetFileMetadataAsync(id, ct);
        if (metadata == null)
        {
            return NotFound(new { message = "找不到指定的檔案。" });
        }

        var path = _storageService.GetPhysicalPath(metadata);
        if (!System.IO.File.Exists(path))
        {
            return NotFound(new { message = "實體檔案不存在。" });
        }

        // enableRangeProcessing: true 原生支援 206 Partial Content, Accept-Ranges: bytes, 以及 HEAD 方法
        return PhysicalFile(path, metadata.ContentType, metadata.FileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// 刪除檔案 (GUI 刪除功能)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFile(Guid id, CancellationToken ct)
    {
        var deleted = await _storageService.DeleteFileAsync(id, ct);
        if (!deleted)
        {
            return NotFound(new { message = "找不到要刪除的檔案。" });
        }

        return NoContent();
    }
}
