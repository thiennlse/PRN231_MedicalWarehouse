using MedicalWarehouse_BusinessObject.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Response
{
    public class AreaResponseModel
    {
        public Guid AreaId { get; set; }
        public string AreaName { get; set; }
        public List<ShipmentReponseModel> Shipment { get; set; } = new List<ShipmentReponseModel>();
        public List<Order> Order { get; set; } = new List<Order>();
    }
}
