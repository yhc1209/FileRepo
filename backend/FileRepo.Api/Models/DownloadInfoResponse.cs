namespace FileRepo.Api.Models;

public class DownloadInfoResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}
