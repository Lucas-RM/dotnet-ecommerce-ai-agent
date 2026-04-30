# E-Commerce — API .NET + Angular + Agent de IA

## Introdução do projeto

Este repositório reúne uma solução de e-commerce com **API REST em ASP.NET Core** (`ECommerce.API`), **frontend em Angular** (`ecommerce-web`), persistência em **SQL Server** via **Entity Framework Core** e um serviço separado `**ECommerce.AgentAPI`**: agente de chat baseado em **Microsoft Semantic Kernel**, com suporte a **OpenAI** e opcionalmente **Google (Gemini)** via configuração, chamadas à API do e-commerce por **Refit** (JWT do mesmo utilizador autenticado), **rate limiting**, **health checks** e **OpenTelemetry**.

A API do e-commerce segue versionamento em URL (`/api/v1/...`), autenticação **JWT** com **refresh token** em cookie HTTP-only, e papéis **Admin** e **Customer**.

**Funcionalidades principais (conforme o código):**

- **Autenticação:** registo, login, refresh de token, revogação (logout).
- **Produtos:** listagem paginada com filtros (categoria, pesquisa), detalhe por ID; criação de produtos (Admin).
- **Carrinho:** consulta, inclusão/atualização/remoção de itens, limpeza (Customer).
- **Pedidos:** checkout a partir do carrinho, listagem e detalhe dos pedidos do cliente; listagem administrativa (Admin).
- **Utilizadores:** perfil do utilizador autenticado; listagem paginada (Admin).
- **Agente de compras (IA):** chat em `/chat` no Angular; o agente expõe tools para catálogo, carrinho e pedidos, com fluxo de **aprovação** para ações sensíveis.

Em ambiente **Development**, a API do e-commerce executa **seed** inicial (utilizadores e produtos de exemplo) e redireciona `GET /` para o **Swagger**.

---

## Estrutura do projeto

```
dotnet-ecommerce-ai-agent/
├── e-commerce/
│   ├── backend/
│   │   ├── ECommerce.sln
│   │   ├── ECommerce.API/              # Host web, controllers v1, middlewares, Swagger, Program.cs
│   │   ├── ECommerce.Application/      # Serviços, DTOs, validadores, interfaces
│   │   ├── ECommerce.Domain/           # Entidades e enums
│   │   ├── ECommerce.Infrastructure/   # EF Core (DbContext), repositórios, migrations
│   │   └── ECommerce.Tests/            # Testes (xUnit)
│   ├── agent-ai/
│   │   └── backend/
│   │       └── ECommerce.AgentAPI/     # Minimal API: chat, SK, tools, Refit, aprovação, observabilidade
│   │           ├── API/                # Program.cs, endpoints, filtros, modelos HTTP
│   │           ├── Application/        # Casos de uso, agente, registo de tools
│   │           ├── Domain/
│   │           └── Infrastructure/   # LLM, Refit, Redis/memória, aprovação, plugins SK
│   └── frontend/                       # Angular (ecommerce-web)
│       ├── src/
│       │   └── app/features/agent-chat/   # Widget de chat (/chat)
│       ├── angular.json
│       └── package.json
└── README.md
```

- **Clean Architecture** no e-commerce: `Domain` → `Application` → `Infrastructure`; `ECommerce.API` compõe a DI e expõe os endpoints.
- `**ECommerce.AgentAPI`** é um host independente (porta **5200** em desenvolvimento). Valida o **mesmo JWT** que o e-commerce e envia o token nas chamadas Refit à API versionada.

---

## Tecnologias e ferramentas (com versões)

### Plataforma e e-commerce (backend)


| Tecnologia                                              | Versão / observação                                                      |
| ------------------------------------------------------- | ------------------------------------------------------------------------ |
| **.NET / C#**                                           | `net8.0`                                                                 |
| **ASP.NET Core** (pacotes Microsoft no `ECommerce.API`) | 8.0.12 (JWT Bearer, EF Design)                                           |
| **Entity Framework Core** (SQL Server)                  | 8.0.12 (`ECommerce.Infrastructure`)                                      |
| **API versioning**                                      | `Asp.Versioning.Mvc` / `Asp.Versioning.Mvc.ApiExplorer` **8.1.1**        |
| **FluentValidation**                                    | `FluentValidation` **12.1.1** + `FluentValidation.AspNetCore` **11.3.1** |
| **AutoMapper**                                          | `AutoMapper.Extensions.Microsoft.DependencyInjection` **12.0.1**         |
| **OpenAPI / Swagger**                                   | `Swashbuckle.AspNetCore` **10.1.7**, `Microsoft.OpenApi` **2.4.1**       |
| **Serilog**                                             | `Serilog.AspNetCore` **10.0.0**, `Serilog.Sinks.File` **7.0.0**          |
| **Hash de palavra-passe**                               | `BCrypt.Net-Next` **4.1.0**                                              |
| **SQL Server**                                          | Provider `Microsoft.EntityFrameworkCore.SqlServer` **8.0.12**            |


