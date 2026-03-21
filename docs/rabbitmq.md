# RabbitMQ (MassTransit) — inbound e outbound

## Filas quorum

Com `RabbitMq:UseQuorumQueues` = `true` (padrão):

- **Inbound** — o `ReceiveEndpoint` declara `InboundQueueName` como **fila quorum**.
- **Outbound** — a API **publica** na exchange `OutboundExchangeName` (padrão `interactive-outbound`, tipo **fanout**). O MassTransit declara essa exchange, a fila `OutboundQueueName` como **quorum** e o binding exchange → fila.

`Send` direto para `queue:nome` não repassa bem os argumentos de fila quorum no MassTransit; por isso o outbound usa **`IPublishEndpoint.Publish`** + topologia explícita.

## Fluxos

### Receber mensagens (inbound)

A integração externa (n8n, bridge, etc.) **publica** na fila `InboundQueueName` (default `interactive-inbound-events`) uma mensagem **`InboundIntegrationEvent`**: corpo JSON **simples** (sem envelope MassTransit), por exemplo `{ "provider": "...", "eventType": "message", "identifications": { ... }, "payload": { ... } }`. O consumer MassTransit mapeia para `NormalizedInboundEvent` e executa **`ProcessInboundEventCommand`** com **`ReliableMessaging = true`** (persistência + SignalR). O endpoint inbound usa **raw JSON deserializer** para ser compatível com publicadores que não enviam o envelope do MassTransit.

#### Confiabilidade (ACK, retry, redelivery)

- **Sucesso** (`InboundProcessingOutcome.Persisted` ou `DuplicateIgnored`): o consumer conclui sem exceção → **ACK**.
- **Erro permanente** (`PermanentRejected`: payload inválido, tipo não suportado, telefone ausente, etc.): **sem exceção** → **ACK** (não retentar infinitamente). Opcionalmente `RabbitMq:ForwardPermanentRejectionsToUnprocessedQueue` envia cópia para `UnprocessedQueueName` (default `chat.unprocessed`).
- **Erro transitório** (`integration_not_found`, `integration_missing_in_tenant`) ou falha técnica (EF/SQL): **`InboundTransientException`** ou exceção propagada → **NACK** / retentativas MassTransit.

**Retry imediato** (no consumer): intervalos `200 ms`, `1 s`, `5 s`.

**Redelivery atrasada** (segundo nível): `30 s`, `2 min`, `10 min` após esgotar os retries imediatos.

> **Broker:** `UseDelayedRedelivery` no RabbitMQ requer normalmente o **plugin [rabbitmq_delayed_message_exchange](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange)** (ou broker equivalente). Sem o plugin, a API pode falhar ao declarar a topologia de delay; nesse caso instale o plugin ou avalie versões futuras do MassTransit com redelivery baseada em fila (TTL/DLX).

Mensagens que esgotam retries + redeliveries vão para a fila de **erro** MassTransit (`*_error` junto ao endpoint), conforme documentação do MassTransit.

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
| `chat.unprocessed` (configurável) | Fila (opcional) | Cópias de eventos permanentemente rejeitados (se forwarding ativo). |

## Configuração

- `RabbitMq__VirtualHost` — deve coincidir com o vhost do broker (ex.: stack de dev com `RABBITMQ_DEFAULT_VHOST=default` → `VirtualHost`: `default`).
- `RabbitMq__InboundQueueName`, `RabbitMq__OutboundQueueName`, `RabbitMq__OutboundExchangeName`
- `RabbitMq__UseQuorumQueues` — `false` volta ao comportamento sem `SetQuorumQueue` / quorum no binding outbound (não recomendado se o broker exige quorum).
- `RabbitMq__UnprocessedQueueName` — nome da fila para cópias de rejeições permanentes (default `chat.unprocessed`).
- `RabbitMq__ForwardPermanentRejectionsToUnprocessedQueue` — `true` para enviar `InboundUnprocessedDispatch` a essa fila.
- `Integration__MessageSender__UseHttpFallback` — outbound por HTTP em vez do broker.

## Formato das mensagens (MassTransit + System.Text.Json)

- **Inbound** (`InboundIntegrationEvent`): propriedades na raiz — `provider`, `eventType`, `identifications`, `payload`.
- **Outbound** (`OutboundMessageDispatch`): envelope MassTransit com propriedade `message` contendo `OutboundMessageContract` (`provider`, `eventType`, `tenantId`, `channelId`, `auth`, `contact`, `payload`, `metadata`). Em `auth` (WhatsApp): `webhookVerifyToken`, `phoneNumberId`, `businessAccountId` (sem `type` nem `accessToken`). O corpo enviável (`id`, `type`, `content`) está em `payload`; para mensagem de texto, `content` usa `{ "body": "..." }`.
- **Unprocessed** (`InboundUnprocessedDispatch`, opcional): `reasonCode`, `provider`, `externalIdentifier`, `messageId`, `rawEventJson`, `occurredAt`.

## Outcomes (`InboundProcessingOutcome`)

| Outcome | Significado | ACK na fila? |
|---------|-------------|--------------|
| `Persisted` | Mensagem nova gravada | Sim |
| `DuplicateIgnored` | Idempotência (mesmo `ExternalMessageId`) | Sim |
| `PermanentRejected` | Dados inválidos / não suportado | Sim |
| `TransientRetry` | Só com `ReliableMessaging: false` (ex.: futuro HTTP) | N/A |

Com `ReliableMessaging: true`, cenários transitórios **lançam** exceção em vez de devolver `TransientRetry`.

## Stack Docker de referência

Ver [`deploy/rabbitmq-stack.dev.yml`](../deploy/rabbitmq-stack.dev.yml).

## TLS

Se o broker usar AMQPS, estender `MassTransitServiceCollectionExtensions` (`UsingRabbitMq` → SSL no `Host`).
