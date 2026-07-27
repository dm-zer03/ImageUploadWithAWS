import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FileUploadService,
  S3FileInfo,
  UploadProgress
} from '../../services/file-upload.service';

interface FileItem {
  file: File;
  progress: number;
  status: 'pending' | 'uploading' | 'complete' | 'error';
  error?: string;
  result?: any;
  preview?: string;
}

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-upload.component.html',
  styleUrls: ['./file-upload.component.scss']
})
export class FileUploadComponent implements OnInit {

  fileItems: FileItem[] = [];
  uploadedFiles: S3FileInfo[] = [];
  isDragOver = false;
  isLoadingFiles = false;

  // Accepted types (matches backend AllowedContentTypes)
  acceptedTypes = '.jpg,.jpeg,.png,.gif,.webp,.pdf,.docx,.xlsx,.txt,.csv';
  maxFileSizeMB = 50;

  constructor(private uploadService: FileUploadService) {}

  ngOnInit() {
    this.loadUploadedFiles();
  }

  // ─── Drag & Drop handlers ─────────────────────────────────────────────────
  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDragLeave() {
    this.isDragOver = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = false;
    const files = Array.from(event.dataTransfer?.files ?? []);
    this.addFiles(files);
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.addFiles(Array.from(input.files));
      input.value = ''; // reset so same file can be re-selected
    }
  }

  // ─── File validation & queuing ────────────────────────────────────────────
  addFiles(files: File[]) {
    for (const file of files) {
      if (file.size > this.maxFileSizeMB * 1024 * 1024) {
        this.fileItems.push({ file, progress: 0, status: 'error', error: `Exceeds ${this.maxFileSizeMB}MB limit` });
        continue;
      }
      const item: FileItem = { file, progress: 0, status: 'pending' };
      if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = e => item.preview = e.target?.result as string;
        reader.readAsDataURL(file);
      }
      this.fileItems.push(item);
    }
  }

  uploadAll() {
    const pending = this.fileItems.filter(f => f.status === 'pending');
    pending.forEach(item => this.uploadFile(item));
  }

  // ─── Upload a single file (using Pre-signed URL approach) ─────────────────
  uploadFile(item: FileItem) {
    item.status = 'uploading';
    item.progress = 0;

    this.uploadService.uploadViaPresignedUrl(item.file).subscribe({
      next: (progressEvent: UploadProgress) => {
        item.progress = progressEvent.progress;
        if (progressEvent.status === 'complete') {
          item.status = 'complete';
          item.result = progressEvent.result;
          this.loadUploadedFiles(); // refresh list
        } else if (progressEvent.status === 'error') {
          item.status = 'error';
          item.error = progressEvent.error ?? 'Upload failed';
        }
      },
      error: (err) => {
        item.status = 'error';
        item.error = err.message ?? 'Upload failed';
      }
    });
  }

  removeItem(index: number) {
    this.fileItems.splice(index, 1);
  }

  clearCompleted() {
    this.fileItems = this.fileItems.filter(f => f.status !== 'complete');
  }

  // ─── Uploaded files list ──────────────────────────────────────────────────
  loadUploadedFiles() {
    this.isLoadingFiles = true;
    this.uploadService.getFiles().subscribe({
      next: files => {
        this.uploadedFiles = files;
        this.isLoadingFiles = false;
      },
      error: () => this.isLoadingFiles = false
    });
  }

  deleteFile(file: S3FileInfo) {
    if (!confirm(`Delete ${file.fileName}?`)) return;
    this.uploadService.deleteFile(file.key).subscribe({
      next: () => this.loadUploadedFiles()
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  getFileIcon(fileName: string): string {
    const ext = fileName.split('.').pop()?.toLowerCase();
    const icons: Record<string, string> = {
      pdf: '📄', docx: '📝', xlsx: '📊', csv: '📊',
      jpg: '🖼️', jpeg: '🖼️', png: '🖼️', gif: '🖼️', webp: '🖼️',
      txt: '📃'
    };
    return icons[ext ?? ''] ?? '📁';
  }
}
