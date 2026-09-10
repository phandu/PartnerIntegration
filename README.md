# Partner Integration BFF

A lightweight .NET 8 Backend-for-Frontend (BFF) service for receiving partner transactions, validating incoming requests, verifying partner eligibility through an external service, and publishing accepted transactions to RabbitMQ.

The solution demonstrates API design, validation, resilient HTTP communication, asynchronous messaging, exception handling, automated testing, and containerised local development.

---

## Technology Stack

- .NET 8 / ASP.NET Core Web API
- C#
- FluentValidation
- HttpClientFactory
- Microsoft.Extensions.Http.Resilience
- Polly resilience strategies
- RabbitMQ
- Docker / Docker Compose
- xUnit
- Moq
- Coverlet
- ReportGenerator
- Swagger / OpenAPI

---

## Solution Structure

```text
PartnerIntegration
│
├── src
│   └── PartnerIntegration.Api
│       ├── Controllers
│       ├── Exceptions
│       ├── Messaging
│       ├── Models
│       ├── Services
│       ├── Validation
│       ├── Program.cs
│       └── appsettings.json
│
├── tests
│   └── PartnerIntegration.Tests
│       ├── Controllers
│       ├── Helpers
│       ├── Messaging
│       ├── Resilience
│       ├── Services
│       └── Validation
│
├── docker-compose.yml
└── README.md
```

---

## Architecture

The service acts as a small integration BFF between a client and external/downstream systems.

```text
Client
  |
  | POST /api/v1/partner/transactions
  v
Partner Integration API
  |
  +--> Request Validation
  |
  +--> Partner Verification Service
  |        |
  |        +--> Retry / Timeout
  |        |
  |        +--> Mock Partner API
  |
  +--> RabbitMQ Publisher
           |
           v
    partner-transactions
           queue
```

The API does not process the transaction synchronously after it has been accepted.

Once validation and partner verification succeed, the transaction is published to RabbitMQ and the API returns `202 Accepted`.

This keeps the HTTP request path lightweight and decouples transaction ingestion from downstream processing.

---

## Request Flow

When a transaction is submitted:

1. The API receives a `POST` request.
2. FluentValidation validates the request.
3. The partner verification service verifies the supplied `partnerId`.
4. The outbound HTTP request is protected by retry and timeout resilience policies.
5. If the partner is valid, the transaction is published to RabbitMQ.
6. The API returns `202 Accepted`.

Example request:

```json
{
  "partnerId": "P-1001",
  "transactionReference": "TXN-DOCKER-001",
  "amount": 250.00,
  "currency": "USD",
  "timestamp": "2026-09-10T00:00:00Z"
}
```

---

## API Endpoint

### Submit Partner Transaction

```http
POST /api/v1/partner/transactions
Content-Type: application/json
```

Example successful response:

```json
{
  "message": "Transaction accepted.",
  "transactionReference": "TXN-DOCKER-001"
}
```

Expected HTTP status:

```text
202 Accepted
```

---

## Validation

Incoming transactions are validated before any partner verification or message publishing takes place.

Validation is implemented using FluentValidation.

The validation layer prevents invalid requests from reaching external dependencies such as the partner verification service or RabbitMQ.

Invalid requests return:

```text
400 Bad Request
```

---

## Partner Verification

Before a transaction is accepted, the API verifies the partner through an HTTP service.

The implementation uses a typed `HttpClient`:

```text
IPartnerVerificationService
        |
        v
PartnerVerificationService
```

For local development, the project includes a mock partner verification endpoint.

Example:

```http
GET /api/mock/partners/P-1001/verify
```

This allows resilience behaviour and the complete transaction flow to be demonstrated without depending on a real external partner system.

---

## Resilience Strategy

Calls to the partner verification service use resilience policies to handle transient failures.

The configured strategy includes:

- Retry
- Exponential backoff
- Request timeout

Transient HTTP failures can therefore be retried automatically instead of immediately failing the transaction request.

A timeout prevents the API from waiting indefinitely for an unavailable or slow external service.

The resilience logic is applied to the outbound partner verification HTTP client rather than being implemented directly inside the controller.

This keeps the controller focused on request orchestration and keeps infrastructure concerns separated.

---

## RabbitMQ Messaging

Accepted transactions are published asynchronously to RabbitMQ.

Messaging is abstracted behind:

```text
IMessagePublisher
        |
        v
RabbitMqMessagePublisher
```

The application publishes accepted transactions to:

```text
partner-transactions
```

Using an interface allows the controller to remain independent of RabbitMQ-specific implementation details and makes the messaging behaviour easier to unit test.

The assessment scope only requires publishing the transaction to the message broker. A downstream consumer is therefore intentionally not included.

In a production architecture, one or more worker services could consume messages from this queue and perform downstream transaction processing.

---

## Exception Handling

Application exceptions are handled centrally through a global exception handler.

This avoids duplicating exception handling logic across controllers and provides consistent HTTP responses.

The service distinguishes between request validation errors and failures involving external dependencies.

Typical responses include:

```text
400 Bad Request
503 Service Unavailable
504 Gateway Timeout
500 Internal Server Error
```

Detailed internal exception information should be logged but should not be exposed directly to API consumers.

---

## Running Locally

### Prerequisites

Install:

- .NET 8 SDK
- Docker Desktop

Clone the repository and navigate to the project root:

```powershell
cd D:\PhanDu\PartnerIntegration
```

Build the solution:

```powershell
dotnet build
```

Run the API directly:

```powershell
cd src\PartnerIntegration.Api
dotnet run
```

The local development URL is displayed by ASP.NET Core when the application starts.

---

## Running with Docker Compose

The recommended way to run the complete local environment is Docker Compose.

From the solution root:

