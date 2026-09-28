using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace RezerveApp.Services
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder = "rezerveapp");
        Task<bool> DeleteImageAsync(string publicId);
    }
}
