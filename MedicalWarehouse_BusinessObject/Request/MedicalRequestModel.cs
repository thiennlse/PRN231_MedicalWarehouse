using MedicalWarehouse_BusinessObject.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Request
{
    public class MedicalRequestModel
    {
        public string Name { get; set; }
        public List<string> Image { get; set; } = new List<string>();
        public string TypeMedical { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public Guid SupplierId { get; set; }
    }
}
