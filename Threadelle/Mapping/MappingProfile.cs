using AutoMapper;
using Threadelle.Models;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Mapping
{
    /// <summary>
    /// Central AutoMapper profile. Maps are added here per module; all maps are
    /// EF-translatable so services can use <c>ProjectTo</c> for efficient SQL projections.
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ── Dashboard (Module 2) ────────────────────────────────────────
            CreateMap<Order, RecentOrderRow>()
                .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.User.FullName))
                .ForMember(d => d.Total, opt => opt.MapFrom(s => s.TotalPrice));

            // ── Orders (Module 8) ───────────────────────────────────────────
            CreateMap<Order, OrderListItemViewModel>()
                .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.User.FullName));

            CreateMap<ApplicationUser, RecentUserRow>();

            CreateMap<Testimonial, RecentTestimonialRow>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.FullName));

            CreateMap<CustomOrder, RecentCustomOrderRow>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.FullName));

            // ── Categories (Module 4) ───────────────────────────────────────
            CreateMap<Category, CategoryListItemViewModel>()
                .ForMember(d => d.ParentName, opt => opt.MapFrom(s => s.ParentCategory != null ? s.ParentCategory.Name : null))
                .ForMember(d => d.ProductCount, opt => opt.MapFrom(s => s.Products.Count(p => !p.IsDeleted)))
                .ForMember(d => d.ChildCount, opt => opt.MapFrom(s => s.Children.Count(c => !c.IsDeleted)));

            CreateMap<Category, CategoryUpsertViewModel>()
                .ForMember(d => d.ExistingImageUrl, opt => opt.MapFrom(s => s.ImageUrl))
                .ForMember(d => d.ImageFile, opt => opt.Ignore())
                .ForMember(d => d.ParentOptions, opt => opt.Ignore());

            // Scalar fields only; slug, image and navigation/soft-delete handled in the service.
            CreateMap<CategoryUpsertViewModel, Category>()
                .ForMember(d => d.Slug, opt => opt.Ignore())
                .ForMember(d => d.ImageUrl, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore())
                .ForMember(d => d.DeletedAt, opt => opt.Ignore())
                .ForMember(d => d.DeletedBy, opt => opt.Ignore())
                .ForMember(d => d.ParentCategory, opt => opt.Ignore())
                .ForMember(d => d.Children, opt => opt.Ignore())
                .ForMember(d => d.Products, opt => opt.Ignore());

            // ── Products (Module 7) ─────────────────────────────────────────
            CreateMap<Product, ProductUpsertViewModel>()
                .ForMember(d => d.NewImages, opt => opt.Ignore())
                .ForMember(d => d.ExistingImages, opt => opt.Ignore())
                .ForMember(d => d.ColorRows, opt => opt.Ignore())
                .ForMember(d => d.MaterialRows, opt => opt.Ignore())
                .ForMember(d => d.CategoryOptions, opt => opt.Ignore())
                .ForMember(d => d.MaterialOptions, opt => opt.Ignore());

            CreateMap<ProductUpsertViewModel, Product>()
                .ForMember(d => d.Slug, opt => opt.Ignore())
                .ForMember(d => d.Category, opt => opt.Ignore())
                .ForMember(d => d.Images, opt => opt.Ignore())
                .ForMember(d => d.ProductColors, opt => opt.Ignore())
                .ForMember(d => d.ProductMaterials, opt => opt.Ignore())
                .ForMember(d => d.ProductStory, opt => opt.Ignore())
                .ForMember(d => d.ProductType, opt => opt.Ignore())
                .ForMember(d => d.SoldOutDisplayMode, opt => opt.Ignore())
                .ForMember(d => d.SoldOutLabel, opt => opt.Ignore())
                .ForMember(d => d.Price, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore())
                .ForMember(d => d.DeletedAt, opt => opt.Ignore())
                .ForMember(d => d.DeletedBy, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore());

            // ── Colors (Module 6) ──────────────────────────────────────────
            CreateMap<Color, ColorListItemViewModel>()
                .ForMember(d => d.ProductCount, opt => opt.MapFrom(s => s.ProductColors.Count));

            CreateMap<Color, ColorUpsertViewModel>();

            CreateMap<ColorUpsertViewModel, Color>()
                .ForMember(d => d.ProductColors, opt => opt.Ignore())
                .ForMember(d => d.CustomOrderColors, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore())
                .ForMember(d => d.DeletedAt, opt => opt.Ignore())
                .ForMember(d => d.DeletedBy, opt => opt.Ignore());

            // ── Materials (Module 5) ────────────────────────────────────────
            CreateMap<Material, MaterialListItemViewModel>()
                .ForMember(d => d.ProductCount, opt => opt.MapFrom(s => s.ProductMaterials.Count));

            CreateMap<Material, MaterialUpsertViewModel>();

            CreateMap<MaterialUpsertViewModel, Material>()
                .ForMember(d => d.ProductMaterials, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore())
                .ForMember(d => d.DeletedAt, opt => opt.Ignore())
                .ForMember(d => d.DeletedBy, opt => opt.Ignore());
        }
    }
}
