using ECommerce.Application.DTOs;
using FluentValidation;

namespace ECommerce.Application.Validators;

public sealed class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Name).Must(n => string.IsNullOrWhiteSpace(n) == false).When(x => x.Name is not null)
            .WithMessage("Nome não pode ser vazio.");

        RuleFor(x => x.Description).MaximumLength(4000).When(x => x.Description is not null);

        RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price.HasValue);

        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).When(x => x.StockQuantity.HasValue);

        RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
        RuleFor(x => x.Category).Must(c => string.IsNullOrWhiteSpace(c) == false).When(x => x.Category is not null)
            .WithMessage("Categoria não pode ser vazia.");
    }
}

public sealed class ProductQueryParamsValidator : AbstractValidator<ProductQueryParams>
{
    public ProductQueryParamsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
    }
}
