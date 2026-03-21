# RabbitMQ (MassTransit) — inbound e outbound

## Filas quorum

Com `RabbitMq:UseQuorumQueues` = `true` (padrão):

- **Inbound** — o `ReceiveEndpoint` declara `InboundQueueName` como **fila quorum**.
- **Outbound** — a API **publica** na exchange `OutboundExchangeName` (padrão `interactive-outbound`, tipo **fanout**). O MassTransit declara essa exchange, a fila `OutboundQueueName` como **quorum** e o binding exchange → fila.

`Send` direto para `queue:nome` não repassa bem os argumentos de fila quorum no MassTransit; por isso o outbound usa **`IPublishEndpoint.Publish`** + topologia explícita.

## Fluxos

### Receber mensagens (inbound)

1. **HTTP** — `POST /api/webhooks/messages` com o payload normalizado. O endpoint **processa de forma síncrona** (`ProcessWebhookEventCommand`), persiste e notifica o frontend via SignalR como antes.

2. **Fila** — A integração externa **publica** na fila `InboundQueueName` (default `interactive-inbound-events`) uma mensagem **`InboundIntegrationEvent`**: corpo JSON **simples** (sem envelope MassTransit), por exemplo `{ "provider": "...", "eventType": "message", "identifications": { ... }, "payload": { ... } }`. O endpoint inbound está configurado com **raw JSON deserializer** para ser compatível com n8n e outros publicadores que não enviam o formato envelope do MassTransit.

### Enviar mensagens (outbound)

1. O CRM chama o envio; a API **persiste** a mensagem no banco.
2. **Publica** `OutboundMessageDispatch` na exchange `OutboundExchangeName` (**ou** HTTP se `Integration:MessageSender:UseHttpFallback` for `true`).
3. Workers externos consomem pela **fila** `OutboundQueueName` (ligada à exchange).

## Recursos no broker

| Nome | Tipo | Função |
|------|------|--------|
| `interactive-inbound-events` (configurável) | Fila quorum | Consumida pela API (inbound). |
| `interactive-outbound` (configurável) | Exchange fanout | Onde a API publica outbound. |
| `interactive-outbound-send` (configurável) | Fila quorum | Consumida por workers; binding a partir da exchange outbound. |

## Configuração

- `RabbitMq__VirtualHost` — deve coincidir com o vhost do broker (ex.: stack de dev com `RABBITMQ_DEFAULT_VHOST=default` → `VirtualHost`: `default`).
- `RabbitMq__InboundQueueName`, `RabbitMq__OutboundQueueName`, `RabbitMq__OutboundExchangeName`
- `RabbitMq__UseQuorumQueues` — `false` volta ao comportamento sem `SetQuorumQueue` / quorum no binding outbound (não recomendado se o broker exige quorum).
- `Integration__MessageSender__UseHttpFallback` — outbound por HTTP em vez do broker.

## Formato das mensagens (MassTransit + System.Text.Json)

- **Inbound** (`InboundIntegrationEvent`): propriedades na raiz — `provider`, `eventType`, `identifications`, `payload`.
- **Outbound** (`OutboundMessageDispatch`): propriedade `message` (`OutboundMessageContract`).

## Stack Docker de referência

Ver [`deploy/rabbitmq-stack.dev.yml`](../deploy/rabbitmq-stack.dev.yml).

## TLS

Se o broker usar AMQPS, estender `MassTransitServiceCollectionExtensions` (`UsingRabbitMq` → SSL no `Host`).
