import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FileItem, DownloadInfoResponse } from '../models/file-item.model';

@Injectable({
  providedIn: 'root'
})
export class FileService {
  private apiUrl = '/api/files';

  constructor(private http: HttpClient) {}

  getFiles(): Observable<FileItem[]> {
    return this.http.get<FileItem[]>(this.apiUrl);
  }

  uploadFile(file: File): Observable<HttpEvent<FileItem>> {
    const formData = new FormData();
    formData.append('file', file);

    const req = new HttpRequest('POST', `${this.apiUrl}/upload`, formData, {
      reportProgress: true
    });

    return this.http.request<FileItem>(req);
  }

  getDownloadInfo(fileId: string): Observable<DownloadInfoResponse> {
    return this.http.post<DownloadInfoResponse>(`${this.apiUrl}/download-info`, { fileId });
  }

  getDownloadUrl(fileId: string): string {
    return `${this.apiUrl}/${fileId}/download`;
  }

  deleteFile(fileId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${fileId}`);
  }
}
