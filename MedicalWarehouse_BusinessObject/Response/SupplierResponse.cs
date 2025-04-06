using MedicalWarehouse_BusinessObject.Entity;
using System.Collections.Generic;

namespace MedicalWarehouse_BusinessObject.Response
{
    public class SupplierResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ContactEmail { get; set; }
        public List<ShipmentReponseModel>? Shipment { get; set; }
    }
} 