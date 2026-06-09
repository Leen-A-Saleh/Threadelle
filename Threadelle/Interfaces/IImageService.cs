using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Treadelle.Interfaces
{
    public interface IImageService
    {
        Task<string> UploadImage(IFormFile image);
        Task<string> UploadGalleryImage(IFormFile Image);
        bool DeleteImage(string fileName);
    }
}
