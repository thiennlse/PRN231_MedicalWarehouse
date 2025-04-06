using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Response;

public class ProductSaleResponse
{
    public double Total { get; set; }
    public double Increase { get; set; }
    public List<ProductSaleData> ExportData { get; set; } = new List<ProductSaleData>();
    public List<ProductSaleData> ImportData { get; set; } = new List<ProductSaleData>();
}
