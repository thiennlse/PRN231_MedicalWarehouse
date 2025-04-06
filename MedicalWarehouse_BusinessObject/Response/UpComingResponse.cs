using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Response
{
    public class UpComingResponse
    {
        public Guid Id { get; set; }
        public string Date { get; set; }
        public string Content { get; set; }
    }
}
