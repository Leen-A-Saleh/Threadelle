using System.Collections.Generic;

namespace Threadelle.ViewModels
{
    public class GalleryPageVM
    {
        public List<GalleryItemVM> FeaturedItems { get; set; } = new List<GalleryItemVM>();
        public List<GalleryItemVM> MasonryItems { get; set; } = new List<GalleryItemVM>();
    }

    public class GalleryItemVM
    {
        public int Id { get; set; }
        
        public string Title { get; set; } = string.Empty;
        
        public string ImageUrl { get; set; } = string.Empty;
        
        public bool IsFeatured { get; set; }
        
        public int DisplayOrder { get; set; }
        
        public string? AltText { get; set; }
    }
}
