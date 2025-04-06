namespace MedicalWarehouse_BusinessObject.Response;

public class ShipmentDetailResponse
{
    public Guid MedicalId { get; set; }
    public string MedicalName { get; set; }
    public string TypeMedical { get; set; }
    public string MedicalDescription { get; set; }
    public double Price { get; set; }
    public int Quantity { get; set; }
    public DateTime ExpiredDate { get; set; }
}
