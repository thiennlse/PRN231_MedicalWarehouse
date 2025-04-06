using MedicalWarehouse_BusinessObject.Contract;
using MedicalWarehouse_Services.Interface;
using MedicalWarehouse_Services.Services;
using MedicalWarehouse_BusinessObject.Settings;
using Microsoft.EntityFrameworkCore;
using MedicalWareHouse_Client;
using MedicalWarehouse_Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using MedicalWareHouse_Client.Extensions;

var builder = WebApplication.CreateBuilder(args);
IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
     options.UseNpgsql(builder.Configuration.GetConnectionString("local")));

// Thêm các dịch vụ xác thực
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

// Đăng ký lớp AuthorizationPolicyProvider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
builder.Services.AddAuthorization();

builder.Services.AddRazorPages(options =>
{
    // Cấu hình chính sách phân quyền mặc định
    options.Conventions.AuthorizeFolder("/", "RequireAuthentication");
    
    // Các trang không yêu cầu xác thực
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Register");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AllowAnonymousToPage("/AccessDenied");
    
    // Bạn cũng có thể phân quyền theo vai trò cụ thể cho một số thư mục
    // Ví dụ: Chỉ cho phép vai trò ADMIN truy cập vào thư mục AdminPage
    // options.Conventions.AuthorizeFolder("/AdminPage", "RequireRole:ADMIN");
    
    // Hoặc phân quyền cho từng trang cụ thể
    // options.Conventions.AuthorizePage("/MedicalPage/Index", "RequireRole:PHARMACY");
});

builder.Services.AddHttpClient();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySetting"));
builder.Services.Configure<DomainSetting>(builder.Configuration.GetSection("Domain"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();
app.UseRouting();

// Thêm middleware xác thực và phân quyền
app.UseAuthentication();
app.UseAuthorization();

// Tùy chỉnh định tuyến Razor Pages
app.MapRazorPages();
app.MapGet("/", async context =>
{
    context.Response.Redirect("/Login"); // Chuyển hướng root URL đến /Login
    await Task.CompletedTask;
});
app.Run();

