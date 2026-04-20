# E-Commerce — API .NET + Angular

## Descrição

Este repositório contém uma solução de e-commerce com **API REST em ASP.NET Core** (`ECommerce.API`), **frontend em Angular** (`ecommerce-web`) e persistência em **SQL Server** via **Entity Framework Core**. A API segue versionamento em URL (`/api/v1/...`), autenticação **JWT** com **refresh token** em cookie HTTP-only, e papéis **Admin** e **Customer**.

**Principais funcionalidades (conforme implementação):**

- **Autenticação:** registro, login, refresh de token, revogação (logout).
- **Produtos:** listagem paginada com filtros (categoria, busca), detalhe por ID; criação de produtos (Admin).
- **Carrinho:** consulta, inclusão/atualização/remoção de itens, limpeza (Customer).
- **Pedidos:** checkout a partir do carrinho, listagem e detalhe dos pedidos do cliente; listagem administrativa de pedidos (Admin).
- **Usuários:** perfil do usuário autenticado; listagem paginada de usuários (Admin).

Em ambiente **Development**, a API executa um **seed** inicial (usuários de exemplo e produtos) e redireciona a raiz `/` para o **Swagger**.

---

## Tecnologias e ferramentas

| Tecnologia | Versão / observação |
|------------|---------------------|
| **.NET** | 8.0 (`net8.0`) |
| **ASP.NET Core** | 8.x (pacotes alinhados a 8.0.12 onde aplicável) |
| **Entity Framework Core** | 8.0.12 (`Microsoft.EntityFrameworkCore.SqlServer`, Tools, Design) |
| **SQL Server** | Provider `Microsoft.EntityFrameworkCore.SqlServer` |
| **Angular** | 17.3.x (aplicação `ecommerce-web`) |
| **Angular Material / CDK** | 17.3.x |
| **TypeScript** | ~5.4.2 |
| **RxJS** | ~7.8.0 |
| **Node.js** | Não fixado em `package.json`; para Angular 17 costuma-se **Node 18.x ou 20.x LTS** (requisito típico do ecossistema Angular CLI 17). |

**Bibliotecas relevantes (backend):**

- **JWT:** `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.12  
- **AutoMapper:** `AutoMapper.Extensions.Microsoft.DependencyInjection` 12.0.1  
- **FluentValidation:** `FluentValidation` 12.1.1 + `FluentValidation.AspNetCore` 11.3.1  
- **API versioning:** `Asp.Versioning.Mvc` / `Asp.Versioning.Mvc.ApiExplorer` 8.1.1  
- **Documentação OpenAPI:** `Swashbuckle.AspNetCore` 10.1.7, `Microsoft.OpenApi` 2.4.1  
- **Hash de senha:** `BCrypt.Net-Next` 4.1.0  
- **Logging:** `Serilog.AspNetCore` 10.0.0, `Serilog.Sinks.File` 7.0.0  

**Testes (`ECommerce.Tests`):** xUnit 2.5.3, FluentAssertions 8.9.0, Moq 4.20.72, Coverlet 6.0.0.

---

## Estrutura do projeto

```
dotnet-ecommerce-ai-agent/
├── e-commerce/
│   ├── backend/
│   │   ├── ECommerce.sln
│   │   ├── ECommerce.API/          # Host web, controllers, middlewares, Swagger, Program.cs
│   │   ├── ECommerce.Application/  # Serviços, DTOs, validadores, interfaces
│   │   ├── ECommerce.Domain/       # Entidades e enums
│   │   ├── ECommerce.Infrastructure/  # EF Core (DbContext), repositórios, migrations
│   │   └── ECommerce.Tests/        # Testes unitários
│   └── frontend/                   # Angular (ecommerce-web)
│       ├── src/
│       ├── angular.json
│       └── package.json
└── README.md
```

- **Clean Architecture** em camadas: `Domain` → `Application` → `Infrastructure`; `API` compõe a injeção de dependência e expõe os endpoints.

---

## Como executar o projeto

### Pré-requisitos gerais

- [.NET SDK 8.0](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) acessível pela connection string
- Para o frontend: [Node.js](https://nodejs.org/) (recomenda-se LTS compatível com Angular 17) e **npm** (definido em `angular.json` como `packageManager`)

---

### Backend (.NET)

1. **Configurar `ConnectionStrings:DefaultConnection`** em `e-commerce/backend/ECommerce.API/appsettings.json` (ou variáveis de ambiente / `appsettings.Development.json`) apontando para sua instância SQL Server e banco desejado (ex.: catálogo `ECommerceDb`).

2. **JWT** — ajuste em `appsettings.json` a seção `Jwt`:

   - `SecretKey`, `Issuer`, `Audience`
   - `AccessTokenExpirationMinutes` (padrão no código: **15**)
   - `RefreshTokenExpirationDays` (padrão: **7**)

   O código valida assinatura simétrica com a chave configurada; use uma chave forte e adequada ao ambiente (evite commitar segredos reais).

3. **Aplicar migrations** (não há `Database.Migrate()` automático no `Program.cs`; é necessário atualizar o banco manualmente):

   ```bash
   cd e-commerce/backend
   dotnet ef database update --project ECommerce.Infrastructure/ECommerce.Infrastructure.csproj --startup-project ECommerce.API/ECommerce.API.csproj
   ```

4. **Executar a API:**

   ```bash
   cd e-commerce/backend/ECommerce.API
   dotnet run
   ```

   URLs de desenvolvimento (conforme `launchSettings.json`):

   - HTTP: `http://localhost:5149`
   - HTTPS: `https://localhost:7026`

   Em **Development**, o Swagger fica em `/swagger` (e `GET /` redireciona para ele).

