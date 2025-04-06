using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace MedicalWarehouse_Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSection _cloudinarySetting;

        public CloudinaryService(IConfiguration configuration)
        {
            _configuration = configuration;
            _cloudinarySetting = _configuration.GetSection("CloudinarySetting");

            var account = new Account
            {
                ApiKey = _cloudinarySetting.GetSection("ApiKey").Value,
                ApiSecret = _cloudinarySetting.GetSection("ApiSecret").Value,
                Cloud = _cloudinarySetting.GetSection("CloudName").Value
            };

            _cloudinary = new Cloudinary(account);
        }

        public async Task<String> SaveImage(IFormFile file)
        {
            var uploadResult = new ImageUploadResult();
            if (file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream)
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);

            }
            return uploadResult.SecureUrl.ToString();
        }
    }
}
