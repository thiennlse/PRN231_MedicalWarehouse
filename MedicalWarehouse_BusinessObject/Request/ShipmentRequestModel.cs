using System.ComponentModel.DataAnnotations;

namespace MedicalWarehouse_BusinessObject.Request
{
    public class ShipmentRequestModel
    {
        [Required]
        public Guid AreaId { get; set; }
        
        // SupplierId không còn là required, có thể được suy ra từ thuốc
        public Guid SupplierId { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ShipDate { get; set; }
        public Guid? OrderId { get; set; }
        public List<ShipmentDetailRequestModel>? ShipmentDetails { get; set; } = new List<ShipmentDetailRequestModel>();
    }
}
