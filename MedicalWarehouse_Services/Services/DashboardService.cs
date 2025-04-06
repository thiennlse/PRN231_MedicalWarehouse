using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_BusinessObject.Response;
using MedicalWarehouse_Repository.Interface;
using MedicalWarehouse_Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedicalWarehouse_Services.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IOrderRepository _order;

        public DashboardService(IDashboardRepository dashboardRepository, IOrderRepository order)
        {
            _dashboardRepository = dashboardRepository;
            _order = order;
        }

        public async Task<List<UpComingResponse>> GetAllUpComing()
        {
            try
            {
                var date = DateTime.UtcNow.ToUniversalTime();
                var dataNextTime = await _dashboardRepository.GetUpComingOrderAsync(date);

                var result = await dataNextTime
                    .Select(p => new UpComingResponse
                    {
                        Date = p.OrderDate.ToString("dd/MM/yyyy"),
                        Content = $"Upcoming Import Order by {p.User.UserName}"
                    })
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<BaseResponse<List<ColumnChartResponse>>> GetColumnChartAsync(DateTime date, OrderType type)
        {
            try
            {
                date = date == default ? DateTime.UtcNow : date;
                var newDate = date.ToUniversalTime();
                var weekStart = newDate.AddDays(-((int)date.DayOfWeek - 2)).ToUniversalTime();
                var weekEnd = weekStart.AddDays(6).ToUniversalTime();
                var dataThisWeek = await _dashboardRepository.GetImportOrdersInWeekAsync(weekStart, weekEnd, type);

                var result = await dataThisWeek
                    .GroupBy(p => p.OrderDate.DayOfWeek)
                    .Select(p => new ColumnChartResponse
                    {
                        DayOfWeek = p.Key,
                        Value = p.SelectMany(o => o.OrderDetails).Sum(t => t.Quantity)
                    })
                    .ToListAsync();

                return new BaseResponse<List<ColumnChartResponse>>
                {
                    Success = true,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ColumnChartResponse>>
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }

        public async Task<BaseResponse<CircelChartResponse>> GetColumnChartAsync(int year)
        {
            try
            {
                DateTime startOfYear = new DateTime(year, 1, 1, 0, 0, 0).ToUniversalTime();
                DateTime endOfYear = new DateTime(year, 12, 31, 23, 59, 59, 999).ToUniversalTime();
                DateTime startOfLastYear = new DateTime(year - 1, 1, 1, 0, 0, 0).ToUniversalTime();
                DateTime endOfLastYear = new DateTime(year - 1, 12, 31, 23, 59, 59, 999).ToUniversalTime();

                var dataThisYear = await _dashboardRepository.GetOrdersInYearAsync(startOfYear, endOfYear);
                var dataLastYear = await _dashboardRepository.GetOrdersInYearAsync(startOfLastYear, endOfLastYear);

                var result = new CircelChartResponse();
                foreach (var item in dataThisYear)
                {
                    if (item.Type == OrderType.Import)
                        result.Import += item.OrderDetails.Sum(p => p.Quantity);
                    else
                        result.Export += item.OrderDetails.Sum(p => p.Quantity);
                }

                var compare = new CircelChartResponse();
                foreach (var item in dataLastYear)
                {
                    if (item.Type == OrderType.Import)
                        compare.Import += item.OrderDetails.Sum(p => p.Quantity);
                    else
                        compare.Export += item.OrderDetails.Sum(p => p.Quantity);
                }

                result.Increace = dataLastYear.Any()
                    ? (result.Export - compare.Export) * 100 / compare.Export
                    : 100.00;

                return new BaseResponse<CircelChartResponse>
                {
                    Success = true,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<CircelChartResponse>
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }

        public async Task<BaseResponse<ProductSaleResponse>> GetProductSaleAsync(int year)
        {
            try
            {
                DateTime startOfYear = new DateTime(year, 1, 1, 0, 0, 0).ToUniversalTime();
                DateTime endOfYear = new DateTime(year, 12, 31, 23, 59, 59, 999).ToUniversalTime();
                DateTime startOfLastYear = new DateTime(year - 1, 1, 1, 0, 0, 0).ToUniversalTime();
                DateTime endOfLastYear = new DateTime(year - 1, 12, 31, 23, 59, 59, 999).ToUniversalTime();

                var dataThisYear = await _dashboardRepository.GetOrdersInYearAsync(startOfYear, endOfYear);
                var dataLastYear = await _dashboardRepository.GetOrdersInYearAsync(startOfLastYear, endOfLastYear);

                var exportResult = await dataThisYear
                    .Where(p => p.Type == OrderType.Export)
                    .GroupBy(p => p.OrderDate.Month)
                    .Select(o => new ProductSaleData
                    {
                        Month = o.Key,
                        Value = o.SelectMany(c => c.OrderDetails).Sum(r => r.TotalCost)
                    })
                    .ToListAsync();

                var importResult = await dataThisYear
                    .Where(p => p.Type == OrderType.Import)
                    .GroupBy(p => p.OrderDate.Month)
                    .Select(o => new ProductSaleData
                    {
                        Month = o.Key,
                        Value = o.SelectMany(c => c.OrderDetails).Sum(r => r.TotalCost)
                    })
                    .ToListAsync();

                var lastYearExport = await dataLastYear
                    .Where(p => p.Type == OrderType.Export)
                    .GroupBy(p => p.OrderDate.Month)
                    .Select(o => new ProductSaleData
                    {
                        Month = o.Key,
                        Value = o.SelectMany(c => c.OrderDetails).Sum(r => r.TotalCost)
                    })
                    .ToListAsync();

                // Fill missing months
                for (int month = 1; month <= 12; month++)
                {
                    if (!exportResult.Any(x => x.Month == month))
                        exportResult.Add(new ProductSaleData { Month = month, Value = 0 });
                    if (!importResult.Any(x => x.Month == month))
                        importResult.Add(new ProductSaleData { Month = month, Value = 0 });
                }

                exportResult = exportResult.OrderBy(x => x.Month).ToList();
                importResult = importResult.OrderBy(x => x.Month).ToList();

                var currentYearExportSum = exportResult.Sum(p => p.Value);
                var lastYearExportSum = lastYearExport.Sum(p => p.Value);

                return new BaseResponse<ProductSaleResponse>
                {
                    Success = true,
                    Result = new ProductSaleResponse
                    {
                        ExportData = exportResult,
                        ImportData = importResult,
                        Increase = lastYearExportSum > 0
                            ? (currentYearExportSum - lastYearExportSum) * 100 / lastYearExportSum
                            : 100,
                        Total = currentYearExportSum + importResult.Sum(p => p.Value)
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ProductSaleResponse>
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }
    }
}