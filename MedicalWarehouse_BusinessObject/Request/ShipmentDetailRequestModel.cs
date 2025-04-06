using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_BusinessObject.Request
{
    public class ShipmentDetailRequestModel
    {
        [Required(ErrorMessage = "Mã sản phẩm y tế là bắt buộc")]
        public Guid MedicalId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Ngày hết hạn là bắt buộc")]
        [DataType(DataType.DateTime)]
        [FutureDate(ErrorMessage = "Ngày hết hạn phải là một ngày trong tương lai")]
        public DateTime ExpiredDate { get; set; }

        public class FutureDateAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                if (value is DateTime dateTime)
                {
                    if (dateTime <= DateTime.Now)
                    {
                        return new ValidationResult(ErrorMessage ?? "Ngày phải là một ngày trong tương lai.");
                    }
                }
                return ValidationResult.Success;
            }
        }
    }
}