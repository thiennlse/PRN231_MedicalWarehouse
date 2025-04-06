using MedicalWarehouse_BusinessObject.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Response
{
    public class MedicalResponseModel
    {
        public Guid Id { get; set; }
        public List<string> Image { get; set; } = new List<string>();
        public string TypeMedical { get; set; }
        public string Unit {  get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string SupplierName { get; set; }
        public Guid SupplierId { get; set; }
        public string Name { get; set; }
    }
}
