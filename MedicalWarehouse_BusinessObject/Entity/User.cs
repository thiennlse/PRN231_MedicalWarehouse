using Microsoft.AspNetCore.Identity;

namespace MedicalWarehouse_BusinessObject.Entity
{
    public class User : IdentityUser
    {
        public ICollection<Order>? Orders { get; set; } = new List<Order>();
        public string? Image { get; set; }
        public string? VerifyToken { get; set; }
        public DateTime? VerifyTokenExpired { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Role {  get; set; }
    }
}
