using ECommerce.Application.DTOs;
using FluentValidation;

namespace ECommerce.Application.Validators;

public sealed class AddCartItemDtoValidator : AbstractValidator<AddCartItemDto>
{
    public AddCartItemDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1_000_000);
    }
}

public sealed class UpdateCartItemDtoValidator : AbstractValidator<UpdateCartItemDto>
{
    public UpdateCartItemDtoValidator()
    {
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1_000_000);
    }
}
