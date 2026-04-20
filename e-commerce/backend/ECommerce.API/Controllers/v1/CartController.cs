using Asp.Versioning;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "Customer")]
[Route("api/v{version:apiVersion}/cart")]
public sealed class CartController(ICartService cartService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = UserId();
        var cart = await cartService.GetCartAsync(userId, cancellationToken);
        return Ok(ApiResponse<CartDto>.Ok(cart));
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemDto dto, CancellationToken cancellationToken)
    {
        var cart = await cartService.AddItemAsync(UserId(), dto, cancellationToken);
        return Ok(ApiResponse<CartDto>.Ok(cart));
    }

    [HttpPut("items/{productId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid productId, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken)
    {
        var cart = await cartService.UpdateItemAsync(UserId(), productId, dto, cancellationToken);
        return Ok(ApiResponse<CartDto>.Ok(cart));
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken cancellationToken)
    {
        var cart = await cartService.RemoveItemAsync(UserId(), productId, cancellationToken);
        return Ok(ApiResponse<CartDto>.Ok(cart));
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        await cartService.ClearCartAsync(UserId(), cancellationToken);
        return NoContent();
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
