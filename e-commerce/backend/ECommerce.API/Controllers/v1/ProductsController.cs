using Asp.Versioning;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet("api/v{version:apiVersion}/products")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParams query, CancellationToken cancellationToken)
        => Ok(ApiResponse<PagedResult<ProductDto>>.Ok(await products.GetPagedAsync(query, cancellationToken)));

    [HttpGet("api/v{version:apiVersion}/products/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound(ApiResponse<string>.Fail("Produto não encontrado.")) : Ok(ApiResponse<ProductDto>.Ok(product));
    }

    [HttpPost("api/v{version:apiVersion}/admin/products")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        var created = await products.CreateAsync(dto, cancellationToken);
        return Created($"/api/v1/products/{created.Id}", ApiResponse<ProductDto>.Ok(created));
    }
}
