using MedicalWarehouse_BusinessObject.Enums;
using MedicalWarehouse_BusinessObject.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Services.Interface;

public interface IDashboardService
{
    Task<List<UpComingResponse>> GetAllUpComing();
    Task<BaseResponse<List<ColumnChartResponse>>> GetColumnChartAsync(DateTime date, OrderType type);
    Task<BaseResponse<CircelChartResponse>> GetColumnChartAsync(int year);
    Task<BaseResponse<ProductSaleResponse>> GetProductSaleAsync(int year);
}
