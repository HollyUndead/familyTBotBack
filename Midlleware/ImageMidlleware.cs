using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace familyTBotBack.Midlleware
{
    public class ImageMiddleware
    {
        private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
        private const long MaxSize = 5*1024*1024;

        private readonly Cloudinary _cloudinary;

        public ImageMiddleware(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }
        public async Task<string> PostImageFromFile(IFormFile file, string folder = "familyTBot", string? publicId = null)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("No file");
            if (file.Length > MaxSize) throw new ArgumentException("File is too large (max 5MB)");
            if (!AllowedTypes.Contains(file.ContentType)) throw new ArgumentException("Unsupported file type");
            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams { File = new FileDescription(file.FileName, stream), Overwrite = false };
            if(publicId is null)
            {
                uploadParams.Folder = folder;
            }
            else
            {
                uploadParams.PublicId = $"{folder}/{publicId}";
                uploadParams.UniqueFilename = false;
            }
            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null) return "{500, result.Error.Message}";

            return result.SecureUrl.ToString();
        }
    }
}
