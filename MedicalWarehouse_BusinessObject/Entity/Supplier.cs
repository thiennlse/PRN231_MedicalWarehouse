using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Entity
{
    public class Supplier : BaseEntity
    {
        public string? PhoneNumber {  get; set; }
        public string? ContactEmail { get; set; }
        public ICollection<Shipment>? Shipments { get; set; } = new List<Shipment>();
    }
}
