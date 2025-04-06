namespace MedicalWarehouse_BusinessObject.Response
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiredTime { get; set; }
        public string? AccessToken { get; set; }
    }
}
