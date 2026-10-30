import { Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileUploadComponent } from './components/file-upload/file-upload.component';
import { FileListComponent } from './components/file-list/file-list.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FileUploadComponent, FileListComponent],
  templateUrl: './app.component.html'
})
export class AppComponent {
  @ViewChild(FileListComponent) fileListComponent!: FileListComponent;

  onUploadSuccess(): void {
    if (this.fileListComponent) {
      this.fileListComponent.loadFiles();
    }
  }
}
