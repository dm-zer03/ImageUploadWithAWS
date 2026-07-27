using FileUpload.API.Models;
using FileUpload.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileUpload.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly IS3Service _s3Service;
        private readonly ILogger<FileUploadController> _logger;

        // Allowed file types (customize as needed)
        private static readonly string[] AllowedContentTypes =
        [
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "text/plain", "text/csv"
        ];

        private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

        public FileUploadController(IS3Service s3Service, ILogger<FileUploadController> logger)
        {
            _s3Service = s3Service;
            _logger = logger;
        }

        // ─── APPROACH B: Get Pre-signed URL → Angular uploads directly to S3 ──
        // POST api/fileupload/presigned-url
        [HttpPost("presigned-url")]
        public async Task<IActionResult> GetPresignedUrl([FromBody] PresignedUrlRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest(new { error = "FileName is required." });

            if (!AllowedContentTypes.Contains(request.ContentType))
                return BadRequest(new { error = $"File type '{request.ContentType}' is not allowed." });

            try
            {
                var fileKey = $"uploads/{Guid.NewGuid()}_{request.FileName}";
                var presignedUrl = await _s3Service.GeneratePresignedUrlAsync(request.FileName, request.ContentType);

                // Extract bucket/region from the URL to build public URL
                _logger.LogInformation("Presigned URL generated for file: {FileName}", request.FileName);

                return Ok(new PresignedUrlResponse
                {
                    PresignedUrl = presignedUrl,
                    FileKey = fileKey
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating presigned URL");
                return StatusCode(500, new { error = "Failed to generate upload URL." });
            }
        }

        // ─── APPROACH A: Server-side Upload (API → S3) ────────────────────────
        // POST api/fileupload/upload
        [HttpPost("upload")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file provided." });

            if (file.Length > MaxFileSizeBytes)
                return BadRequest(new { error = "File exceeds maximum allowed size of 50MB." });

            if (!AllowedContentTypes.Contains(file.ContentType))
                return BadRequest(new { error = $"File type '{file.ContentType}' is not allowed." });

            try
            {
                var result = await _s3Service.UploadFileAsync(file);
                _logger.LogInformation("File uploaded: {FileName} → {FileKey}", file.FileName, result.FileKey);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
                return StatusCode(500, new { error = "File upload failed. Please try again." });
            }
        }

        // ─── List uploaded files ──────────────────────────────────────────────
        // GET api/fileupload/files
        [HttpGet("files")]
        public async Task<IActionResult> GetFiles()
        {
            try
            {
                var files = await _s3Service.ListFilesAsync();
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files");
                return StatusCode(500, new { error = "Failed to retrieve files." });
            }
        }

        // ─── Delete a file ────────────────────────────────────────────────────
        // DELETE api/fileupload/files/{fileKey}
        [HttpDelete("files/{fileKey}")]
        public async Task<IActionResult> DeleteFile(string fileKey)
        {
            var decodedKey = Uri.UnescapeDataString(fileKey);
            var success = await _s3Service.DeleteFileAsync(decodedKey);

            if (!success)
                return NotFound(new { error = "File not found or could not be deleted." });

            return Ok(new { message = "File deleted successfully." });
        }
    }
}
