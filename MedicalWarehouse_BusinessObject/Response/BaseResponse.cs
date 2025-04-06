namespace MedicalWarehouse_BusinessObject.Response
{
    public class BaseResponse<T>
    {
        public bool Success { get; set; }
        public T? Result { get; set; }
        public List<T>? Results { get; set; }
        public string Message { get; set; }
    }
}
