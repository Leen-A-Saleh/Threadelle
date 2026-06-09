using Threadelle.Models;

namespace Threadelle.ViewModels
{
    public class HomeViewModel
    {
        public List<ProductCardViewModel> FeaturedProducts { get; set; } = new();
        public List<ProductCardViewModel> OnePieceProducts { get; set; } = new();
        public List<CollectionCardViewModel> Collections { get; set; } = new();
        public List<Testimonial> Testimonials { get; set; } = new();
    }

    public class CollectionCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int ProductCount { get; set; }
    }
}
