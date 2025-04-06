using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Entity
{
    public class OrderDetail : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Order Orders { get; set; }
        public Guid MedicalId { get; set; }
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
        public int Quantity { get; set; }
        public double TotalCost {  get; set; }
    }
}