### Agent de IA (`ECommerce.AgentAPI`)


| Tecnologia                                  | Versão / observação                                                                           |
| ------------------------------------------- | --------------------------------------------------------------------------------------------- |
| **.NET**                                    | `net8.0`                                                                                      |
| **Microsoft Semantic Kernel**               | **1.74.0**                                                                                    |
| **Connectors**                              | `Microsoft.SemanticKernel.Connectors.OpenAI` **1.74.0**; Google **1.74.0-alpha** (opcional)   |
| **Plugins SK**                              | `Microsoft.SemanticKernel.Plugins.Core` **1.74.0-preview**; `Plugins.Memory` **1.74.0-alpha** |
| **Refit**                                   | **10.1.6** (+ `Refit.HttpClientFactory`)                                                      |
| **Polly**                                   | **8.6.6**; `Microsoft.Extensions.Http.Polly` **10.0.6**                                       |
| **JWT Bearer** (Agent)                      | **8.0.11**                                                                                    |
| **Redis** (memória conversacional opcional) | `StackExchange.Redis` **2.8.16**                                                              |
| **System.Text.Json**                        | **10.0.6**                                                                                    |
| **OpenTelemetry**                           | **1.9.0** (OTLP, ASP.NET Core, HTTP, Runtime)                                                 |


### Frontend (Angular)


| Tecnologia                                          | Versão (package.json / lock típico)               |
| --------------------------------------------------- | ------------------------------------------------- |
| **Angular** (core, router, forms, animations, etc.) | **^17.3.0**–**^17.3.12** (CLI/build **^17.3.17**) |
| **Angular Material / CDK**                          | **^17.3.10**                                      |
| **TypeScript**                                      | **~5.4.2**                                        |
| **RxJS**                                            | **~7.8.0**                                        |
| **Zone.js**                                         | **~0.14.3**                                       |
| **Gestor de pacotes**                               | **npm** (`angular.json`)                          |


**Node.js:** não está fixado no repositório; para Angular 17 recomenda-se **Node 18.x ou 20.x LTS**.

### Testes (`ECommerce.Tests`)


| Pacote                 | Versão  |
| ---------------------- | ------- |
| xUnit                  | 2.5.3   |
| FluentAssertions       | 8.9.0   |
| Moq                    | 4.20.72 |
| Coverlet               | 6.0.0   |
| Microsoft.NET.Test.Sdk | 17.8.0  |


---

## Como executar o projeto

### Pré-requisitos gerais

