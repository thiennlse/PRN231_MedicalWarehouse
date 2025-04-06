using MedicalWarehouse_BusinessObject.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Request
{
    public class AreaRequestModel
    {
        public string AreaName { get; set; }
        List<Guid>? Shipments { get; set; }
        List<Guid>? Orders { get; set; }
    }
}
