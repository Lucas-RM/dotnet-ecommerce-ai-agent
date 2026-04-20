using ECommerce.Application.DTOs;
using FluentValidation;

namespace ECommerce.Application.Validators;

public sealed class OrderQueryParamsValidator : AbstractValidator<OrderQueryParams>
{
    public OrderQueryParamsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}

public sealed class AdminOrderQueryParamsValidator : AbstractValidator<AdminOrderQueryParams>
{
    public AdminOrderQueryParamsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}

public sealed class AdminUserQueryParamsValidator : AbstractValidator<AdminUserQueryParams>
{
    public AdminUserQueryParamsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
