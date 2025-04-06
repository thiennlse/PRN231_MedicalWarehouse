using MedicalWarehouse_BusinessObject.Response;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using MedicalWareHouse_Client.Extensions;
namespace MedicalWareHouse_Client.Pages.ShipmentPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]


    public class IndexModel : PageModel
    {

        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContext;
        private readonly string _baseurl = "http://localhost:5273";

        public IndexModel(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContext = httpContextAccessor;
        }

        public List<ShipmentReponseModel> listShipment { get; set; } = new List<ShipmentReponseModel>();

        [BindProperty]
        public ShipmentReponseModel Shipment { get; set; } = default!;

        public Dictionary<string, string> AreaNames { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> SupplierNames { get; set; } = new Dictionary<string, string>();

        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10; // Số lượng mỗi trang
        public int TotalShipments { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalShipments / PageSize);

        // Search property
        [BindProperty(SupportsGet = true)]
        public string SearchByName { get; set; }

        // Error message
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync(int? page, string searchTerm)
        {
            try
            {
                // Handle pagination
                CurrentPage = page.HasValue && page > 0 ? page.Value : 1;

                // Handle search term - if passed as parameter use it, otherwise keep the model bound value
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    SearchByName = searchTerm;
                }

                SetAuthorizationHeader();
                await LoadAreasAsync();
                await LoadSuppliersAsync();

                // Load shipments based on whether search is being performed
                await LoadShipmentsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
                Console.WriteLine($"Error in OnGetAsync: {ex}");
            }
        }

        private async Task LoadShipmentsAsync()
        {
            try
            {
                // Reset the shipment list
                listShipment = new List<ShipmentReponseModel>();

                // If no search is being performed, load all shipments
                if (string.IsNullOrWhiteSpace(SearchByName))
                {
                    var shipmentResponse = await _httpClient.GetAsync($"{_baseurl}/shipment");
                    if (shipmentResponse.IsSuccessStatusCode)
                    {
                        var content = await shipmentResponse.Content.ReadAsStringAsync();
                        var responseObject = JsonConvert.DeserializeObject<BaseResponse<ShipmentReponseModel>>(content);
                        if (responseObject != null && responseObject.Success && responseObject.Results.Any())
                        {
                            listShipment = responseObject.Results;
                            TotalShipments = listShipment.Count;
                        }
                    }
                    else
                    {
                        ErrorMessage = $"Lỗi khi tải dữ liệu: {shipmentResponse.StatusCode}";
                    }
                }
                else // Search is being performed
                {
                    // MODIFICATION: First load all shipments and filter locally
                    // This is more reliable for exact matches
                    var allShipmentsResponse = await _httpClient.GetAsync($"{_baseurl}/shipment");
                    if (allShipmentsResponse.IsSuccessStatusCode)
                    {
                        var allContent = await allShipmentsResponse.Content.ReadAsStringAsync();
                        var allResponseObject = JsonConvert.DeserializeObject<BaseResponse<ShipmentReponseModel>>(allContent);
                        
                        if (allResponseObject != null && allResponseObject.Success && allResponseObject.Results.Any())
                        {
                            // Filter client-side by name - use multiple approaches to ensure matches
                            var searchTerm = SearchByName.Trim();
                            
                            // Try exact match first
                            var exactMatches = allResponseObject.Results
                                .Where(s => s.Name != null && 
                                          (s.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                           s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                                .ToList();
                            
                            if (exactMatches.Any())
                            {
                                listShipment = exactMatches;
                                TotalShipments = listShipment.Count;
                                Console.WriteLine($"Found {listShipment.Count} matches with client-side filtering");
                                return; // Exit early if we found matches
                            }
                        }
                    }
                    
                    // If client-side filtering didn't find results, try API search with several approaches
                    Console.WriteLine("Client-side search did not find matches, trying API search...");
                    
                    // 1. Try with URL encoding
                    string encodedSearch = HttpUtility.UrlEncode(SearchByName);
                    var url = $"{_baseurl}/shipment?search={encodedSearch}";
                    Console.WriteLine($"API Search URL: {url}");
                    
                    var shipmentResponse = await _httpClient.GetAsync(url);
                    
                    if (shipmentResponse.IsSuccessStatusCode)
                    {
                        var content = await shipmentResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"API Response: {content}");
                        
                        var responseObject = JsonConvert.DeserializeObject<BaseResponse<ShipmentReponseModel>>(content);
                        if (responseObject != null && responseObject.Success && responseObject.Results.Any())
                        {
                            listShipment = responseObject.Results;
                            TotalShipments = listShipment.Count;
                            Console.WriteLine($"Found {listShipment.Count} matches with API search");
                            return;
                        }
                    }
                    
                    // 2. Try direct API filtering with OData-style filter
                    var filterUrl = $"{_baseurl}/shipment?$filter=contains(name,'{encodedSearch}')";
                    Console.WriteLine($"API Filter URL: {filterUrl}");
                    
                    var filterResponse = await _httpClient.GetAsync(filterUrl);
                    
                    if (filterResponse.IsSuccessStatusCode)
                    {
                        var content = await filterResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"API Filter Response: {content}");
                        
                        var responseObject = JsonConvert.DeserializeObject<BaseResponse<ShipmentReponseModel>>(content);
                        if (responseObject != null && responseObject.Success && responseObject.Results.Any())
                        {
                            listShipment = responseObject.Results;
                            TotalShipments = listShipment.Count;
                            Console.WriteLine($"Found {listShipment.Count} matches with API filter");
                            return;
                        }
                    }
                    
                    // 3. Try with a broader filter if all else fails
                    try {
                        // Load all shipments again (in case the first attempt failed)
                        var fallbackResponse = await _httpClient.GetAsync($"{_baseurl}/shipment");
                        if (fallbackResponse.IsSuccessStatusCode)
                        {
                            var allContent = await fallbackResponse.Content.ReadAsStringAsync();
                            var allShipments = JsonConvert.DeserializeObject<BaseResponse<ShipmentReponseModel>>(allContent);
                            
                            if (allShipments != null && allShipments.Success && allShipments.Results.Any())
                            {
                                // Try fuzzy matching - look for parts of the search term in the name
                                var searchParts = SearchByName.Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
                                var fuzzyMatches = allShipments.Results
                                    .Where(s => s.Name != null && 
                                              (searchParts.Any(part => 
                                                  s.Name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)))
                                    .ToList();
                                
                                if (fuzzyMatches.Any())
                                {
                                    listShipment = fuzzyMatches;
                                    TotalShipments = listShipment.Count;
                                    Console.WriteLine($"Found {listShipment.Count} matches with fuzzy search");
                                    return;
                                }
                                
                                // No matches found after all attempts
                                ErrorMessage = $"Không tìm thấy kết quả cho '{SearchByName}' sau khi thử nhiều cách tìm kiếm khác nhau.";
                            }
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Error in fallback search: {ex.Message}");
                        // Continue execution to show no results
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải dữ liệu lô hàng: {ex.Message}";
                Console.WriteLine($"Error in LoadShipmentsAsync: {ex}");
            }
        }

        private void SetAuthorizationHeader()
        {
            var token = _httpContext.HttpContext.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        private async Task LoadAreasAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseurl}/area");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<BaseResponse<AreaResponseModel>>(content);
                    if (apiResponse != null && apiResponse.Success && apiResponse.Results != null)
                    {
                        AreaNames = apiResponse.Results.ToDictionary(
                            a => a.AreaId.ToString(),
                            a => a.AreaName
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading areas: {ex.Message}");
            }
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseurl}/supplier");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var suppliers = JsonConvert.DeserializeObject<List<SupplierResponse>>(content);
                    if (suppliers != null && suppliers.Count > 0)
                    {
                        SupplierNames = suppliers.ToDictionary(
                            s => s.Id.ToString(),
                            s => s.Name
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading suppliers: {ex.Message}");
            }
        }
    }
}