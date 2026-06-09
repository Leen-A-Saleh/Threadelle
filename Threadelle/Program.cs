using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Hubs;
using Threadelle.Mapping;
using Threadelle.Models;
using Threadelle.Services;
using Treadelle.Interfaces;

namespace Threadelle
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            // Services
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, Threadelle.Services.NoOpEmailSender>();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IImageService, ImageService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<IStripeService, StripeService>();
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // Admin services
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IProductPricingService, ProductPricingService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IColorService, ColorService>();
            builder.Services.AddScoped<IMaterialService, MaterialService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<ICouponService, CouponService>();

            builder.Services.AddSignalR();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // Seed data  
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await db.Database.MigrateAsync();

                string[] roleNames = { "Admin", "User" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                }

                // Admin initialization — credentials from appsettings/env
                var adminEmail = builder.Configuration["AdminSeed:Email"]
                    ?? throw new InvalidOperationException("AdminSeed:Email not configured.");
                var adminPassword = builder.Configuration["AdminSeed:Password"]
                    ?? throw new InvalidOperationException("AdminSeed:Password not configured.");
                var adminFullName = builder.Configuration["AdminSeed:FullName"] ?? "Admin";

                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    var newAdmin = new ApplicationUser
                    {
                        FullName = adminFullName,
                        UserName = adminEmail,
                        Email = adminEmail,
                        Gender = ApplicationUserGender.Female,
                        EmailConfirmed = true,
                        PhoneNumber = "0799999999"
                    };

                    var result = await userManager.CreateAsync(newAdmin, adminPassword);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
                else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                var allUsers = await userManager.Users.ToListAsync();
                foreach (var user in allUsers)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    if (!roles.Any())
                        await userManager.AddToRoleAsync(user, "User");
                }
                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseMigrationsEndPoint();
                }
                else
                {
                    app.UseExceptionHandler("/Home/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }


                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseSession();
                app.UseAuthentication();
                app.UseAuthorization();

                // Routing
                app.MapControllerRoute(
                    name: "collection-alias",
                    pattern: "Collection",
                    defaults: new { controller = "Collections", action = "Index" }
                );

                app.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
                );

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}"
                );

                app.MapRazorPages();
                app.MapHub<AdminNotificationHub>("/hubs/admin-notifications");

                app.Run();
            }
        }
    }
}