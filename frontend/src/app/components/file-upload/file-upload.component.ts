import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpEventType } from '@angular/common/http';
import { FileService } from '../../services/file.service';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-upload.component.html',
  styleUrls: ['./file-upload.component.css']
})
export class FileUploadComponent {
  selectedFile: File | null = null;
  progress = 0;
  isUploading = false;
  message = '';

  @Output() uploadSuccess = new EventEmitter<void>();

  constructor(private fileService: FileService) {}

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      this.progress = 0;
      this.message = '';
    }
  }

  upload(): void {
    if (!this.selectedFile) return;

    this.isUploading = true;
    this.progress = 0;
    this.message = '';

    this.fileService.uploadFile(this.selectedFile).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          this.progress = Math.round((100 * event.loaded) / event.total);
        } else if (event.type === HttpEventType.Response) {
          this.message = '檔案上傳成功！';
          this.isUploading = false;
          this.selectedFile = null;
          this.uploadSuccess.emit();
        }
      },
      error: (err) => {
        this.message = '上傳失敗：' + (err.error?.message || err.statusText || '未知錯誤');
        this.isUploading = false;
      }
    });
  }
}
