using System.Collections.Generic;

namespace Threadelle.ViewModels
{
    public class GalleryItemDetailsVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        public List<GalleryLightboxImageVM> Images { get; set; } = new List<GalleryLightboxImageVM>();
    }

    public class GalleryLightboxImageVM
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
    }
}
