using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Request
{
    public class CheckOutRequestModel
    {
        public List<PaymentRequestModel> OrderRequest { get; set; }
        public string CancelUrl { get; set; }
        public string ReturnUrl { get; set; }
    }
}