5. **Seed (Development):** ao iniciar em `Development`, `DataSeeder` cria usuários e produtos iniciais **se as tabelas estiverem vazias**:

   - Admin: `admin@ecommerce.com` / `Admin@123456`
   - Customer: `user@ecommerce.com` / `User@123456`
   - 10 produtos de exemplo (`Produto 1` … `Produto 10`)

6. **Testes:**

   ```bash
   cd e-commerce/backend
   dotnet test
   ```

---

### Frontend (Angular)

```bash
cd e-commerce/frontend
npm install
npm start
```

Por padrão o dev server do Angular usa a porta **4200**. A URL base da API está em `src/environments/environment.ts` e `environment.production.ts`:

- `apiUrl: 'https://localhost:7026/api/v1'`

Ajuste se a API rodar em outro host/porta. O CORS da API em **Development** permite origens como `http://localhost:4200` e `https://localhost:4200` (e as URLs da própria API), conforme `DependencyInjectionExtensions.cs`.

---

## Formato de resposta da API

As respostas usam o envelope `ApiResponse<T>`:

```json
{
  "success": true,
  "data": { },
  "message": null,
  "errors": null
}
```

Em erro de validação do modelo, a API pode retornar **400** com `success: false`, `message` e `errors` (coleção de mensagens).

---

## Endpoints da API

Versão padrão da API: **1.0**. Rotas abaixo usam o prefixo **`/api/v1`**.

### Raiz (somente Development)

| Método | URL | Descrição |
|--------|-----|-----------|
| GET | `/` | Redireciona para `/swagger`. |

---

### Autenticação (`/api/v1/auth`)

| Método | URL | Auth | Descrição |
|--------|-----|------|-----------|
| POST | `/api/v1/auth/register` | Anônimo | Registra novo usuário. Corpo: `RegisterDto` (`name`, `email`, `password`, `confirmPassword`). Resposta **201** com `UserDto`. |
| POST | `/api/v1/auth/login` | Anônimo | Login. Corpo: `LoginDto` (`email`, `password`). Resposta **200** com `AuthResponseDto`; cookie `refreshToken`. |
| POST | `/api/v1/auth/refresh` | Cookie refresh | Renova access token. Resposta **200** com `AuthResponseDto`; atualiza cookie. |
| POST | `/api/v1/auth/revoke` | Bearer | Revoga refresh tokens do usuário e remove cookie. Resposta **204**. |

**Exemplo de request (login):**

```json
{
  "email": "user@ecommerce.com",
  "password": "User@123456"
}
```

**Exemplo de response (login / refresh) — campos principais em `data`:**

```json
{
  "success": true,
  "data": {
    "accessToken": "<jwt>",
    "expiresIn": 900,
    "role": "Customer"
  }
}
```

---

### Produtos

| Método | URL | Auth | Descrição |
|--------|-----|------|-----------|
| GET | `/api/v1/products` | Anônimo | Lista paginada. Query: `page`, `pageSize`, `category`, `search`. Resposta: `PagedResult<ProductDto>`. |
| GET | `/api/v1/products/{id}` | Anônimo | Detalhe do produto. **404** se não existir. |
| POST | `/api/v1/admin/products` | Bearer **Admin** | Cria produto. Corpo: `CreateProductDto` (`name`, `description`, `price`, `stockQuantity`, `category`). Resposta **201** com `ProductDto`. |

**Exemplo de request (criar produto):**

```json
{
  "name": "Notebook",
  "description": "Ultrafino 14\"",
  "price": 3999.90,
  "stockQuantity": 5,
  "category": "Eletrônicos"
}
```

---

### Carrinho (`/api/v1/cart`) — Bearer **Customer**

| Método | URL | Descrição |
|--------|-----|-----------|
| GET | `/api/v1/cart` | Retorna o carrinho (`CartDto`: itens e `totalPrice`). |
| POST | `/api/v1/cart/items` | Adiciona item. Corpo: `AddCartItemDto` (`productId`, `quantity`). |
| PUT | `/api/v1/cart/items/{productId}` | Atualiza quantidade. Corpo: `UpdateCartItemDto` (`quantity`). |
| DELETE | `/api/v1/cart/items/{productId}` | Remove item. |
| DELETE | `/api/v1/cart` | Limpa o carrinho. Resposta **204**. |

---

### Pedidos

| Método | URL | Auth | Descrição |
|--------|-----|------|-----------|
| POST | `/api/v1/orders/checkout` | Bearer **Customer** | Finaliza pedido a partir do carrinho. Resposta **201** com `Location` `/api/v1/orders/{id}` e `OrderDto`. |
| GET | `/api/v1/orders` | Bearer **Customer** | Lista pedidos do cliente. Query: `page`, `pageSize`. `PagedResult<OrderSummaryDto>`. |
| GET | `/api/v1/orders/{id}` | Bearer **Customer** | Detalhe do pedido. **404** se não pertencer ao cliente. |
| GET | `/api/v1/admin/orders` | Bearer **Admin** | Lista todos (filtros admin). Query: `page`, `pageSize`, `userId`, `status` (`OrderStatus`). |

---

### Usuários

| Método | URL | Auth | Descrição |
|--------|-----|------|-----------|
| GET | `/api/v1/users/me` | Bearer | Perfil do usuário logado (`UserDto`). **404** se não encontrado. |
| GET | `/api/v1/admin/users` | Bearer **Admin** | Lista paginada para administração. Query: `page`, `pageSize`. `PagedResult<UserDto>`. |