using FluentValidation;
using MedicalWarehouse_BusinessObject.Request;

namespace MedicalWarehouse_Validations.Medical
{
    public class MedicalValidator : AbstractValidator<MedicalRequestModel>
    {
        public MedicalValidator()
        {
            RuleFor(m => m.Name)
                .NotEmpty().WithMessage("Tên là bắt buộc.")
                .MaximumLength(100).WithMessage("Tên không được vượt quá 100 ký tự.");

            RuleFor(m => m.Image)
                .NotNull().WithMessage("Danh sách hình ảnh không được để trống.")
                .Must(images => images.Count > 0).WithMessage("Cần ít nhất một hình ảnh.");

            RuleFor(m => m.TypeMedical)
                .NotEmpty().WithMessage("Loại sản phẩm y tế là bắt buộc.");

            RuleFor(m => m.Description)
                .NotEmpty().WithMessage("Mô tả là bắt buộc.")
                .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.");

            RuleFor(m => m.Price)
                .GreaterThan(0).WithMessage("Giá phải lớn hơn 0.");

            RuleFor(m => m.SupplierId)
                .NotEmpty().WithMessage("Mã nhà cung cấp là bắt buộc.");
        }
    }
}