import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileService } from '../../services/file.service';
import { FileItem } from '../../models/file-item.model';

@Component({
  selector: 'app-file-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-list.component.html',
  styleUrls: ['./file-list.component.css']
})
export class FileListComponent implements OnInit {
  files: FileItem[] = [];
  isLoading = false;
  copiedHashId: string | null = null;
  copiedInfoId: string | null = null;

  constructor(private fileService: FileService) {}

  ngOnInit(): void {
    this.loadFiles();
  }

  loadFiles(): void {
    this.isLoading = true;
    this.fileService.getFiles().subscribe({
      next: (data) => {
        this.files = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('無法載入檔案列表', err);
        this.isLoading = false;
      }
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  downloadFile(file: FileItem): void {
    const url = this.fileService.getDownloadUrl(file.id);
    window.open(url, '_blank');
  }

  copyHash(file: FileItem): void {
    navigator.clipboard.writeText(file.sha256Hash).then(() => {
      this.copiedHashId = file.id;
      setTimeout(() => (this.copiedHashId = null), 2000);
    });
  }

  copyDownloadInfo(file: FileItem): void {
    this.fileService.getDownloadInfo(file.id).subscribe({
      next: (info) => {
        const infoText = JSON.stringify(info, null, 2);
        navigator.clipboard.writeText(infoText).then(() => {
          this.copiedInfoId = file.id;
          setTimeout(() => (this.copiedInfoId = null), 2000);
        });
      },
      error: (err) => alert('取得下載資訊失敗：' + err.message)
    });
  }

  deleteFile(file: FileItem): void {
    if (confirm(`確定要刪除檔案 "${file.fileName}" 嗎？`)) {
      this.fileService.deleteFile(file.id).subscribe({
        next: () => this.loadFiles(),
        error: (err) => alert('刪除檔案失敗')
      });
    }
  }
}
