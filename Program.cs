using EduLearn.Data;
using EduLearn.Models;                              // NEW
using EduLearn.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;                // NEW
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Local-only overrides (e.g. real admin seed credentials) — gitignored, never committed
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// NEW — Identity registration
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// Google is used ONLY to capture a verified email during the /Apply access-request flow
// ("Continue with Google") — it's never wired up as a general site-wide login option, and
// AddDefaultIdentity above already registers the external cookie scheme this lands in with
// zero extra config. Registered only when real credentials exist (see
// appsettings.Local.json); if blank, the scheme is never wired up at all, mirroring
// SmtpEmailService.IsConfigured's gate-don't-crash pattern one level earlier.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
    });
}

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IChatService, GeminiChatService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IDeadlineReminderService, DeadlineReminderService>();
builder.Services.AddScoped<IBkashPaymentService, BkashPaymentService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>(); // NEW
builder.Services.AddHostedService<DeadlineReminderBackgroundService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();

// .avif isn't in ASP.NET Core's built-in static-file MIME map, so without this it gets
// served as application/octet-stream and browsers won't render it as an <img> — needed
// for wwwroot/image/user-circles-set_78370-4704.avif (the default profile avatar). Placed
// first, ahead of routing/MapStaticAssets, so it always gets the first look at file requests.
var staticFileContentTypeProvider = new FileExtensionContentTypeProvider();
staticFileContentTypeProvider.Mappings[".avif"] = "image/avif";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = staticFileContentTypeProvider });

app.UseRouting();
app.UseSession();

app.UseAuthentication();     // NEW — must come before UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages();         // NEW — needed for Identity UI pages

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "Instructor", "Student" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = app.Configuration["AdminSeed:Email"];
    var adminPassword = app.Configuration["AdminSeed:Password"];
    var seedLogger = app.Services.GetRequiredService<ILogger<Program>>();

    // Outside Development, a real AdminSeed:Email/Password must be configured explicitly
    // (e.g. via appsettings.Local.json or environment variables) — falling back to a
    // known default like "ChangeMe123!" would mean every unconfigured deployment ships
    // with a publicly-guessable admin password, since these fallbacks used to live in the
    // committed appsettings.json.
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        if (app.Environment.IsDevelopment())
        {
            adminEmail ??= "admin@edulearn.com";
            adminPassword ??= "ChangeMe123!";
        }
        else
        {
            seedLogger.LogWarning("AdminSeed:Email/Password are not configured — skipping admin account seeding. Set them explicitly (e.g. via environment variables) to create the initial admin account.");
        }
    }

    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword)
        && await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "EduLearn Admin",
            IsApproved = true,
            IsActive = true
        };
        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    // One-time move: files uploaded before private storage existed (lesson content, resumes,
    // assignment submissions) are still sitting under wwwroot, publicly reachable by anyone
    // with the URL. Relocate them to PrivateUploads now so the access-control fix in
    // FileUploadService actually covers content that already exists, not just new uploads.
    var env = app.Services.GetRequiredService<IWebHostEnvironment>();
    foreach (var subfolder in new[] { "lessons", "resumes", "submissions" })
    {
        var legacyFolder = Path.Combine(env.WebRootPath, "uploads", subfolder);
        if (!Directory.Exists(legacyFolder)) continue;

        var privateFolder = Path.Combine(env.ContentRootPath, "PrivateUploads", subfolder);
        Directory.CreateDirectory(privateFolder);

        foreach (var file in Directory.GetFiles(legacyFolder))
        {
            var destination = Path.Combine(privateFolder, Path.GetFileName(file));
            if (!File.Exists(destination))
            {
                File.Move(file, destination);
            }
        }
    }
}

app.Run();