- [.NET SDK 8.0](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) acessível pela connection string do e-commerce
- Para o frontend: [Node.js](https://nodejs.org/) LTS (18 ou 20) e **npm**
- Para o agente com OpenAI: chave em `LLM:OpenAI:ApiKey` (ou ficheiros de ambiente); opcionalmente chave Google se `LLM:Provider` for Google
- **Redis** no Agent apenas quando `Memory:Provider` = `redis`: servidor acessível em `Memory:Redis:ConnectionString` (ex.: `localhost:6379`). Em desenvolvimento típico: `docker run -d --name redis -p 6379:6379 redis:alpine` ou `docker start redis` (se já rodou `docker run ...`). Com `volatile`, não é necessário Redis.

---

### Backend — e-commerce (.NET)

1. Configure `**ConnectionStrings:DefaultConnection`** em `e-commerce/backend/ECommerce.API/appsettings.json` ou `appsettings.Development.json` (ou variáveis de ambiente).
2. **JWT** — secção `Jwt` em `appsettings`: `SecretKey`, `Issuer`, `Audience`, `AccessTokenExpirationMinutes`, `RefreshTokenExpirationDays`. Use chave forte; evite commitar segredos reais.
3. **Aplicar migrations:**
  ```bash
   cd e-commerce/backend
   dotnet ef database update --project ECommerce.Infrastructure/ECommerce.Infrastructure.csproj --startup-project ECommerce.API/ECommerce.API.csproj
  ```
4. **Executar a API:**
  ```bash
   cd e-commerce/backend/ECommerce.API
   dotnet run
  ```
   URLs típicas (`launchSettings.json`):
  - HTTP: `http://localhost:5149`
  - HTTPS: `https://localhost:7026`
   Em **Development**, o Swagger está em `/swagger` e `GET /` redireciona para ele.
5. **Seed (Development):** se as tabelas estiverem vazias, são criados utilizadores e produtos de exemplo:
  - Admin: `admin@ecommerce.com` / `Admin@123456`
  - Customer: `user@ecommerce.com` / `User@123456`
6. **Testes:**
  ```bash
   cd e-commerce/backend
   dotnet test
  ```

---

### Backend — Agent de IA (`ECommerce.AgentAPI`)

1. Configure `appsettings.Development.json` (repositório) ou `appsettings.json` / variáveis de ambiente em outros ambientes, em `e-commerce/agent-ai/backend/ECommerce.AgentAPI/`:
  - `**LLM`:** `Provider` (`OpenAI` ou `Google`), chaves e modelos em `OpenAI` / `Google`.
  - `**Jwt`:** `Issuer`, `Audience`, `SecretKey` **alinhados** ao `ECommerce.API`.
  - `**ECommerceApi:BaseUrl`:** base da API versionada (ex.: `http://localhost:5149/api/v1` ou HTTPS conforme o perfil do e-commerce).
  - `**Cors:AllowedOrigins`:** deve incluir a origem do Angular (ex.: `http://localhost:4200`).
  - `**Memory`** — histórico de conversa por `sessionId`:
    - `**Provider`:** `volatile` (omissão no código) — estado em memória no processo do Agent; útil sem infraestrutura extra.
    - `**Provider`:** `redis` — histórico serializado em Redis; obrigatório `**Memory:Redis:ConnectionString`**. O `ConnectionMultiplexer` usa `AbortOnConnectFail = false` para não falhar no arranque se o broker ainda não estiver disponível (reconexão em background). Operações de chat continuam a precisar de Redis acessível.
    - Opcionais com Redis: `Memory:Redis:KeyTtlSeconds` (TTL da chave), `Memory:Redis:KeyPrefix` (prefixo das chaves).
2. **Ordem:** iniciar **SQL Server** e **ECommerce.API** antes do Agent (o Refit precisa de alcançar o e-commerce). Se `Memory:Provider` = `redis`, ter **Redis** a correr antes de depender de `/health/ready` ou de persistência de conversa.
3. **Executar:**
  ```bash
   cd e-commerce/agent-ai/backend/ECommerce.AgentAPI
   dotnet run
  ```
   URL por defeito (`launchSettings.json`): `**http://localhost:5200**`.

---

### Frontend (Angular)

```bash
cd e-commerce/frontend
npm install
npm start
```

O servidor de desenvolvimento usa normalmente a porta **4200**.

**Ambientes** (`src/environments/`):


| Ficheiro                    | `apiUrl`                        | `agentApiUrl`           |
| --------------------------- | ------------------------------- | ----------------------- |
| `environment.ts`            | `http://localhost:5149/api/v1`  | `http://localhost:5200` |
| `environment.production.ts` | `https://localhost:7026/api/v1` | `http://localhost:5200` |


Ajuste portas e protocolos se alterar os perfis .NET. O **CORS** do e-commerce em Development e a lista `**Cors:AllowedOrigins`** do Agent devem incluir a origem do Angular. O widget em `**/chat**` usa o token JWT (interceptor) para o URL configurado em `agentApiUrl`.

---

## Endpoints da API e-commerce

Versão da API: **1.0**. Prefixo: `**/api/v1`**.

**Formato de resposta:** o e-commerce usa o envelope `ApiResponse<T>` (`success`, `data`, `message`, `errors`).

### Raiz (Development)


| Método | URL | Descrição                    |
| ------ | --- | ---------------------------- |
| GET    | `/` | Redireciona para `/swagger`. |


### Autenticação — `/api/v1/auth`


| Método | URL                     | Auth                  | Descrição                                                                                              |
| ------ | ----------------------- | --------------------- | ------------------------------------------------------------------------------------------------------ |
| POST   | `/api/v1/auth/register` | Anónimo               | Registo. Corpo: `RegisterDto` (`name`, `email`, `password`, `confirmPassword`). **201** com `UserDto`. |
| POST   | `/api/v1/auth/login`    | Anónimo               | Login. Corpo: `LoginDto` (`email`, `password`). **200** com `AuthResponseDto`; cookie `refreshToken`.  |
| POST   | `/api/v1/auth/refresh`  | Cookie `refreshToken` | Renova access token. **200** com `AuthResponseDto`.                                                    |
| POST   | `/api/v1/auth/revoke`   | Bearer                | Revoga refresh tokens e remove cookie. **204**.                                                        |


### Produtos


| Método | URL                      | Auth             | Descrição                                                                                |
| ------ | ------------------------ | ---------------- | ---------------------------------------------------------------------------------------- |
| GET    | `/api/v1/products`       | Anónimo          | Lista paginada. Query: `page`, `pageSize`, `category`, `search`.                         |
| GET    | `/api/v1/products/{id}`  | Anónimo          | Detalhe por GUID. **404** se inexistente.                                                |
| POST   | `/api/v1/admin/products` | Bearer **Admin** | Cria produto. Corpo: `CreateProductDto`. **201** com `Location` `/api/v1/products/{id}`. |


### Carrinho — `/api/v1/cart` (Bearer **Customer**)


| Método | URL                              | Descrição                                                         |
| ------ | -------------------------------- | ----------------------------------------------------------------- |
| GET    | `/api/v1/cart`                   | Carrinho atual (`CartDto`).                                       |
| POST   | `/api/v1/cart/items`             | Adiciona item. Corpo: `AddCartItemDto` (`productId`, `quantity`). |
| PUT    | `/api/v1/cart/items/{productId}` | Atualiza quantidade. Corpo: `UpdateCartItemDto` (`quantity`).     |
| DELETE | `/api/v1/cart/items/{productId}` | Remove item.                                                      |
| DELETE | `/api/v1/cart`                   | Limpa o carrinho. **204**.                                        |


### Pedidos


| Método | URL                       | Auth                | Descrição                                                            |
| ------ | ------------------------- | ------------------- | -------------------------------------------------------------------- |
| POST   | `/api/v1/orders/checkout` | Bearer **Customer** | Checkout. **201** com `Location` `/api/v1/orders/{id}`.              |
| GET    | `/api/v1/orders`          | Bearer **Customer** | Pedidos do cliente. Query: `page`, `pageSize`.                       |
| GET    | `/api/v1/orders/{id}`     | Bearer **Customer** | Detalhe. **404** se não for do cliente.                              |
| GET    | `/api/v1/admin/orders`    | Bearer **Admin**    | Lista administrativa. Query: `page`, `pageSize`, `userId`, `status`. |


### Utilizadores


| Método | URL                   | Auth             | Descrição                                  |
| ------ | --------------------- | ---------------- | ------------------------------------------ |
| GET    | `/api/v1/users/me`    | Bearer           | Perfil (`UserDto`).                        |
| GET    | `/api/v1/admin/users` | Bearer **Admin** | Lista paginada. Query: `page`, `pageSize`. |


---

## Endpoint do AgentAPI

Base típica em desenvolvimento: `**http://localhost:5200`**.


| Método | URL                             | Auth                                 | Descrição                                                                                                                                                                         |
| ------ | ------------------------------- | ------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| GET    | `/`                             | —                                    | Resposta simples OK (health lógico mínimo).                                                                                                                                       |
| GET    | `/health`                       | —                                    | Health check agregado.                                                                                                                                                            |
| GET    | `/health/live`                  | —                                    | Liveness (tags `live`).                                                                                                                                                           |
| GET    | `/health/ready`                 | —                                    | Readiness (tags `ready`). Com `Memory:Provider=redis`, inclui verificação de conexão ao Redis.                                                                                    |
| POST   | `/api/agent/chat`               | **Bearer** (mesmo JWT do e-commerce) | Chat do agente. **Rate limiting** aplicado. Timeout configurável (`Agent:Hosting:ChatRequestTimeoutSeconds`, padrão 120 s).                                                       |
| POST   | `/api/agent/chat/session/clear` | **Bearer**                           | **204** — apaga histórico da sessão no armazenamento de memória e limpa aprovações pendentes para o `sessionId`. Corpo JSON: `sessionId` (GUID). **Rate limiting** igual ao chat. |


**Corpo de `POST /api/agent/chat` (`ChatRequest`):**


| Campo           | Tipo   | Obrigatório | Descrição                                                                     |
| --------------- | ------ | ----------- | ----------------------------------------------------------------------------- |
| `sessionId`     | GUID   | Sim         | Identificador da sessão de chat (o frontend mantém um por sessão do browser). |
| `message`       | string | Sim         | Mensagem do utilizador.                                                       |
| `clientVersion` | string | Não         | Versão do cliente.                                                            |
| `locale`        | string | Não         | Locale.                                                                       |
| `channel`       | string | Não         | Canal.                                                                        |
| `metadata`      | JSON   | Não         | Metadados arbitrários.                                                        |
| `correlationId` | string | Não         | ID de correlação (pode ser preenchido pelo servidor na resposta).             |


**Resposta (`ChatResponse`):** `introMessage`, `outroMessage`, `tool`, `data`, `requiresApproval`, `llmProvider`, `correlationId`, `contractVersion`. O fluxo de aprovação de tools continua na **mesma rota**: o utilizador responde na conversa (confirmar/cancelar conforme classificação no servidor), sem endpoint separado de aprovação.

Pode desativar o endpoint de chat com `Agent:Hosting:ChatEndpointEnabled: false` em configuração (útil em manutenção).