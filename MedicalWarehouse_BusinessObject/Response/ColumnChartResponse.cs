using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Response;

public class ColumnChartResponse
{
    public DayOfWeek DayOfWeek { get; set; }
    public int Value { get; set; }
}
