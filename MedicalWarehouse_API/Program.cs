using FluentValidation;
using MedicalWarehouse_BusinessObject.Contract;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_BusinessObject.Settings;
using MedicalWarehouse_Repository.Interface;
using MedicalWarehouse_Repository.Repositories;
using MedicalWarehouse_Services;
using MedicalWarehouse_Services.Interface;
using MedicalWarehouse_Services.Services;
using MedicalWarehouse_Validations.Area;
using MedicalWarehouse_Validations.Medical;
using MedicalWarehouse_Validations.Shipment;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using Net.payOS;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.Configure<DomainSetting>(builder.Configuration.GetSection("Domain"));
#region PayOS
PayOS payOS = new PayOS(configuration["PayOs:ClientId"] ?? throw new Exception("can not find Environment"),
                        configuration["PayOs:ApiKey"] ?? throw new Exception("can not find Environment"),
                        configuration["PayOs:CheckSumKey"] ?? throw new Exception("can not find Environment")
                        );
#endregion
#region OData
builder.Services.AddControllers()
    .AddOData(options =>
    {
        var modelBuilder = new ODataConventionModelBuilder();
        modelBuilder.EntitySet<Area>("Area");
        modelBuilder.EntitySet<Shipment>("Shipments"); // Đăng ký entity OData
        modelBuilder.EntitySet<ShipmentDetail>("ShipmentDetails");
        modelBuilder.EntitySet<Supplier>("Suppliers");
        modelBuilder.EntitySet<UpComingResponse>("Dashboard");
        modelBuilder.EntitySet<Medical>("Medical");

        options.Select().Filter().Expand().OrderBy().Count().SetMaxTop(100)
               .AddRouteComponents("odata", modelBuilder.GetEdmModel());
    });
#endregion
#region Add Authentication
var jwtSetting = configuration.GetSection("JWT");
var key = Encoding.UTF8.GetBytes(jwtSetting["Secret"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSetting["Issuer"],
        ValidAudience = jwtSetting["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

});
builder.Services.AddAuthorization();

#endregion
#region Context
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
});
/*builder.Services.AddDbContext<ApplicationDbContext>(options =>
     options.UseSqlServer(builder.Configuration.GetConnectionString("local")));*/

builder.Services.AddDbContext<ApplicationDbContext>(options =>
     options.UseNpgsql(builder.Configuration.GetConnectionString("local")));
#endregion
#region CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
#endregion
builder.Services.AddEndpointsApiExplorer();
#region AddScope
builder.Services.AddScoped<SendEmailService>();
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<PayOsService>();
builder.Services.AddSingleton(payOS);
builder.Services.AddScoped<UserManager<User>>();
builder.Services.AddScoped<IAuthService, AuthServices>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IShipmentDetailRepository, ShipmentDetailRepository>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IMedicalRepository, MedicalRepository>();
builder.Services.AddScoped<IMedicalService, MedicalService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<MedicalService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
#endregion
#region AddFluentValidator
builder.Services.AddScoped<IValidator<AreaRequestModel>, AreaValidator>();
builder.Services.AddScoped<IValidator<ShipmentRequestModel>, ShipmentValidator>();
builder.Services.AddScoped<IValidator<MedicalRequestModel>, MedicalValidator>();
#endregion
#region Authentication in Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Description = "Please enter your token with this format: ''Bearer YOUR_TOKEN''",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Name = "Bearer",
                In = ParameterLocation.Header,
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new string[]{}
        }
    });
});
#endregion
var app = builder.Build();
if (app.Environment.IsDevelopment())
{

}
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Medical Warehosue V1");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
