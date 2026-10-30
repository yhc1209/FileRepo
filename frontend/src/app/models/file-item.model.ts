export interface FileItem {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  sha256Hash: string;
  uploadDate: string;
  storagePath?: string;
}

export interface DownloadInfoResponse {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  sha256Hash: string;
  uploadDate: string;
  downloadUrl: string;
}
