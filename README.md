# FCG Payments API

Microservico de pagamentos do **FIAP Cloud Games (FCG)** para a **Fase 2 do Tech Challenge**.

Este servico recebe `OrderPlacedEvent`, processa pagamento simulado com regra deterministica e publica `PaymentProcessedEvent` para os servicos de Catalogo e Notificacoes.

---

## Visao Geral

Fluxo implementado neste corte:

1. `CatalogAPI` envia `POST /api/payments/order-placed` com `userId`, `jogoId` e `preco`.
2. `PaymentsAPI` processa o pagamento:
   - `preco <= 200` => **Aprovado (2)**
   - `preco > 200` => **Reprovado (3)**
3. `PaymentsAPI` gera `payId` e publica `PaymentProcessedEvent` via HTTP para:
   - `CatalogAPI`: `POST /api/payment-events/processed`
   - `NotificationsAPI`: `POST /api/notifications/payment-processed`
4. Se qualquer downstream falhar, a requisicao do `PaymentsAPI` falha (nao mascara erro).

---

## Estrutura da Solucao

- `Payments.Api`: controllers, middlewares, configuracao, DI, Swagger, Serilog.
- `Payments.Application`: contratos de eventos, interfaces e servicos de negocio.
- `Payments.Domain`: enums de dominio (`PaymentStatus`).
- `Payments.Infra`: dispatcher HTTP para publicar `PaymentProcessedEvent`.
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

Recebe ordem de compra e dispara o fluxo de pagamento.

Resposta:
- `204 No Content` em caso de sucesso.
- `400 Bad Request` para payload invalido.
- `502 Bad Gateway` quando falha envio para CatalogAPI/NotificationsAPI.

---

## Configuracao

Arquivo: `Payments.Api/appsettings.json`

```json
{
  "Downstream": {
    "CatalogBaseUrl": "http://localhost:5001",
    "NotificationsBaseUrl": "http://localhost:5003"
  },
  "PaymentRules": {
    "MaxPrecoAprovado": 200
  }
}
```

Variaveis equivalentes:
- `Downstream__CatalogBaseUrl`
- `Downstream__NotificationsBaseUrl`
- `PaymentRules__MaxPrecoAprovado`

---

## Como Executar

### Local

```bash
dotnet restore
dotnet build Payments.sln
dotnet run --project Payments.Api
```

Swagger:
- `http://localhost:5254/swagger` (ou porta definida no profile local)

### Docker

```bash
docker build -f Payments.Api/Dockerfile -t fcg-payments-api .
docker run --rm -p 5002:8080 \
  -e Downstream__CatalogBaseUrl=http://host.docker.internal:5001 \
  -e Downstream__NotificationsBaseUrl=http://host.docker.internal:5003 \
  -e PaymentRules__MaxPrecoAprovado=200 \
  fcg-payments-api
```

---

## Exemplo de Requisicao

```bash
curl -X POST http://localhost:5254/api/payments/order-placed \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "jogoId": "1fa85f64-5717-4562-b3fc-2c963f66afa6",
    "preco": 199.90
  }'
```

---

## Testes

Executar todos os testes:

```bash
dotnet test Payments.sln
```

Cobertura implementada:
- Unitarios do processador de pagamento (limite, aprovacao/reprovacao, `payId`, status valido).
- Integracao/API para:
  - payload valido (`204`),
  - payload invalido (`400`),
  - falha de downstream (`502`).
- Teste do dispatcher HTTP validando envio para os dois destinos e payload publicado.
