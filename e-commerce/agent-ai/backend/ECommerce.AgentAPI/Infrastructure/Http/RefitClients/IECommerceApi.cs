using ECommerce.AgentAPI.ECommerceClient.Dtos;

namespace ECommerce.AgentAPI.ECommerceClient;

/// <summary>
/// Cliente Refit para a <strong>API pública e de cliente</strong> do e-commerce (produtos, carrinho, pedidos e checkout).
/// Rotas de <strong>admin, auth, registo</strong> e similares não estão mapeadas — o Agent não as invoca.
/// <para>
/// O <c>BaseAddress</c> do <see cref="System.Net.Http.HttpClient"/> deve ser o prefixo <strong>sem</strong> barra no fim
/// (ex.: <c>http://localhost:7026/api/v1</c>); o Refit exige que cada rota comece com <c>/</c>.
/// </para>
/// </summary>
public interface IECommerceApi : IProductsApi, ICartApi, IOrdersApi, ICheckoutApi;
