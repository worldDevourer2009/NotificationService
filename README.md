# Notification Service

## Project Overview
The **Notification Service** is a robust and scalable service designed to handle the delivery of notifications across multiple channels, such as email, real-time communication, and integrations with external platforms like Telegram. Following clean architecture principles, the project is split into several layers for better maintainability, scalability, and testability.  
This project leverages the power of modern technologies like **Entity Framework Core**, **ASP.NET Core**, **Kafka**, **Redis**, and **SignalR**, along with a modular design to deliver notifications efficiently and seamlessly.

## Goals and Features

### Primary Goals:
- Deliver notifications via various communication channels.
- Provide an extensible architecture to support new channels in the future.
- Ensure reliability, scalability, and real-time capabilities.

### Key Features:
1. **Multi-channel Notifications**:
    - Support for Email Notifications.
    - Real-time WebSocket-based features using SignalR.
    - Telegram Bot integration for instant messaging.

2. **Event-driven Architecture**:
    - Kafka for managing asynchronous messaging between different services.

3. **Caching and State Management**:
    - Redis integration for high-speed in-memory caching.
    - In-memory database support for testing.

4. **Database Support**:
    - PostgreSQL support via Entity Framework Core.
    - Enables flexible querying and migration functionality.

5. **Extensibility**:
    - Easily configurable to add more channels or replace integrations as needed.

## Project Architecture
The project adopts **Clean Architecture** principles. It is divided into three main layers:

1. **Domain Layer**:
    - Contains core business entities and domain rules.
    - Independent of external dependencies.

2. **Application Layer**:
    - Contains business logic, use cases, and mediators for workflows.
    - Interacts with the infrastructure and domain layers without being tightly coupled.

3. **Infrastructure Layer**:
    - Implements database, caching, and messaging mechanisms.
    - Handles external integrations.
4. **Api Layer**
   - Controllers and external API of service (available with auth service)
   - Middleware to autentificate services in service to service requests

## Technologies and Dependencies

### Framework:
- **.NET 9.0**

### Libraries and Tools:
- **Database**:
    - PostgreSQL with Entity Framework Core.
    - In-memory and relational database support.

- **Messaging**:
    - Kafka for event-driven communication.

- **Caching**:
    - Redis for high-performance caching.

- **Real-time Communication**:
    - SignalR for WebSocket-style real-time notifications.

- **Email Service**:
    - MailKit for managing email functionality.

- **Telegram**:
    - Telegram.Bot for integration with Telegram instant messaging.

- **Dependency Injection & Hosting**:
    - Microsoft.Extensions.* libraries for configuration, DI, and hosting.
