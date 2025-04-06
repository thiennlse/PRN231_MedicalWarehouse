using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Response;
using Microsoft.AspNetCore.Mvc;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.ShipmentPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class EditModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<EditModel> _logger;
        private string _shipmentId;

        public EditModel(HttpClient httpClient, IConfiguration configuration, ILogger<EditModel> logger)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? throw new ArgumentNullException("ApiSettings:BaseUrl is missing");
            _logger = logger;
        }

        [BindProperty]
        public ShipmentRequestModel ShipmentRequest { get; set; } = new ShipmentRequestModel();

        public List<SelectListItem> Areas { get; set; }
        public List<SelectListItem> Suppliers { get; set; }
        public List<SelectListItem> Medicals { get; set; }

        public string ShipmentId => _shipmentId;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("No shipment ID provided in the request.");
                return NotFound();
            }

            _shipmentId = id;

            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (!string.IsNullOrEmpty(accessToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }

                var response = await _httpClient.GetAsync($"{_baseUrl}/shipment/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call to /shipment/{Id} failed with status code {StatusCode}: {ReasonPhrase}", id, response.StatusCode, response.ReasonPhrase);
                    return NotFound();
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Shipment API Response: {Content}", content);

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ShipmentRequestModel>>(content);
                if (apiResponse == null || !apiResponse.Success || (apiResponse.Result == null && (apiResponse.Results == null || !apiResponse.Results.Any())))
                {
                    _logger.LogWarning("No valid shipment data found for ID {Id}", id);
                    return NotFound();
                }

                ShipmentRequest = apiResponse.Result ?? apiResponse.Results.First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load shipment with ID {Id}. Details: {Message}", id, ex.Message);
                return NotFound();
            }

            await LoadAreasAsync();
            await LoadSuppliersAsync();
            await LoadMedicalsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("ShipmentRequest.ShipmentDetails"); // Loại bỏ lỗi liên quan đến ShipmentDetails nếu cần

            var shipmentId = Request.Form["shipmentId"];
            if (string.IsNullOrEmpty(shipmentId))
            {
                _logger.LogWarning("Shipment ID is missing in the form data.");
                ModelState.AddModelError("", "Shipment ID is missing.");
                await LoadAreasAsync();
                await LoadSuppliersAsync();
                await LoadMedicalsAsync();
                return Page();
            }

            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("Access token not found in session.");
                    ModelState.AddModelError("", "You must be logged in to edit a shipment.");
                    await LoadAreasAsync();
                    await LoadSuppliersAsync();
                    await LoadMedicalsAsync();
                    return Page();
                }

                if (ShipmentRequest.ShipmentDetails == null)
                {
                    ShipmentRequest.ShipmentDetails = new List<ShipmentDetailRequestModel>();
                }
                else
                {
                    ShipmentRequest.ShipmentDetails.Clear();
                }

                int index = 0;
                while (Request.Form.ContainsKey($"ShipmentRequest.ShipmentDetails[{index}].MedicalId"))
                {
                    var medicalIdStr = Request.Form[$"ShipmentRequest.ShipmentDetails[{index}].MedicalId"].ToString();
                    var quantityStr = Request.Form[$"ShipmentRequest.ShipmentDetails[{index}].Quantity"].ToString();
                    var expiredDateStr = Request.Form[$"ShipmentRequest.ShipmentDetails[{index}].ExpiredDate"].ToString();

                    if (!Guid.TryParse(medicalIdStr, out Guid medicalId))
                    {
                        _logger.LogWarning("Invalid MedicalId at index {Index}: {MedicalId}", index, medicalIdStr);
                        ModelState.AddModelError($"ShipmentRequest.ShipmentDetails[{index}].MedicalId", "Invalid Medical ID.");
                        await LoadAreasAsync();
                        await LoadSuppliersAsync();
                        await LoadMedicalsAsync();
                        return Page();
                    }

                    if (!int.TryParse(quantityStr, out int quantity) || quantity <= 0)
                    {
                        _logger.LogWarning("Invalid Quantity at index {Index}: {Quantity}", index, quantityStr);
                        ModelState.AddModelError($"ShipmentRequest.ShipmentDetails[{index}].Quantity", "Quantity must be a positive number.");
                        await LoadAreasAsync();
                        await LoadSuppliersAsync();
                        await LoadMedicalsAsync();
                        return Page();
                    }

                    if (!DateTime.TryParse(expiredDateStr, out DateTime expiredDate))
                    {
                        expiredDate = DateTime.Now.AddYears(1); // Giá trị mặc định
                        _logger.LogInformation("Invalid or missing ExpiredDate at index {Index}, using default: {ExpiredDate}", index, expiredDate);
                    }
                    else if (expiredDate < DateTime.Now)
                    {
                        _logger.LogWarning("ExpiredDate at index {Index} is in the past: {ExpiredDate}", index, expiredDate);
                        ModelState.AddModelError($"ShipmentRequest.ShipmentDetails[{index}].ExpiredDate", "Expiration date cannot be in the past.");
                        await LoadAreasAsync();
                        await LoadSuppliersAsync();
                        await LoadMedicalsAsync();
                        return Page();
                    }

                    ShipmentRequest.ShipmentDetails.Add(new ShipmentDetailRequestModel
                    {
                        MedicalId = medicalId,
                        Quantity = quantity,
                        ExpiredDate = expiredDate
                    });

                    index++;
                }

                if (ShipmentRequest.ShipmentDetails.Count == 0)
                {
                    _logger.LogWarning("No valid shipment details provided.");
                    ModelState.AddModelError("", "At least one shipment detail is required.");
                    await LoadAreasAsync();
                    await LoadSuppliersAsync();
                    await LoadMedicalsAsync();
                    return Page();
                }

                _logger.LogInformation("Dữ liệu ShipmentRequest gửi lên API: {ShipmentRequest}", JsonConvert.SerializeObject(ShipmentRequest));

                var jsonContent = JsonConvert.SerializeObject(ShipmentRequest);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/shipment/{shipmentId}")
                {
                    Content = httpContent
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Shipment updated successfully for ID {ShipmentId}", shipmentId);
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to update shipment. Response: {Response}", errorContent);
                    ModelState.AddModelError("", $"Failed to update shipment: {errorContent}");
                    await LoadAreasAsync();
                    await LoadSuppliersAsync();
                    await LoadMedicalsAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating shipment");
                ModelState.AddModelError("", $"Error: {ex.Message}");
                await LoadAreasAsync();
                await LoadSuppliersAsync();
                await LoadMedicalsAsync();
                return Page();
            }
        }

        private async Task LoadAreasAsync()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (!string.IsNullOrEmpty(accessToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }

                var response = await _httpClient.GetAsync($"{_baseUrl}/area");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call to /area failed with status code {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    throw new HttpRequestException($"API call failed: {response.StatusCode}");
                }
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Area API Response: {Content}", content);

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<AreaResponseModel>>(content);
                if (apiResponse != null && apiResponse.Success && apiResponse.Results != null)
                {
                    Areas = apiResponse.Results.Select(a => new SelectListItem { Value = a.AreaId.ToString(), Text = a.AreaName }).ToList();
                }
                else
                {
                    Areas = new List<SelectListItem>();
                    ModelState.AddModelError("", "No areas found in API response.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load areas from API. Details: {Message}", ex.Message);
                ModelState.AddModelError("", "Could not load areas. Please try again later.");
                Areas = new List<SelectListItem>();
            }
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (!string.IsNullOrEmpty(accessToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }

                var response = await _httpClient.GetAsync($"{_baseUrl}/supplier");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call to /suppliers/get-all failed with status code {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    throw new HttpRequestException($"API call failed: {response.StatusCode}");
                }
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Supplier API Response: {Content}", content);

                var suppliers = JsonConvert.DeserializeObject<List<SupplierResponse>>(content);
                if (suppliers != null && suppliers.Count > 0)
                {
                    Suppliers = suppliers.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToList();
                }
                else
                {
                    Suppliers = new List<SelectListItem>();
                    ModelState.AddModelError("", "No suppliers found in API response.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load suppliers from API. Details: {Message}", ex.Message);
                ModelState.AddModelError("", "Could not load suppliers. Please try again later.");
                Suppliers = new List<SelectListItem>();
            }
        }

        private async Task LoadMedicalsAsync()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (!string.IsNullOrEmpty(accessToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }

                var response = await _httpClient.GetAsync($"{_baseUrl}/medical/");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call to /medical/ failed with status code {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    throw new HttpRequestException($"API call failed: {response.StatusCode}");
                }
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Medical API Response: {Content}", content);

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<MedicalResponseModel>>(content);
                if (apiResponse != null && apiResponse.Success && apiResponse.Results != null)
                {
                    Medicals = apiResponse.Results.Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name }).ToList();
                }
                else
                {
                    Medicals = new List<SelectListItem>();
                    ModelState.AddModelError("", "No medicals found in API response.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load medicals from API. Details: {Message}", ex.Message);
                ModelState.AddModelError("", "Could not load medicals. Please try again later.");
                Medicals = new List<SelectListItem>();
            }
        }
    }
}