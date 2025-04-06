using System.Net.Http.Headers;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_BusinessObject.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.MedicalPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DomainSetting _domainSetting;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            IHttpClientFactory httpClientFactory,
            IOptions<DomainSetting> domainSetting,
            IHttpContextAccessor httpContextAccessor,
            IOptions<CloudinarySettings> cloudinarySettings,
            ILogger<CreateModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _domainSetting = domainSetting.Value;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            var settings = cloudinarySettings.Value;
            var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        private void SetAuthorizationHeader(HttpClient client)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Mã truy cập không được tìm thấy trong phiên. Vui lòng đăng nhập lại.");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSupplier();
            LoadMedicalTypes();
            LoadUnitOptions();
            return Page();
        }

        [BindProperty]
        public Medical Medical { get; set; } = default!;
        [BindProperty]
        public List<SelectListItem> SupplierList { get; set; } = new();
        [BindProperty]
        public List<SelectListItem> MedicalTypesList { get; set; } = new();
        [BindProperty]
        public List<SelectListItem> UnitList { get; set; } = new();

        [BindProperty]
        public List<IFormFile> ImageFiles { get; set; } = new List<IFormFile>();

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (ImageFiles != null && ImageFiles.Count > 0)
                {
                    if (Medical.Image == null)
                    {
                        Medical.Image = new List<string>();
                    }
                    foreach (var file in ImageFiles)
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            var uploadParams = new ImageUploadParams()
                            {
                                File = new FileDescription(file.FileName, stream),
                            };
                            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                Medical.Image.Add(uploadResult.SecureUrl.AbsoluteUri);
                            }
                        }
                    }
                }

                Medical.CreatedDate = DateTime.UtcNow;
                Medical.UpdatedDate = DateTime.UtcNow;

                using var client = _httpClientFactory.CreateClient();
                SetAuthorizationHeader(client);

                var apiUrl = $"{GetCurrentDomain()}medical/create";
                var jsonContent = JsonConvert.SerializeObject(Medical);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                var resultContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<BaseResponse<Medical>>(resultContent);

                if (result?.Success == true)
                {
                    TempData["SuccessMessage"] = "Tạo sản phẩm thành công";
                    return RedirectToPage("./Index");
                }
                else
                {
                    _logger.LogWarning("Medical creation failed: {Response}", resultContent);
                    ModelState.AddModelError("", result?.Message ?? "Lỗi khi tạo sản phẩm");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating medical item");
                ModelState.AddModelError("", "Lỗi khi tạo sản phẩm: " + ex.Message);
                return Page();
            }
        }

        private string GetCurrentDomain() => _domainSetting.Local;

        public async Task LoadSupplier()
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

        private void LoadMedicalTypes()
        {
            var medicalTypes = new List<string>
            {
                "Thuốc Kháng Sinh",
                "Thuốc Giảm Đau",
                "Thuốc Hạ Sốt",
                "Thuốc Chống Viêm",
                "Thuốc Tiêu Hóa",
                "Thuốc An Thần",
                "Thuốc Ngủ",
                "Thuốc Ho",
                "Thuốc Trị Sốt Rét",
                "Thuốc Tim Mạch",
                "Thuốc Huyết Áp",
                "Thuốc Đái Tháo Đường",
                "Thuốc Trị Dị Ứng",
                "Thuốc Bổ",
                "Thuốc Lợi Tiểu",
                "Thuốc Trị Nấm",
                "Thuốc Kháng Virus",
                "Thuốc Trị Hen",
                "Thuốc Tiêu Đờm",
                "Thuốc Tẩy Giun"
            };

            MedicalTypesList = medicalTypes.Select(type => new SelectListItem
            {
                Value = type,
                Text = type
            }).ToList();
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