namespace FileUpload.API.Models
{
    public class AwsSettings
    {
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
    }

    public class UploadResult
    {
        public string FileKey { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }

    public class S3FileInfo
    {
        public string Key { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    // Request model for pre-signed URL
    public class PresignedUrlRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }

    public class PresignedUrlResponse
    {
        public string PresignedUrl { get; set; } = string.Empty;
        public string FileKey { get; set; } = string.Empty;
        public string PublicUrl { get; set; } = string.Empty;
    }
}
