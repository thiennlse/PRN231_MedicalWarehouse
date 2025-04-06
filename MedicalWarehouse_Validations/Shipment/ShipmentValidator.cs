using FluentValidation;
using MedicalWarehouse_BusinessObject.Request;

namespace MedicalWarehouse_Validations.Shipment
{
    public class ShipmentValidator : AbstractValidator<ShipmentRequestModel>
    {
        public ShipmentValidator()
        {
            RuleFor(x => x.ShipDate)
                .NotNull().WithMessage("Ngày gửi là bắt buộc")
                .GreaterThanOrEqualTo(DateTime.UtcNow.ToUniversalTime()).WithMessage("Ngày gửi phải là một ngày trong tương lai");

            RuleFor(x => x.AreaId)
                .NotNull().WithMessage("Mã khu vực là bắt buộc");

            // Không yêu cầu SupplierId vì nó có thể được suy ra từ thông tin thuốc
            // RuleFor(x => x.SupplierId)
            //     .NotNull().WithMessage("Mã nhà cung cấp là bắt buộc");
        }
    }
}