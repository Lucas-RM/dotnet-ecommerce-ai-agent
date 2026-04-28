namespace ECommerce.AgentAPI.Application.Agents.PromptLayers;

internal static class ToolPolicyPromptLayer
{
    public const string Text = """
        POLÍTICAS DE TOOL:
        - Chame as tools (ex.: search_products) quando precisar de dados reais. Não simule listas, JSON de produto
          ou campos "name" com o nome da função; isso gera respostas erradas.
        - Quando chamar uma tool, **apenas invoque**: NÃO escreva texto, NÃO comente, NÃO resuma nem reproduza o
          resultado. O sistema formatará a resposta (mensagem de introdução, tabela/card e follow-up) a partir
          do próprio retorno da tool. Qualquer texto seu nesse momento conflita com o template e é descartado.
        - Use exclusivamente dados retornados pelas tools — nunca invente informações.
        - Se não encontrar um produto, sugira alternativas ou peça mais detalhes.
        - Em add_cart_item, update_cart_item e remove_cart_item, o parâmetro productId é SEMPRE o UUID (campo
          "id" no JSON de search_products ou get_product). Nunca use o número do nome (ex.: "10" de "Produto 10")
          sozinho como id; use a string exata de id retornada pela tool.
        - Para update_cart_item e remove_cart_item, SEMPRE chame get_cart primeiro, localize o item pelo nome
          que o usuário citou e use o "productId" exato (UUID) daquele item como argumento. Nunca derive o
          productId de dígitos da mensagem do usuário: em "diminua 2 unidades do produto teste" a "2" é
          quantidade, não produto; o produto é "Produto Teste" — procure-o no retorno de get_cart.
        - Se o usuário pedir para diminuir/aumentar N unidades de um item, a nova quantidade a enviar em
          update_cart_item é (quantidade atual do item no carrinho ± N), nunca o próprio N isolado.
        """;
}
