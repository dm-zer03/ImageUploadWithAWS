import { Injectable } from '@angular/core';
import { HttpClient, HttpEventType, HttpRequest } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, filter, map } from 'rxjs/operators';

export interface UploadResult {
  fileKey: string;
  fileUrl: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  uploadedAt: string;
}

export interface PresignedUrlResponse {
  presignedUrl: string;
  fileKey: string;
}

export interface S3FileInfo {
  key: string;
  fileName: string;
  fileSize: number;
  lastModified: string;
  url: string;
}

export interface UploadProgress {
  progress: number;       // 0–100
  status: 'uploading' | 'complete' | 'error';
  result?: UploadResult;
  error?: string;
}

@Injectable({ providedIn: 'root' })
export class FileUploadService {

  private apiUrl = 'https://localhost:7001/api/fileupload';

  constructor(private http: HttpClient) {}

  // ─── APPROACH B (Recommended): Pre-signed URL upload ─────────────────────
  uploadViaPresignedUrl(file: File): Observable<UploadProgress> {
    // Step 1: Get pre-signed URL from .NET API
    return new Observable(observer => {
      this.http.post<PresignedUrlResponse>(`${this.apiUrl}/presigned-url`, {
        fileName: file.name,
        contentType: file.type
      }).subscribe({
        next: (response) => {
          // Step 2: Upload directly to S3 using the pre-signed URL
          const req = new HttpRequest('PUT', response.presignedUrl, file, {
            reportProgress: true,
            headers: { 'Content-Type': file.type }
          });

          this.http.request(req).subscribe({
            next: (event) => {
              if (event.type === HttpEventType.UploadProgress && event.total) {
                const progress = Math.round(100 * event.loaded / event.total);
                observer.next({ progress, status: 'uploading' });
              } else if (event.type === HttpEventType.Response) {
                observer.next({
                  progress: 100,
                  status: 'complete',
                  result: {
                    fileKey: response.fileKey,
                    fileUrl: '', // build from fileKey if needed
                    fileName: file.name,
                    fileSize: file.size,
                    contentType: file.type,
                    uploadedAt: new Date().toISOString()
                  }
                });
                observer.complete();
              }
            },
            error: (err) => observer.next({ progress: 0, status: 'error', error: err.message })
          });
        },
        error: (err) => observer.next({ progress: 0, status: 'error', error: err.message })
      });
    });
  }

  // ─── APPROACH A: Server-side upload (API → S3) ────────────────────────────
  uploadViaServer(file: File): Observable<UploadProgress> {
    const formData = new FormData();
    formData.append('file', file);

    const req = new HttpRequest('POST', `${this.apiUrl}/upload`, formData, {
      reportProgress: true
    });

    return this.http.request(req).pipe(
      filter(event =>
        event.type === HttpEventType.UploadProgress ||
        event.type === HttpEventType.Response
      ),
      map(event => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          return {
            progress: Math.round(100 * event.loaded / event.total),
            status: 'uploading' as const
          };
        } else if (event.type === HttpEventType.Response) {
          return {
            progress: 100,
            status: 'complete' as const,
            result: event.body as UploadResult
          };
        }
        return { progress: 0, status: 'uploading' as const };
      }),
      catchError(err => throwError(() => err))
    );
  }

  getFiles(): Observable<S3FileInfo[]> {
    return this.http.get<S3FileInfo[]>(`${this.apiUrl}/files`);
  }

  deleteFile(fileKey: string): Observable<void> {
    const encodedKey = encodeURIComponent(fileKey);
    return this.http.delete<void>(`${this.apiUrl}/files/${encodedKey}`);
  }
}
