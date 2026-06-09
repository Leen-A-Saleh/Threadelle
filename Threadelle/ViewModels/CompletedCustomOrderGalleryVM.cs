using Threadelle.Models;

namespace Threadelle.ViewModels
{
    public class CompletedCustomOrderGalleryVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string DeliveredDate { get; set; } = string.Empty;
        public string? GalleryDescription { get; set; }
        public string CoverImage { get; set; } = string.Empty;
        public int PhotoCount { get; set; }
        public List<GalleryImageVM> GalleryImages { get; set; } = new List<GalleryImageVM>();
    }

    public class GalleryImageVM
    {
        public int Id { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string MediumUrl { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
    }
}
