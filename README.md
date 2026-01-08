# FIAP Cloud Games - Microsserviço de Usuários

Microsserviço responsável pelo gerenciamento de usuários e autenticação da plataforma FIAP Cloud Games.

## 🚀 Funcionalidades

- **Cadastro de Usuários**: Criação de novos usuários com validação de senha forte
- **Autenticação JWT**: Login seguro com geração de tokens JWT
- **Gerenciamento de Perfis**: Atualização de dados de usuário
- **Event Sourcing**: Registro de todos os eventos relacionados a usuários
- **Observabilidade**: Logs estruturados com Serilog e rastreamento distribuído com OpenTelemetry

## 🏗️ Arquitetura

Este microsserviço segue os princípios de:

- **Domain-Driven Design (DDD)**
- **Clean Architecture**
- **Event Sourcing** para auditoria completa
- **Observabilidade** com traces distribuídos

## 📋 Endpoints

### Públicos (sem autenticação)

- `POST /api/users` - Criar novo usuário
- `POST /api/auth/login` - Realizar login
- `GET /api/health` - Health check do serviço

### Protegidos (requer autenticação)

- `GET /api/users` - Listar todos os usuários
- `GET /api/users/{id}` - Obter usuário por ID
- `PUT /api/users/{id}` - Atualizar usuário
- `GET /api/events/{aggregateId}` - Obter eventos do usuário

## 🔧 Tecnologias Utilizadas

- **.NET 8.0**
- **Entity Framework Core** (In-Memory Database)
- **JWT Bearer Authentication**
- **FluentValidation** para validação de entrada
- **Serilog** para logging estruturado
- **OpenTelemetry** para observabilidade
- **Swagger/OpenAPI** para documentação

## 🏃 Como Executar

### Pré-requisitos

- .NET 8.0 SDK

### Executar localmente

```bash
cd src
dotnet restore
dotnet run
```

A API estará disponível em:
- HTTP: http://localhost:5001
- HTTPS: https://localhost:7001
- Swagger: http://localhost:5001/swagger

### Executar com Docker

```bash
docker build -t fiap-cloudgames-usuario-api .
docker run -p 5001:80 fiap-cloudgames-usuario-api
```

## 🔐 Autenticação

O microsserviço utiliza JWT Bearer tokens. Para acessar endpoints protegidos:

1. Faça login através do endpoint `/api/auth/login`
2. Utilize o token retornado no header `Authorization: Bearer {token}`

### Usuário Admin Padrão

Para desenvolvimento, um usuário admin é criado automaticamente:

- **Email**: admin@fiap.com.br
- **Senha**: Admin@123

## 📊 Event Sourcing

Todos os eventos relacionados a usuários são registrados:

- `UserCreatedEvent` - Quando um usuário é criado
- `UserUpdatedEvent` - Quando um usuário é atualizado
- `UserLoggedInEvent` - Quando um usuário faz login

Os eventos podem ser consultados através do endpoint `/api/events/{aggregateId}`.

## 🔍 Observabilidade

### Logs

Logs estruturados são gerados com Serilog, incluindo:
- Informações de requisição
- Eventos de negócio
- Erros e exceções

### Traces

OpenTelemetry é utilizado para rastreamento distribuído, permitindo:
- Rastreamento de requisições entre microsserviços
- Análise de performance
- Identificação de gargalos

## 🌐 Integração com outros Microsserviços

Este microsserviço se comunica com:

- **FIAP.CloudGames.Jogo.API** (porta 5002)
- **FIAP.CloudGames.Pagamento.API** (porta 5003)

As URLs são configuráveis através do `appsettings.json`:

```json
"ServiceUrls": {
  "JogoAPI": "http://localhost:5002",
  "PagamentoAPI": "http://localhost:5003"
}
```

## 📝 Licença

Este projeto é parte do Tech Challenge da FIAP - Pós-Tech.
