using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_BusinessObject.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MedicalWarehouse_Services;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.MedicalPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DomainSetting _domainSetting;
        private readonly IHttpContextAccessor _httpContext;
        private readonly ILogger<EditModel> _logger;
        private readonly Cloudinary _cloudinary;

        public EditModel(
            IHttpClientFactory httpClientFactory,
            IOptions<DomainSetting> domainSetting,
            IHttpContextAccessor httpContext,
            ILogger<EditModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _domainSetting = domainSetting.Value;
            _httpContext = httpContext;
            _logger = logger;

            var account = new Account("dkedkbs8d", "679942311144417", "Y3HP-AH7BfrD7Q_ANLcZZH4yrNc");
            _cloudinary = new Cloudinary(account);
        }

        [BindProperty]
        public MedicalResponseModel MedicalResponse { get; set; }

        [BindProperty]
        public MedicalRequestModel MedicalRequest { get; set; } = new();

        [BindProperty]
        public List<IFormFile> ImageFiles { get; set; } = new();

        [BindProperty]
        public List<string> ExistingImages { get; set; } = new(); // Store existing images

        [BindProperty]
        public List<string> DeletedImages { get; set; } = new(); // Store deleted images

        public List<SelectListItem> SupplierList { get; set; } = new();
        
        public List<SelectListItem> UnitList { get; set; } = new();

        private void SetAuthorizationHeader(HttpClient client)
        {
            var token = _httpContext.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Mã truy cập không được tìm thấy trong phiên. Vui lòng đăng nhập lại.");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private string GetCurrentDomain() => _domainSetting.Local;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (!id.HasValue) return NotFound();

            using var client = _httpClientFactory.CreateClient();
            try
            {
                SetAuthorizationHeader(client);
                var apiUrl = $"{GetCurrentDomain()}medical/{id}";

                _logger.LogInformation("Fetching medical item: {ApiUrl}", apiUrl);

                var response = await client.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<BaseResponse<MedicalResponseModel>>(content);

                if (result?.Success != true || result.Result == null)
                {
                    _logger.LogWarning("Medical item not found: {Id}", id);
                    return NotFound();
                }

                await LoadSupplier();
                LoadUnitOptions();
                MedicalResponse = result.Result;
                ExistingImages = MedicalResponse.Image ?? new List<string>();

                MedicalRequest = new MedicalRequestModel
                {
                    TypeMedical = MedicalResponse.TypeMedical,
                    Name = MedicalResponse.Name,
                    Description = MedicalResponse.Description,
                    Price = MedicalResponse.Price,
                    SupplierId = MedicalResponse.SupplierId,
                    Image = new List<string>(ExistingImages)
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading medical item with ID {MedicalId}", id);
                ModelState.AddModelError("", "Lỗi khi tải thông tin sản phẩm: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (!id.HasValue) return NotFound();

            using var client = _httpClientFactory.CreateClient();
            try
            {
                SetAuthorizationHeader(client);

                // Ensure Image List is Initialized
                MedicalRequest.Image ??= new List<string>();

                // Remove Deleted Images
                if (DeletedImages?.Count > 0)
                {
                    MedicalRequest.Image.RemoveAll(img => DeletedImages.Contains(img));
                }

                // Upload new images if provided
                if (ImageFiles?.Count > 0)
                {
                    foreach (var file in ImageFiles)
                    {
                        using var stream = file.OpenReadStream();
                        var uploadParams = new ImageUploadParams { File = new FileDescription(file.FileName, stream) };
                        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                        if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            MedicalRequest.Image.Add(uploadResult.SecureUrl.AbsoluteUri);
                        }
                        else
                        {
                            _logger.LogWarning("Image upload failed: {FileName}", file.FileName);
                        }
                    }
                }

                var apiUrl = $"{GetCurrentDomain()}medical/{id}";
                var json = JsonConvert.SerializeObject(MedicalRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Updating medical item: {ApiUrl} with data: {RequestData}", apiUrl, json);

                var response = await client.PutAsync(apiUrl, content);
                var resultContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<BaseResponse<MedicalResponseModel>>(resultContent);

                if (result?.Success == true)
                {
                    TempData["SuccessMessage"] = result.Message ?? "Cập nhật thành công";
                    return RedirectToPage("./Index");
                }
                else
                {
                    _logger.LogWarning("Update failed: {Response}", resultContent);
                    ModelState.AddModelError("", result?.Message ?? "Lỗi khi cập nhật");
                    await LoadSupplier();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating medical item with ID {MedicalId}", id);
                ModelState.AddModelError("", "Lỗi khi cập nhật thông tin sản phẩm: " + ex.Message);
                await LoadSupplier();
                return Page();
            }
        }

        private async Task LoadSupplier()
        {
            using var client = _httpClientFactory.CreateClient();
            SetAuthorizationHeader(client);

            var apiUrl = $"{GetCurrentDomain()}supplier";
            _logger.LogInformation("Fetching suppliers from {ApiUrl}", apiUrl);

            var response = await client.GetAsync(apiUrl);
            var content = await response.Content.ReadAsStringAsync();
            var suppliers = JsonConvert.DeserializeObject<List<SupplierResponse>>(content);

            SupplierList = suppliers?.ConvertAll(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            }) ?? new List<SelectListItem>();

            if (SupplierList.Count == 0)
            {
                _logger.LogWarning("No suppliers found.");
            }
        }

        private void LoadUnitOptions()
        {
            var units = new List<string>
            {
                "Vỉ",
                "Hộp",
                "Viên",
                "Tuýp",
                "Ống"
            };

            UnitList = units.Select(unit => new SelectListItem
            {
                Value = unit,
                Text = unit
            }).ToList();
        }
    }
}
