using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Response
{
    public class OrderDetailsResponseModel
    {
        public Guid MedicalId { get; set; }
        public int OrderQuantity { get; set; }
        public double TotalCost { get; set; }
        public DateTime ExpiredDate { get; set; }
    }
}
