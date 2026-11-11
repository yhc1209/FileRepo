import { Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileUploadComponent } from '../file-upload/file-upload.component';
import { FileListComponent } from '../file-list/file-list.component';

@Component({
  selector: 'app-files-page',
  standalone: true,
  imports: [CommonModule, FileUploadComponent, FileListComponent],
  template: `
    <app-file-upload (uploadSuccess)="onUploadSuccess()"></app-file-upload>
    <app-file-list #fileList></app-file-list>
  `
})
export class FilesPageComponent {
  @ViewChild('fileList') fileList!: FileListComponent;

  onUploadSuccess(): void {
    if (this.fileList) {
      this.fileList.loadFiles();
    }
  }
}
