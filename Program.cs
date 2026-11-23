using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StudentSupport.Data;
using StudentSupport.Models;
using StudentSupport.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<AttachmentService>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/Denied";
        o.Cookie.Name = "M12.Session";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        o.SlidingExpiration = true;
        o.Events.OnValidatePrincipal = async ctx =>
        {
            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var raw = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = ctx.Principal?.FindFirstValue(ClaimTypes.Role);
            if (!int.TryParse(raw, out var id)) { ctx.RejectPrincipal(); return; }
            var person = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (person is null || !person.IsActive || person.Role != role || person.LockoutUntilUtc > DateTime.UtcNow)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 16,
            Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});
builder.Services.AddHsts(o => o.MaxAge = TimeSpan.FromDays(30));
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 6 * 1024 * 1024);

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Account/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Demo}/{action=Index}/{id?}");

// FOR CLASSROOM ONLY. Never enable this on public deployment.
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DEMO_SEED"))
{
    using var scope = app.Services.CreateScope();
    await DemoSeeder.Seed(scope.ServiceProvider);
}
app.Run();
