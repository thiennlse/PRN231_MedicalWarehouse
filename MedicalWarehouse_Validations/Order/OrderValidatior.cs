using FluentValidation;
using MedicalWarehouse_BusinessObject.Request;

namespace MedicalWarehouse_Validations.Area
{
    public class OrderValidator : AbstractValidator<OrderRequestModel>
    {
        public OrderValidator()
        {
            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Loại đơn hàng là bắt buộc");
        }
    }
}