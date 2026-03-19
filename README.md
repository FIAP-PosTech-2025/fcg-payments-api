# FCG Payments API

Microservico de pagamentos do **FIAP Cloud Games (FCG)** para a **Fase 2 do Tech Challenge**.

Este servico consome `OrderPlacedEvent` do RabbitMQ, processa pagamento simulado com regra deterministica e publica `PaymentProcessedEvent` na fila consumida pelos outros microsservicos.

---

## Visao Geral

Fluxo implementado:

1. `CatalogAPI` publica `OrderPlacedEvent` na fila `OrderPlaced`.
2. `PaymentsAPI` consome a fila `OrderPlaced`.
3. `PaymentsAPI` processa o pagamento:
   - `preco <= 200` => **Aprovado (2)**
   - `preco > 200` => **Reprovado (3)**
4. `PaymentsAPI` gera `payId` e publica `PaymentProcessedEvent` na fila `PaymentProcessed`.
5. `CatalogAPI` e `NotificationsAPI` podem consumir `PaymentProcessedEvent` conforme a implementacao deles.

---

## Estrutura da Solucao

- `Payments.Api`: controllers, middlewares, configuracao, DI, Swagger, Serilog.
- `Payments.Application`: contratos de eventos, interfaces e servicos de negocio.
- `Payments.Domain`: enums de dominio (`PaymentStatus`).
- `Payments.Infra`: publisher/consumer RabbitMQ.
- `Payments.Tests`: testes unitarios e de integracao/API.

---

## Contratos de Evento

### OrderPlacedEvent (entrada)

```json
{
  "userId": "guid",
  "jogoId": "guid",
  "preco": 199.90
}
```

### PaymentProcessedEvent (saida)

```json
{
  "userId": "guid",
  "jogoId": "guid",
  "payId": "guid",
  "status": 2
}
```

Status:
- `2`: Aprovado
- `3`: Reprovado

---

## Endpoint Publico

### POST `/api/payments/order-placed`

Endpoint auxiliar para teste manual do fluxo sem depender do `CatalogAPI`.

Resposta:
- `204 No Content` em caso de sucesso.
- `400 Bad Request` para payload invalido.
- `502 Bad Gateway` quando falha publicacao do evento no RabbitMQ.

---

## Configuracao

Arquivo: `Payments.Api/appsettings.json`

```json
{
  "PaymentRules": {
    "MaxPrecoAprovado": 200
  },
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "OrderPlacedQueue": "OrderPlaced",
    "PaymentProcessedQueue": "PaymentProcessed"
  }
}
```

Variaveis equivalentes:
- `PaymentRules__MaxPrecoAprovado`
- `RabbitMq__HostName`
- `RabbitMq__Port`
- `RabbitMq__UserName`
- `RabbitMq__Password`
- `RabbitMq__VirtualHost`
- `RabbitMq__OrderPlacedQueue`
- `RabbitMq__PaymentProcessedQueue`

---

## Como Executar

### Local

```bash
dotnet restore
dotnet build Payments.sln
dotnet run --project Payments.Api
```

Swagger:
- `http://localhost:5004/swagger`

### Docker

```bash
docker compose up --build
```

O compose sobe o `PaymentsAPI` na porta `5004` e aponta para RabbitMQ em `host.docker.internal:5672`.

### Kubernetes

```bash
kubectl apply -k k8s/
```

Os manifests incluem:
- `ConfigMap`
- `Secret`
- `Deployment`
- `Service`

---

## Exemplo de Requisicao Manual

```bash
curl -X POST http://localhost:5004/api/payments/order-placed \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "jogoId": "1fa85f64-5717-4562-b3fc-2c963f66afa6",
    "preco": 199.90
  }'
```

## Testes

Executar todos os testes:

```bash
dotnet test Payments.sln
```

Cobertura implementada:
- Unitarios do processador de pagamento (limite, aprovacao/reprovacao, `payId`, status valido).
- Unitarios do fluxo de pagamento validando publicacao do evento processado.
- Integracao/API para:
  - payload valido (`204`),
  - payload invalido (`400`),
  - falha na publicacao (`502`).
