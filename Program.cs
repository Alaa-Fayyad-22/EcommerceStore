using EcommerceStore.Data;
using EcommerceStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStore
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1️⃣ Configure PostgreSQL
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 2️⃣ Add Identity (User + Roles) with UI
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

            // ✅ Configure cookie paths for Identity UI
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            // ✅ Enable Session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true; // Required for GDPR compliance
            });

            // 3️⃣ Add CORS for external dashboard
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowDashboard",
                    policy => policy.AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .AllowAnyHeader());
            });

            // ✅ Load API Key from configuration
            var apiKey = builder.Configuration["ApiSettings:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("[ERROR] ApiSettings:ApiKey missing in appsettings.json!");
            }

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // 4️⃣ Role & SuperAdmin Seeding
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                await SeedRolesAndSuperAdmin(roleManager, userManager);
            }

            // 5️⃣ Middleware Pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // ✅ Enable Session Middleware
            app.UseSession();

            // ✅ API Key Middleware (only for /api routes)
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("API Key is missing");
                        return;
                    }

                    if (string.IsNullOrEmpty(apiKey))
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Server API Key is not configured.");
                        return;
                    }

                    Console.WriteLine($"[DEBUG] Expected: {apiKey}, Received: {extractedApiKey}");

                    if (!apiKey.Equals(extractedApiKey))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Invalid API Key");
                        return;
                    }
                }
                await next();
            });

            app.UseCors("AllowDashboard");

            app.UseAuthentication();
            app.UseAuthorization();

            // ✅ Identity Razor Pages (Login/Register)
            app.MapRazorPages();

            // ✅ Controllers (API + MVC)
            app.MapControllers();

            // ✅ Default MVC route
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        // ✅ Role & SuperAdmin Seeder
        private static async Task SeedRolesAndSuperAdmin(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            string[] roles = { "SuperAdmin", "Admin", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            string superAdminEmail = "superadmin@store.com";
            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
            if (superAdmin == null)
            {
                var newSuperAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true,
                    FullName = "System Super Admin",
                    Address = "Main Office HQ"
                };

                var result = await userManager.CreateAsync(newSuperAdmin, "SuperAdmin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newSuperAdmin, "SuperAdmin");
                }
            }
        }
    }
}
