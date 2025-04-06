using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Entity
{
    public class Medical : BaseEntity
    {
        public List<string> Image {  get; set; } = new List<string>();
        public string TypeMedical {  get; set; }
        public string Unit {  get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public Guid SupplierId { get; set; }
        public Supplier Supplier { get; set; }
        public ICollection<ShipmentDetail> ShipmentDetails { get; set; } = new List<ShipmentDetail>();
    }
}