```powershell
cd D:\PhanDu\PartnerIntegration

docker compose up --build -d
```

Verify the containers:

```powershell
docker ps
```

The environment contains:

```text
partner-api
partner-rabbitmq
```

The API is available at:

```text
http://localhost:8080
```

RabbitMQ Management UI is available at:

```text
http://localhost:15672
```

Default local RabbitMQ credentials:

```text
Username: guest
Password: guest
```

RabbitMQ AMQP port:

```text
5672
```

To stop the environment:

```powershell
docker compose down
```

---

## Testing the API

With Docker Compose running, the API can be tested from PowerShell.

```powershell
$body = @{
    partnerId = "P-1001"
    transactionReference = "TXN-DOCKER-001"
    amount = 250.00
    currency = "USD"
    timestamp = "2026-09-10T00:00:00Z"
}

Invoke-RestMethod `
    -Uri "http://localhost:8080/api/v1/partner/transactions" `
    -Method Post `
    -ContentType "application/json" `
    -Body ($body | ConvertTo-Json)
```

Expected result:

```text
message                transactionReference
-------                --------------------
Transaction accepted.  TXN-DOCKER-001
```

After a successful request, the published message can also be inspected through the RabbitMQ Management UI under:

```text
Queues and Streams
    -> partner-transactions
```

---

## Automated Tests

The solution contains automated tests covering the main application behaviours.

Run all tests from the solution root:

```powershell
dotnet test
```

The test suite covers areas including:

- Request validation
- Partner verification
- HTTP retry behaviour
- Controller behaviour
- Exception handling
- RabbitMQ message publishing

External dependencies are mocked where appropriate so business behaviour can be tested independently.

The RabbitMQ publisher also has integration-level coverage that can verify publishing against a running local RabbitMQ instance.

---

## Code Coverage

Coverage can be collected using Coverlet.

Example:

```powershell
dotnet test `
  --collect:"XPlat Code Coverage"
```

Generate an HTML report using ReportGenerator:

```powershell
reportgenerator `
  "-reports:tests\PartnerIntegration.Tests\TestResults\**\coverage.cobertura.xml" `
  "-targetdir:coverage-report" `
  "-reporttypes:Html"
```

Open the report:

```powershell
start coverage-report\index.html
```

At the time of submission, the test suite provides strong coverage of the core application components, including controllers, validation, exception handling, partner verification, and RabbitMQ publishing.

Bootstrap/configuration code such as `Program.cs` is not the primary focus of unit testing; tests are concentrated on application behaviour and failure scenarios.

---

## Design Decisions

### Dependency Injection

External dependencies are accessed through interfaces and registered through ASP.NET Core dependency injection.

Examples include:

```text
IPartnerVerificationService
IMessagePublisher
```

This improves separation of concerns and testability.

### Typed HttpClient

Partner verification uses `HttpClientFactory` rather than manually creating `HttpClient` instances.

This provides centralised configuration and allows resilience policies to be associated with the external integration.

### Asynchronous Messaging

RabbitMQ is used to decouple transaction ingestion from downstream transaction processing.

The API only needs to verify and accept the transaction before publishing it to the broker.

### Global Exception Handling

Centralised exception handling keeps controllers small and provides consistent error responses.

### Validation Before External Calls

Requests are validated before performing partner verification or message publishing.

This avoids unnecessary calls to external systems for invalid input.

---

## Security Considerations

The sample focuses on the integration flow rather than implementing a complete authentication platform.

For production use, the API should be protected using an authentication and authorization mechanism appropriate to the partner integration.

Possible approaches include:

- OAuth 2.0 / OpenID Connect
- JWT bearer authentication
- Client credentials for machine-to-machine integrations
- API keys where appropriate
- mTLS for higher-trust partner integrations

Additional production controls should include:

- TLS/HTTPS
- Rate limiting
- Secret management
- Input validation
- Request correlation IDs
- Structured logging
- Audit logging
- Authorization policies

Credentials and secrets should not be committed to source control.

---

## Production Considerations

For a production implementation, I would consider adding:

**Idempotency**

`transactionReference` could be used as an idempotency key to prevent duplicate transaction processing when clients retry requests.

**Transactional Outbox**

If the service later persists transactions to a database before publishing them, an Outbox Pattern could be introduced to prevent inconsistencies between database commits and message publishing.

**Dead Letter Queue**

Messages that repeatedly fail downstream processing could be routed to a dead-letter queue for investigation or controlled reprocessing.

**Observability**

Production monitoring should include structured logging, distributed tracing, metrics, correlation IDs, and alerts for external-service and RabbitMQ failures.

**Secrets Management**

RabbitMQ credentials and other sensitive configuration should be supplied through environment variables or a managed secrets service rather than source-controlled configuration files.

**Downstream Consumer**

A separate worker/microservice could consume `partner-transactions` messages and integrate with the legacy or downstream transaction-processing system.

This consumer is intentionally outside the scope of this implementation.

---

## Trade-offs

The solution intentionally remains small and focused.

A database, downstream RabbitMQ consumer, distributed cache, authentication server, and additional microservices were not introduced because they are not required to demonstrate the requested integration flow.

The objective is to keep the implementation understandable while demonstrating clear separation of concerns, resilience, testability, and asynchronous integration patterns.

---

## Summary

The solution demonstrates a partner transaction integration flow using:

```text
HTTP API
   |
Validation
   |
Partner Verification
   |
Retry + Timeout
   |
RabbitMQ Publisher
   |
partner-transactions Queue
```

The implementation prioritises:

- Clean separation of concerns
- Dependency injection
- Resilient external HTTP communication
- Asynchronous messaging
- Centralised exception handling
- Automated testing
- Docker-based local development