using MedicalWarehouse_BusinessObject.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Response
{
    public class ShipmentReponseModel
    {
        public Guid ShipmentId { get; set; }
        public string Name { get; set; }
        public Guid AreaId { get; set; }
        public Guid SupplierId { get; set; }
        public DateTime ShipDate { get; set; }
        public List<ShipmentDetailResponse> ShipmentDetails { get; set; } = new List<ShipmentDetailResponse>();
    }
}
