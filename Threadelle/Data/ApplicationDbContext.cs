using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Threadelle.Models;

namespace Threadelle.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Color> Colors { get; set; }
        
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponProduct> CouponProducts { get; set; }
        public DbSet<CouponCategory> CouponCategories { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }

        public DbSet<CustomOrder> CustomOrders { get; set; }
        public DbSet<CustomOrderColor> CustomOrderColors { get; set; }
        public DbSet<CustomOrderImage> CustomOrderImages { get; set; }
        public DbSet<CustomOrderStatusHistory> CustomOrderStatusHistories { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductColor> ProductColors { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductMaterial> ProductMaterials { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Cart
            // One cart per signed-in user. Guest carts have a null UserId, so the
            // unique constraint is filtered to allow many guests (SQL Server treats
            // multiple NULLs as equal otherwise and would reject a second guest cart).
            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL")
                .HasDatabaseName("IX_Carts_UserId_Unique");

            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.SessionId)
                .HasDatabaseName("IX_Carts_SessionId");

            //CartItem
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => ci.CartId)
                .HasDatabaseName("IX_CartItems_CartId");

            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => ci.ProductId)
                .HasDatabaseName("IX_CartItems_ProductId");

            //Category
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Categories_Slug_Unique");

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.ParentCategoryId)
                .HasDatabaseName("IX_Categories_ParentCategoryId");

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.IsActive)
                .HasDatabaseName("IX_Categories_IsActive");

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.IsDeleted)
                .HasDatabaseName("IX_Categories_IsDeleted");

            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            //Color
            modelBuilder.Entity<Color>()
                .HasIndex(c => c.IsActive)
                .HasDatabaseName("IX_Colors_IsActive");



            //CustomOrder
            modelBuilder.Entity<CustomOrder>()
                .HasOne(co => co.User)
                .WithMany(u => u.CustomOrders)
                .HasForeignKey(co => co.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomOrder>()
                .HasIndex(co => co.CustomOrderNumber)
                .IsUnique()
                .HasDatabaseName("IX_CustomOrders_Number_Unique");

            modelBuilder.Entity<CustomOrder>()
                .HasIndex(co => co.UserId)
                .HasDatabaseName("IX_CustomOrders_UserId");

            modelBuilder.Entity<CustomOrder>()
                .HasIndex(co => co.Status)
                .HasDatabaseName("IX_CustomOrders_Status");

            //CustomOrderColor
            modelBuilder.Entity<CustomOrderColor>()
                .HasIndex(coc => new { coc.CustomOrderId, coc.ColorId })
                .IsUnique()
                .HasDatabaseName("IX_CustomOrderColors_CustomOrderId_ColorId_Unique");

            //CustomOrderStatusHistory
            modelBuilder.Entity<CustomOrderStatusHistory>()
                .HasOne(h => h.CustomOrder)
                .WithMany(co => co.StatusHistory)
                .HasForeignKey(h => h.CustomOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CustomOrderStatusHistory>()
                .HasIndex(h => h.CustomOrderId)
                .HasDatabaseName("IX_CustomOrderStatusHistories_CustomOrderId");

            //Coupon
            modelBuilder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("IX_Coupons_Code_Unique");

            modelBuilder.Entity<CouponProduct>()
                .HasKey(cp => new { cp.CouponId, cp.ProductId });

            modelBuilder.Entity<CouponCategory>()
                .HasKey(cc => new { cc.CouponId, cc.CategoryId });

            //ProductReview
            modelBuilder.Entity<ProductReview>()
                .HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductReview>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductReview>()
                .HasIndex(r => r.ProductId)
                .HasDatabaseName("IX_ProductReviews_ProductId");

            modelBuilder.Entity<ProductReview>()
                .HasIndex(r => r.IsApproved)
                .HasDatabaseName("IX_ProductReviews_IsApproved");

            //Notification
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId)
                .HasDatabaseName("IX_Notifications_UserId");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.IsRead)
                .HasDatabaseName("IX_Notifications_IsRead");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.CreatedAt)
                .HasDatabaseName("IX_Notifications_CreatedAt");

            //Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique()
                .HasDatabaseName("IX_Orders_OrderNumber_Unique");

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.UserId)
                .HasDatabaseName("IX_Orders_UserId");

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status)
                .HasDatabaseName("IX_Orders_Status");

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CreatedAt)
                .HasDatabaseName("IX_Orders_CreatedAt");

            //OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.OrderId)
                .HasDatabaseName("IX_OrderItems_OrderId");

            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.ProductId)
                .HasDatabaseName("IX_OrderItems_ProductId");

            //Product
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Products_Slug_Unique");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.CategoryId)
                .HasDatabaseName("IX_Products_CategoryId");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_Products_IsActive");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsFeatured)
                .HasDatabaseName("IX_Products_IsFeatured");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsDeleted)
                .HasDatabaseName("IX_Products_IsDeleted");

            //ProductColor
            modelBuilder.Entity<ProductColor>()
                .HasIndex(pc => new { pc.ProductId, pc.ColorId })
                .IsUnique()
                .HasDatabaseName("IX_ProductColors_ProductId_ColorId_Unique");

            //ProductImage
            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => pi.ProductId)
                .HasDatabaseName("IX_ProductImages_ProductId");

            //ProductMaterial
            modelBuilder.Entity<ProductMaterial>()
                .HasIndex(pm => new { pm.ProductId, pm.MaterialId })
                .IsUnique()
                .HasDatabaseName("IX_ProductMaterials_ProductId_MaterialId_Unique");

            //Testimonial
            modelBuilder.Entity<Testimonial>()
                .HasIndex(t => t.UserId)
                .HasDatabaseName("IX_Testimonials_UserId");

            modelBuilder.Entity<Testimonial>()
                .HasIndex(t => t.IsApproved)
                .HasDatabaseName("IX_Testimonials_IsApproved");

            //Wishlist
            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.User)
                .WithMany(u => u.Wishlists)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Wishlist>()
                .HasIndex(w => new { w.UserId, w.ProductId })
                .IsUnique()
                .HasDatabaseName("IX_Wishlists_UserId_ProductId_Unique");

            modelBuilder.Entity<Wishlist>()
                .HasIndex(w => w.UserId)
                .HasDatabaseName("IX_Wishlists_UserId");

            // Global Query Filters for Soft Deletes
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Material>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<Color>().HasQueryFilter(c => !c.IsDeleted);

            modelBuilder.Entity<Testimonial>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<Coupon>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
            modelBuilder.Entity<CustomOrder>().HasQueryFilter(co => !co.IsDeleted);
            modelBuilder.Entity<ProductReview>().HasQueryFilter(r => !r.IsDeleted);
            modelBuilder.Entity<CustomOrderStatusHistory>().HasQueryFilter(h => !h.CustomOrder.IsDeleted);
        }
    }
}