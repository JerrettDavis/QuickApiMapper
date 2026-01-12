# QuickApiMapper Demo Architecture

Visual architecture documentation using Mermaid diagrams to illustrate data flows, component interactions, and transformation workflows.

## Table of Contents

- [System Overview](#system-overview)
- [Component Architecture](#component-architecture)
- [Data Flow Diagrams](#data-flow-diagrams)
- [Sequence Diagrams](#sequence-diagrams)
- [Deployment Architecture](#deployment-architecture)
- [Integration Patterns](#integration-patterns)

## System Overview

### High-Level Architecture

```mermaid
graph TB
    subgraph "External Systems"
        ECommerce[E-Commerce Platform<br/>Demo.JsonApi<br/>Port 5100]
        Warehouse[Warehouse System<br/>Demo.SoapApi<br/>Port 5101]
    end

    subgraph "QuickApiMapper Platform"
        subgraph "Core Services"
            Web[QuickApiMapper Web<br/>Transformation Engine<br/>Port 5000]
            Management[Management API<br/>Configuration<br/>Port 7001]
            Designer[Designer Dashboard<br/>UI/Monitoring<br/>Port 7002]
        end

        subgraph "Infrastructure"
            Postgres[(PostgreSQL<br/>Configuration Store)]
            Redis[(Redis<br/>Cache)]
            RabbitMQ[RabbitMQ<br/>Message Queue]
        end
    end

    ECommerce -->|JSON POST| Web
    Web -->|SOAP POST| Warehouse
    Web -->|Read Config| Management
    Management -->|Store/Retrieve| Postgres
    Web -->|Cache| Redis
    RabbitMQ -->|Consume| Web
    Designer -->|Query| Management
    Web -->|Capture Messages| Postgres

    style Web fill:#4CAF50
    style Management fill:#2196F3
    style Designer fill:#FF9800
    style ECommerce fill:#9E9E9E
    style Warehouse fill:#9E9E9E
```

### Demo Ecosystem

```mermaid
graph LR
    subgraph "Demo Scenario"
        A[Modern E-Commerce<br/>JSON REST API] -->|Orders| B[QuickApiMapper Gateway]
        B -->|Fulfillment Requests| C[Legacy Warehouse<br/>SOAP Service]
        C -->|Status Updates| B
        B -->|Order Status| A
    end

    subgraph "Alternative Flows"
        D[Message Queue<br/>RabbitMQ] -->|Batch Orders| B
        B -->|Async Processing| D
    end

    subgraph "Monitoring"
        B -->|Message Capture| E[Designer Dashboard]
        E -->|Real-Time View| F[Users]
    end

    style B fill:#4CAF50,color:#fff
```

## Component Architecture

### QuickApiMapper Core Components

```mermaid
graph TB
    subgraph "QuickApiMapper.Web"
        API[Minimal API Endpoints]
        Engine[Mapping Engine Factory]
        Handler[Destination Handlers]
        Behavior[Behavior Pipeline]

        API --> Engine
        Engine --> Handler
        API --> Behavior
        Behavior --> Engine
    end

    subgraph "Mapping Engines"
        JsonToSoap[JsonToSoap Engine]
        SoapToJson[SoapToJson Engine]
        JsonToJson[JsonToJson Engine]
        XmlToXml[XmlToXml Engine]

        Engine --> JsonToSoap
        Engine --> SoapToJson
        Engine --> JsonToJson
        Engine --> XmlToXml
    end

    subgraph "Destination Handlers"
        SoapHandler[SOAP Handler]
        JsonHandler[JSON Handler]
        GrpcHandler[gRPC Handler]

        Handler --> SoapHandler
        Handler --> JsonHandler
        Handler --> GrpcHandler
    end

    subgraph "Cross-Cutting Concerns"
        Validation[Validation Behavior]
        Auth[Authentication Behavior]
        Timing[Timing Behavior]
        Logging[Logging Behavior]

        Behavior --> Validation
        Behavior --> Auth
        Behavior --> Timing
        Behavior --> Logging
    end

    style Engine fill:#4CAF50
    style Handler fill:#2196F3
    style Behavior fill:#FF9800
```

### Management & Designer Stack

```mermaid
graph TB
    subgraph "QuickApiMapper.Management.Api"
        MAPI[REST API Controllers]
        Service[Integration Service]
        Repo[Repository Pattern]
        EF[Entity Framework Core]

        MAPI --> Service
        Service --> Repo
        Repo --> EF
        EF --> DB[(PostgreSQL)]
    end

    subgraph "QuickApiMapper.Designer.Web"
        Blazor[Blazor Server Pages]
        MudBlazor[MudBlazor Components]
        Charts[Chart.js Integration]
        API_Client[HTTP Client to Management API]

        Blazor --> MudBlazor
        Blazor --> Charts
        Blazor --> API_Client
        API_Client --> MAPI
    end

    subgraph "Data Models"
        Integration[IntegrationMapping]
        Field[FieldMapping]
        Transformer[Transformer]
        Message[CapturedMessage]

        Integration --> Field
        Field --> Transformer
        Integration --> Message
    end

    Service --> Integration

    style MAPI fill:#2196F3
    style Blazor fill:#FF9800
    style DB fill:#4CAF50
```

## Data Flow Diagrams

### JSON to SOAP Transformation Flow

```mermaid
flowchart TD
    Start([Client Submits JSON Order]) --> Receive[QuickApiMapper Web<br/>Receives POST Request]
    Receive --> LoadConfig[Load Integration Configuration<br/>from Management API]
    LoadConfig --> Validate[Validation Behavior<br/>Check Request Format]
    Validate --> |Valid| SelectEngine[Select Mapping Engine<br/>JsonToSoapEngine]
    Validate --> |Invalid| Error1[Return 400 Bad Request]

    SelectEngine --> ParseJSON[Parse JSON Payload<br/>JSONPath Evaluation]
    ParseJSON --> ApplyMappings[Apply Field Mappings<br/>16 Mappings]

    ApplyMappings --> Transform1[Transformer: ToLower<br/>Email Normalization]
    Transform1 --> Transform2[Transformer: ToUpper<br/>SKU Standardization]
    Transform2 --> Transform3[Transformer: MapValue<br/>Priority Code Mapping]

    Transform3 --> BuildXML[Build XML Document<br/>XPath Construction]
    BuildXML --> BuildSoap[Wrap in SOAP Envelope<br/>Add Headers & Body]

    BuildSoap --> Capture1[Message Capture<br/>Store Input JSON]
    Capture1 --> Forward[Forward to Destination<br/>HTTP POST to SOAP Service]

    Forward --> |Success| ReceiveResponse[Receive SOAP Response]
    Forward --> |Error| Error2[Handle Error<br/>Dead-Letter Queue]

    ReceiveResponse --> Capture2[Message Capture<br/>Store Output SOAP]
    Capture2 --> Return[Return Response to Client]
    Return --> End([Complete])

    Error1 --> End
    Error2 --> End

    style SelectEngine fill:#4CAF50
    style Transform1 fill:#FF9800
    style Transform2 fill:#FF9800
    style Transform3 fill:#FF9800
    style BuildSoap fill:#2196F3
    style Capture1 fill:#9C27B0
    style Capture2 fill:#9C27B0
```

### SOAP to JSON Transformation Flow

```mermaid
flowchart TD
    Start([Client Submits SOAP Request]) --> Receive[QuickApiMapper Web<br/>Receives POST Request]
    Receive --> LoadConfig[Load Integration Configuration]
    LoadConfig --> SelectEngine[Select Mapping Engine<br/>SoapToJsonEngine]

    SelectEngine --> ParseSOAP[Parse SOAP Envelope<br/>Extract Body Content]
    ParseSOAP --> ParseXML[Parse XML Document<br/>XPath Evaluation]

    ParseXML --> ApplyMappings[Apply Field Mappings<br/>5 Mappings]
    ApplyMappings --> BuildJSON[Build JSON Object<br/>JSONPath Construction]

    BuildJSON --> Capture1[Message Capture<br/>Store Input SOAP]
    Capture1 --> Forward[Forward to Destination<br/>HTTP POST JSON]

    Forward --> |Success| ReceiveResponse[Receive JSON Response]
    Forward --> |Error| Error[Handle Error]

    ReceiveResponse --> Capture2[Message Capture<br/>Store Output JSON]
    Capture2 --> Return[Return Response to Client]
    Return --> End([Complete])

    Error --> End

    style SelectEngine fill:#4CAF50
    style BuildJSON fill:#2196F3
    style Capture1 fill:#9C27B0
    style Capture2 fill:#9C27B0
```

### RabbitMQ Worker Processing Flow

```mermaid
flowchart TD
    Start([Message Published to Queue]) --> Consumer[RabbitMQ Consumer<br/>Listening on Queue]
    Consumer --> Receive[Receive Message<br/>Extract Headers & Body]

    Receive --> GetIntegration[Determine Integration<br/>From Header or Routing Key]
    GetIntegration --> |Found| LoadConfig[Load Integration Config]
    GetIntegration --> |Not Found| DeadLetter1[Send to Dead-Letter Queue]

    LoadConfig --> SelectEngine[Select Mapping Engine<br/>Based on Source/Dest Types]
    SelectEngine --> Transform[Apply Transformation<br/>Same as HTTP Flow]

    Transform --> Capture1[Message Capture<br/>Store Input]
    Capture1 --> Forward[Forward to Destination]

    Forward --> |Success| Capture2[Message Capture<br/>Store Output]
    Forward --> |Error| DeadLetter2[Send to Dead-Letter Queue]

    Capture2 --> Ack[Acknowledge Message<br/>Remove from Queue]
    Ack --> End([Complete])

    DeadLetter1 --> Ack
    DeadLetter2 --> Ack

    style Consumer fill:#FF5722
    style SelectEngine fill:#4CAF50
    style Transform fill:#2196F3
    style Capture1 fill:#9C27B0
    style Capture2 fill:#9C27B0
    style DeadLetter1 fill:#F44336
    style DeadLetter2 fill:#F44336
```

## Sequence Diagrams

### End-to-End Order Processing

```mermaid
sequenceDiagram
    participant Client as E-Commerce Client
    participant Web as QuickApiMapper Web
    participant Mgmt as Management API
    participant Engine as Mapping Engine
    participant Transformer as Transformers
    participant SOAP as Warehouse SOAP API
    participant DB as PostgreSQL

    Client->>Web: POST /api/demo/fulfillment/submit<br/>(JSON Order)
    activate Web

    Web->>Mgmt: GET /api/integrations?endpoint=/api/demo/fulfillment/submit
    activate Mgmt
    Mgmt->>DB: SELECT FROM integrationmappings
    DB-->>Mgmt: Integration + Field Mappings
    Mgmt-->>Web: Integration Config
    deactivate Mgmt

    Web->>Engine: Transform(JSON, Config)
    activate Engine

    Engine->>Engine: Parse JSON with JSONPath
    Engine->>Transformer: ToLower(email)
    Transformer-->>Engine: "john.smith@example.com"

    Engine->>Transformer: ToUpper(sku)
    Transformer-->>Engine: "LAPTOP-XPS15"

    Engine->>Transformer: MapValue(priority)
    Transformer-->>Engine: "STD"

    Engine->>Engine: Build XML Document
    Engine->>Engine: Wrap in SOAP Envelope
    Engine-->>Web: SOAP XML
    deactivate Engine

    Web->>DB: INSERT INTO capturedmessages (input)
    DB-->>Web: Message ID

    Web->>SOAP: POST /WarehouseService.asmx<br/>(SOAP Request)
    activate SOAP
    SOAP->>SOAP: Process Fulfillment
    SOAP-->>Web: SOAP Response (Confirmation)
    deactivate SOAP

    Web->>DB: UPDATE capturedmessages (output, status)
    DB-->>Web: Updated

    Web-->>Client: 200 OK (Confirmation JSON)
    deactivate Web
```

### Designer Dashboard Query Flow

```mermaid
sequenceDiagram
    participant User as User Browser
    participant Designer as Designer Dashboard
    participant Mgmt as Management API
    participant DB as PostgreSQL

    User->>Designer: Navigate to Message History
    activate Designer

    Designer->>Designer: Render Message List Component

    Designer->>Mgmt: GET /api/messages?pageSize=50&status=Success
    activate Mgmt
    Mgmt->>DB: SELECT FROM capturedmessages<br/>WHERE status = 'Success'<br/>ORDER BY timestamp DESC<br/>LIMIT 50
    DB-->>Mgmt: Message Records
    Mgmt-->>Designer: JSON Array of Messages
    deactivate Mgmt

    Designer->>Designer: Populate MudBlazor Table

    Designer-->>User: Display Message List
    deactivate Designer

    User->>Designer: Click on Message
    activate Designer

    Designer->>Mgmt: GET /api/messages/{id}
    activate Mgmt
    Mgmt->>DB: SELECT * FROM capturedmessages<br/>WHERE id = {id}
    DB-->>Mgmt: Full Message Details
    Mgmt-->>Designer: Message with Input/Output Payloads
    deactivate Mgmt

    Designer->>Designer: Render Message Detail View<br/>Side-by-Side Input/Output

    Designer-->>User: Display Transformation Details
    deactivate Designer
```

### RabbitMQ Message Processing

```mermaid
sequenceDiagram
    participant Publisher as Message Publisher
    participant RabbitMQ as RabbitMQ Queue
    participant Consumer as RabbitMQ Consumer
    participant Web as QuickApiMapper Core
    participant DLQ as Dead-Letter Queue

    Publisher->>RabbitMQ: Publish Message<br/>Headers: IntegrationName=Demo:RabbitMQ...
    activate RabbitMQ

    RabbitMQ->>Consumer: Deliver Message (DeliveryTag: 123)
    activate Consumer

    Consumer->>Consumer: Extract IntegrationName from Headers
    Consumer->>Web: ProcessMessageAsync(body, integrationName)
    activate Web

    alt Integration Found
        Web->>Web: Transform Message
        Web->>Web: Forward to Destination
        Web-->>Consumer: Success
        Consumer->>RabbitMQ: BasicAck(deliveryTag: 123)
        RabbitMQ-->>Consumer: Acknowledged
    else Integration Not Found
        Web-->>Consumer: Error: Integration Not Found
        Consumer->>RabbitMQ: BasicReject(deliveryTag: 123, requeue: false)
        RabbitMQ->>DLQ: Route to Dead-Letter Queue
    end

    deactivate Web
    deactivate Consumer
    deactivate RabbitMQ
```

## Deployment Architecture

### Aspire Orchestration

```mermaid
graph TB
    subgraph "Aspire AppHost"
        AppHost[QuickApiMapper.Host.AppHost]
    end

    subgraph "Service Container"
        Web[QuickApiMapper.Web<br/>Container: web]
        Mgmt[QuickApiMapper.Management.Api<br/>Container: management-api]
        Designer[QuickApiMapper.Designer.Web<br/>Container: designer-web]
        JsonApi[Demo.JsonApi<br/>Container: demo-jsonapi]
        SoapApi[Demo.SoapApi<br/>Container: demo-soapapi]
    end

    subgraph "Infrastructure Container"
        Postgres[PostgreSQL Container<br/>Image: postgres:16]
        Redis[Redis Container<br/>Image: redis:7]
        RabbitMQ[RabbitMQ Container<br/>Image: rabbitmq:3-management]
    end

    AppHost -->|Orchestrates| Web
    AppHost -->|Orchestrates| Mgmt
    AppHost -->|Orchestrates| Designer
    AppHost -->|Orchestrates| JsonApi
    AppHost -->|Orchestrates| SoapApi
    AppHost -->|Manages| Postgres
    AppHost -->|Manages| Redis
    AppHost -->|Manages| RabbitMQ

    Web -.->|Connects| Postgres
    Web -.->|Connects| Redis
    Web -.->|Connects| RabbitMQ
    Mgmt -.->|Connects| Postgres
    Designer -.->|HTTP Client| Mgmt

    style AppHost fill:#4CAF50,color:#fff
    style Web fill:#2196F3,color:#fff
    style Mgmt fill:#2196F3,color:#fff
    style Designer fill:#FF9800,color:#fff
```

### Network Flow

```mermaid
flowchart LR
    subgraph "External Network"
        Client[Client/Browser]
    end

    subgraph "Aspire Service Discovery"
        Gateway[Service Discovery<br/>DNS Resolution]
    end

    subgraph "Service Mesh"
        Web[web:5000]
        Mgmt[management-api:7001]
        Designer[designer-web:7002]
        JsonApi[demo-jsonapi:5100]
        SoapApi[demo-soapapi:5101]
    end

    Client -->|HTTP/HTTPS| Gateway
    Gateway -->|Route to web| Web
    Gateway -->|Route to management-api| Mgmt
    Gateway -->|Route to designer-web| Designer
    Gateway -->|Route to demo-jsonapi| JsonApi

    Web -->|Service-to-Service| SoapApi
    Designer -->|Service-to-Service| Mgmt

    style Gateway fill:#4CAF50
```

## Integration Patterns

### Synchronous HTTP Integration Pattern

```mermaid
graph LR
    subgraph "Source System"
        A[REST Client]
    end

    subgraph "QuickApiMapper"
        B[HTTP Endpoint]
        C[Transformation Engine]
        D[HTTP Client]
    end

    subgraph "Destination System"
        E[SOAP Service]
    end

    A -->|1. POST JSON| B
    B -->|2. Load Config & Transform| C
    C -->|3. POST SOAP| D
    D -->|4. Forward| E
    E -->|5. Response| D
    D -->|6. Return| C
    C -->|7. Return| B
    B -->|8. Response| A

    style C fill:#4CAF50
```

### Asynchronous Message Queue Pattern

```mermaid
graph TB
    subgraph "Source Systems"
        A1[System A]
        A2[System B]
        A3[System C]
    end

    subgraph "Message Queue"
        Q[RabbitMQ Exchange]
        Q1[Queue 1]
        Q2[Queue 2]
    end

    subgraph "QuickApiMapper Workers"
        W1[Worker 1<br/>Integration A]
        W2[Worker 2<br/>Integration B]
    end

    subgraph "Destination Systems"
        D1[Destination 1]
        D2[Destination 2]
    end

    A1 -->|Publish| Q
    A2 -->|Publish| Q
    A3 -->|Publish| Q

    Q -->|Route| Q1
    Q -->|Route| Q2

    Q1 -->|Consume| W1
    Q2 -->|Consume| W2

    W1 -->|Transform & Forward| D1
    W2 -->|Transform & Forward| D2

    style Q fill:#FF5722
    style W1 fill:#4CAF50
    style W2 fill:#4CAF50
```

### Bi-Directional Integration Pattern

```mermaid
graph TB
    subgraph "Modern System"
        Modern[JSON REST API]
    end

    subgraph "QuickApiMapper Gateway"
        FwdInt[Integration 1:<br/>JSON to SOAP]
        RevInt[Integration 2:<br/>SOAP to JSON]
    end

    subgraph "Legacy System"
        Legacy[SOAP Service]
    end

    Modern -->|Orders<br/>JSON| FwdInt
    FwdInt -->|Fulfillment<br/>SOAP| Legacy

    Legacy -->|Status<br/>SOAP| RevInt
    RevInt -->|Updates<br/>JSON| Modern

    style FwdInt fill:#4CAF50
    style RevInt fill:#2196F3
```

### Hybrid Integration Pattern (HTTP + Queue)

```mermaid
graph TB
    subgraph "Clients"
        C1[Direct HTTP Client]
        C2[Message Publisher]
    end

    subgraph "Integration Layer"
        HTTP[HTTP Endpoint<br/>Integration Config A]
        Queue[Message Queue Consumer<br/>Integration Config A]
    end

    subgraph "QuickApiMapper Core"
        Engine[Same Mapping Engine<br/>Same Configuration]
    end

    subgraph "Destination"
        Dest[SOAP Service]
    end

    C1 -->|Real-Time| HTTP
    C2 -->|Async| Queue

    HTTP -->|Transform| Engine
    Queue -->|Transform| Engine

    Engine -->|Forward| Dest

    style Engine fill:#4CAF50
```

### Transformation Pipeline

```mermaid
graph LR
    Input[Input Payload] --> Parse[Parse<br/>JSON/XML/SOAP]
    Parse --> Map[Apply Field Mappings<br/>JSONPath/XPath]
    Map --> T1[Transformer 1<br/>ToLower]
    T1 --> T2[Transformer 2<br/>ToUpper]
    T2 --> T3[Transformer 3<br/>MapValue]
    T3 --> Build[Build Output<br/>JSON/XML/SOAP]
    Build --> Envelope[Wrap in Envelope<br/>if SOAP]
    Envelope --> Output[Output Payload]

    style Map fill:#4CAF50
    style T1 fill:#FF9800
    style T2 fill:#FF9800
    style T3 fill:#FF9800
    style Build fill:#2196F3
```

---

**Document Version**: 1.0
**Last Updated**: 2026-01-11
**Related Documents**: [DEMO_GUIDE.md](DEMO_GUIDE.md), [DEMO_IMPLEMENTATION_PLAN.md](DEMO_IMPLEMENTATION_PLAN.md)
