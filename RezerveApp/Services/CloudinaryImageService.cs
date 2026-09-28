using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace RezerveApp.Services
{
    public class CloudinaryImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryImageService(IConfiguration configuration)
        {
            var cloudinaryUrl = configuration["Cloudinary:Url"];
            if (!string.IsNullOrEmpty(cloudinaryUrl))
            {
                _cloudinary = new Cloudinary(cloudinaryUrl);
                _cloudinary.Api.Secure = true;
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder = "rezerveapp")
        {
            if (_cloudinary == null || file == null || file.Length == 0)
                return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception(uploadResult.Error.Message);

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (_cloudinary == null || string.IsNullOrEmpty(publicId))
                return false;

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            return result.Result == "ok";
        }
    }
}
