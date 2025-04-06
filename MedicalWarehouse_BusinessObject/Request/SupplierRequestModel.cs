using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Request
{
    public class SupplierRequestModel
    {
        public string? Name { get; set; }
        [Phone]
        public string? PhoneNumber { get; set; }
        [EmailAddress]
        public string? ContactEmail { get; set; }
    }
}
