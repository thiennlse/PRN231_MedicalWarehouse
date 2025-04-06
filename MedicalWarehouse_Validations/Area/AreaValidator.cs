using FluentValidation;
using MedicalWarehouse_BusinessObject.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalWarehouse_Validations.Area
{
    public class AreaValidator : AbstractValidator<AreaRequestModel>
    {
        public AreaValidator()
        {
            RuleFor(x => x.AreaName)
                .NotEmpty().WithMessage("Tên khu vực là bắt buộc")
                .MaximumLength(100).WithMessage("Độ dài tên khu vực không được vượt quá 100 ký tự");
        }
    }
}