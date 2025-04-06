using MedicalWarehouse_BusinessObject.Entity;
using MedicalWarehouse_BusinessObject.Response;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Text;
using Azure;
using MedicalWarehouse_BusinessObject.Request;
using MedicalWarehouse_BusinessObject.Contract;
using Microsoft.EntityFrameworkCore;
using MedicalWarehouse_BusinessObject.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MedicalWareHouse_Client.Extensions;

namespace MedicalWareHouse_Client.Pages.AreaPage
{
    [RequireAuthentication("ADMIN", "PHARMACY", "STAFF")]
    public class DashboardModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContext;
        private readonly string _baseurl = "http://localhost:5273";
        private readonly ApplicationDbContext _context;
        
        public int[] Icolums = [0, 0, 0, 0, 0, 0, 0];
        public int[] Ecolums = [0, 0, 0, 0, 0, 0, 0];
        public int[] CircelChart = [0, 0];
        public double Increase = 0;
        public double[] ExportSale = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        public double[] ImportSale = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        public double PIncrease = 0;
        [BindProperty]
        public int CurrentPage { get; set; } = 1;
        [BindProperty]
        public int PageSize { get; set; } = 4;
        [BindProperty]
        public bool hasPrePage { get; set; } = false;
        [BindProperty]
        public bool hasNextPage { get; set; } = true;
        public IList<Order> ImportOrders { get; set; } = new List<Order>();
        [BindProperty]
        public DateTime date { get; set; }

        public bool IsStaff { get; private set; }
        
        public DashboardModel(IHttpContextAccessor httpContext, ApplicationDbContext context)
        {
            _httpClient = new HttpClient();
            _httpContext = httpContext;
            _context = context;
            LoadImportOrders();
        }

        public async Task OnGetAsync(DateTime? date)
        {
            try
            {
                SetAuthorizationHeader();
                
                // Check if user is Staff
                IsStaff = GetUserRoles().Contains(UserRoles.STAFF);
                
                if (date == default)
                {
                    this.date = DateTime.UtcNow.ToUniversalTime();
                }
                else
                {
                    this.date = date.Value.ToUniversalTime();
                }
                var iColumnReponse = await _httpClient.GetAsync($"{_baseurl}/Dashboard/get?date={this.date}&type=1");
                var eColumnReponse = await _httpClient.GetAsync($"{_baseurl}/Dashboard/get?date={this.date}&type=0");
                var circleReponse = await _httpClient.GetAsync($"{_baseurl}/Dashboard/get-by-year?year={this.date.Year}");
                var psResponse = await _httpClient.GetAsync($"{_baseurl}/Dashboard/product-sale?year={this.date.Year}");
                if (psResponse.IsSuccessStatusCode || iColumnReponse.IsSuccessStatusCode || eColumnReponse.IsSuccessStatusCode || circleReponse.IsSuccessStatusCode)
                {
                    var content = await iColumnReponse.Content.ReadAsStringAsync();
                    var reponse = JsonConvert.DeserializeObject<BaseResponse<List<ColumnChartResponse>>>(content);
                    foreach (var item in reponse.Result)
                    {
                        if (item.DayOfWeek == DayOfWeek.Sunday)
                        {
                            Icolums[6] = item.Value;
                        }
                        else
                        {
                            Icolums[(int)item.DayOfWeek - 1] = item.Value;
                        }
                    }
                    var contente = await eColumnReponse.Content.ReadAsStringAsync();
                    var reponsee = JsonConvert.DeserializeObject<BaseResponse<List<ColumnChartResponse>>>(contente);
                    foreach (var item in reponsee.Result)
                    {
                        if (item.DayOfWeek == DayOfWeek.Sunday)
                        {
                            Ecolums[6] = item.Value;
                        }
                        else
                        {
                            Ecolums[(int)item.DayOfWeek - 1] = item.Value;
                        }
                    }
                    var circleContent = await circleReponse.Content.ReadAsStringAsync();
                    var responseC = JsonConvert.DeserializeObject<BaseResponse<CircelChartResponse>>(circleContent);
                    CircelChart[0] = responseC.Result.Import;
                    CircelChart[1] = responseC.Result.Export;
                    Increase = responseC.Result.Increace;

                    var psContent = await psResponse.Content.ReadAsStringAsync();
                    var reponseP = JsonConvert.DeserializeObject<BaseResponse<ProductSaleResponse>>(psContent);
                    foreach (var item in reponseP.Result.ExportData)
                    {
                        this.ExportSale[item.Month - 1] = item.Value;
                    }
                    foreach (var item in reponseP.Result.ImportData)
                    {
                        this.ImportSale[item.Month - 1] = item.Value;
                    }
                    PIncrease = reponseP.Result.Increase;
                }
                
                // Tải danh sách đơn hàng nhập
                LoadImportOrders();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"HTTP Request Error: {e.Message}");
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                Console.WriteLine($"JSON Deserialization Error: {e.Message}");
            }
        }

        public async Task<IActionResult> OnPostUpAsync(int? CurrentPage)
        {
            LoadImportOrders();
            return RedirectToPage();
        }
        
        private void LoadImportOrders()
        {
            try
            {
                ImportOrders = _context.Orders
                    .Include(o => o.User)
                    .Where(o => o.IsDeleted != true && o.Type == OrderType.Import)
                    .OrderByDescending(o => o.CreatedDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading import orders: {ex.Message}");
                ImportOrders = new List<Order>();
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
        
        private List<string> GetUserRoles()
        {
            var token = _httpContext?.HttpContext?.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                return new List<string>();
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role");
            return roleClaims.Select(c => c.Value).ToList();
        }
    }
}