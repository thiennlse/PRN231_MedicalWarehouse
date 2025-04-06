using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Request
{
    public class PaymentRequestModel
    {
        public Guid MedicalId { get; set; }
        public int Quantity { get; set; }
    }
}
