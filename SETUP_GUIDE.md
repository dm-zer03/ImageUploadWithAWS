# File Upload: Angular + .NET Core + AWS S3
## Complete Setup Guide

---

## Project Structure

```
file-upload/
├── backend/                          ← .NET Core Web API
│   ├── Controllers/
│   │   └── FileUploadController.cs   ← API endpoints
│   ├── Services/
│   │   └── S3Service.cs              ← AWS S3 logic
│   ├── Models/
│   │   └── Models.cs                 ← DTOs / config models
│   ├── Program.cs                    ← DI + middleware setup
│   └── appsettings.json              ← AWS credentials/config
│
└── frontend/                         ← Angular App
    └── src/app/
        ├── services/
        │   └── file-upload.service.ts
        └── components/file-upload/
            ├── file-upload.component.ts
            ├── file-upload.component.html
            └── file-upload.component.scss
```

---

## Step 1 — AWS S3 Setup

### 1.1 Create S3 Bucket
- Go to **AWS Console → S3 → Create Bucket**
- Bucket name: `your-app-uploads` (globally unique)
- Region: `ap-south-1` (Mumbai)
- **Uncheck** "Block all public access" if files need to be viewable (optional)
- Enable **Versioning** (optional but recommended)

### 1.2 Configure CORS on S3 Bucket
Go to **Bucket → Permissions → CORS** and paste:

```json
[
  {
    "AllowedHeaders": ["*"],
    "AllowedMethods": ["GET", "PUT", "POST", "DELETE"],
    "AllowedOrigins": ["http://localhost:4200", "https://yourdomain.com"],
    "ExposeHeaders": ["ETag"]
  }
]
```

### 1.3 Create IAM User for API
- Go to **IAM → Users → Create User**
- Attach policy: **AmazonS3FullAccess** (or create a scoped policy)
- Generate **Access Key** + **Secret Key**
- Paste into `appsettings.json`

---

## Step 2 — .NET Core Backend Setup

### 2.1 Install NuGet Package
```bash
cd backend
dotnet add package AWSSDK.S3
```

### 2.2 Update appsettings.json
```json
"AWS": {
  "BucketName": "your-app-uploads",
  "Region": "ap-south-1",
  "AccessKey": "AKIA...",
  "SecretKey": "your-secret-key"
}
```

### 2.3 Run the API
```bash
dotnet run
# API available at: https://localhost:7001
# Swagger UI: https://localhost:7001/swagger
```

---

## Step 3 — Angular Frontend Setup

### 3.1 Install Dependencies
```bash
cd frontend
npm install
```

### 3.2 Add HttpClientModule
In `app.config.ts` (standalone) or `app.module.ts`:
```typescript
import { provideHttpClient } from '@angular/common/http';

// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient()   // ← Add this
  ]
};
```

### 3.3 Use the Component
```typescript
// In your routes or parent component
import { FileUploadComponent } from './components/file-upload/file-upload.component';
```

```html
<!-- In your template -->
<app-file-upload></app-file-upload>
```

### 3.4 Update API URL
In `file-upload.service.ts`, update:
```typescript
private apiUrl = 'https://localhost:7001/api/fileupload';
```

---

## API Endpoints Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/fileupload/presigned-url` | Get S3 pre-signed PUT URL |
| POST | `/api/fileupload/upload` | Upload file via server |
| GET | `/api/fileupload/files` | List all uploaded files |
| DELETE | `/api/fileupload/files/{fileKey}` | Delete a file |

---

## Upload Flow (Pre-signed URL — Recommended)

```
Angular                  .NET API              AWS S3
  │                          │                    │
  │── POST /presigned-url ──►│                    │
  │   { fileName, type }     │── GenerateURL ────►│
  │                          │◄─ presignedUrl ────│
  │◄── { presignedUrl } ─────│                    │
  │                          │                    │
  │── PUT presignedUrl ──────────────────────────►│
  │   (file bytes directly)  │                    │
  │◄─────────────────────────────────── 200 OK ───│
```

**Why pre-signed URL is better:**
- File never passes through your server → faster, less bandwidth cost
- Supports large files easily
- S3 handles the load, not your API

---

## Security Checklist

- [ ] Store AWS keys in **AWS Secrets Manager** or environment variables, not appsettings.json in production
- [ ] Use **IAM roles** when deploying on EC2/ECS instead of access keys
- [ ] Restrict bucket policy to only allow your API's IAM user
- [ ] Add **file type validation** on both frontend and backend
- [ ] Set **max file size** limits (currently 50MB)
- [ ] Enable **S3 server-side encryption** (SSE-S3 or SSE-KMS)
- [ ] Use **CloudFront** CDN in front of S3 for production file serving
