using Treadelle.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Threadelle.Services
{

    public class ImageService : IImageService
    {
        private readonly string _imagePath;

        public ImageService() {
            _imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "upload");
        }

        public async Task<string> UploadImage(IFormFile Image)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Image.FileName);
            var filePath = Path.Combine(_imagePath, fileName);

            using (var stream = File.Create(filePath))
            {
                await Image.CopyToAsync(stream);
            }

            return fileName;
        } 

        public async Task<string> UploadGalleryImage(IFormFile Image)
        {
            var ext = Path.GetExtension(Image.FileName);
            var baseName = Guid.NewGuid().ToString();
            var fileName = baseName + ext;
            var filePath = Path.Combine(_imagePath, fileName);

            // Save original
            using (var stream = File.Create(filePath))
            {
                await Image.CopyToAsync(stream);
            }

            // Generate variants using SixLabors.ImageSharp
            try
            {
                using var image = await SixLabors.ImageSharp.Image.LoadAsync(filePath);
                
                // Medium (1200px max)
                var mediumPath = Path.Combine(_imagePath, baseName + "_medium" + ext);
                var mediumClone = image.Clone(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(1200, 1200),
                    Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max
                }));
                await mediumClone.SaveAsync(mediumPath);
                
                // Thumbnail (400px max)
                var thumbPath = Path.Combine(_imagePath, baseName + "_thumb" + ext);
                var thumbClone = image.Clone(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(400, 400),
                    Mode = SixLabors.ImageSharp.Processing.ResizeMode.Crop
                }));
                await thumbClone.SaveAsync(thumbPath);
            }
            catch (Exception)
            {
                // Fallback: if ImageSharp fails, just use original for all sizes
            }

            return fileName;
        } 

        public bool DeleteImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            var fullPath = Path.Combine(_imagePath, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
            return false;
        }

    }
}
