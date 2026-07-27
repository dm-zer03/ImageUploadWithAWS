using Amazon.S3;
using Amazon.S3.Model;
using FileUpload.API.Models;
using Microsoft.Extensions.Options;

namespace FileUpload.API.Services
{
    public interface IS3Service
    {
        Task<string> GeneratePresignedUrlAsync(string fileName, string contentType);
        Task<UploadResult> UploadFileAsync(IFormFile file);
        Task<bool> DeleteFileAsync(string fileKey);
        Task<List<S3FileInfo>> ListFilesAsync();
    }

    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly AwsSettings _awsSettings;

        public S3Service(IAmazonS3 s3Client, IOptions<AwsSettings> awsSettings)
        {
            _s3Client = s3Client;
            _awsSettings = awsSettings.Value;
        }

        // ─── APPROACH B: Pre-signed URL (Angular uploads directly to S3) ───────
        public async Task<string> GeneratePresignedUrlAsync(string fileName, string contentType)
        {
            var fileKey = $"uploads/{Guid.NewGuid()}_{SanitizeFileName(fileName)}";

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _awsSettings.BucketName,
                Key = fileKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(10),
                ContentType = contentType
            };

            var presignedUrl = await _s3Client.GetPreSignedURLAsync(request);

            return presignedUrl;
        }

        // ─── APPROACH A: Server-side upload (API receives file, uploads to S3) ─
        public async Task<UploadResult> UploadFileAsync(IFormFile file)
        {
            var fileKey = $"uploads/{Guid.NewGuid()}_{SanitizeFileName(file.FileName)}";

            using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _awsSettings.BucketName,
                Key = fileKey,
                InputStream = stream,
                ContentType = file.ContentType,
                // Makes the file publicly readable (remove if private)
                // CannedACL = S3CannedACL.PublicRead
            };

            await _s3Client.PutObjectAsync(request);

            var fileUrl = $"https://{_awsSettings.BucketName}.s3.{_awsSettings.Region}.amazonaws.com/{fileKey}";

            return new UploadResult
            {
                FileKey = fileKey,
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> DeleteFileAsync(string fileKey)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _awsSettings.BucketName,
                    Key = fileKey
                };
                await _s3Client.DeleteObjectAsync(request);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<S3FileInfo>> ListFilesAsync()
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _awsSettings.BucketName,
                Prefix = "uploads/"
            };

            var response = await _s3Client.ListObjectsV2Async(request);

            return response.S3Objects.Select(obj => new S3FileInfo
            {
                Key = obj.Key,
                FileName = obj.Key.Split('/').Last(),
                FileSize = obj.Size,
                LastModified = obj.LastModified,
                Url = $"https://{_awsSettings.BucketName}.s3.{_awsSettings.Region}.amazonaws.com/{obj.Key}"
            }).ToList();
        }

        private static string SanitizeFileName(string fileName)
            => string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }
}
