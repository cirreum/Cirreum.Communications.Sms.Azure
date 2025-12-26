# Cirreum.Communications.Sms.Azure

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Communications.Sms.Azure.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Communications.Sms.Azure/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Communications.Sms.Azure.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Communications.Sms.Azure/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Communications.Sms.Azure?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Communications.Sms.Azure/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Communications.Sms.Azure?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Communications.Sms.Azure/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**A robust, production-ready SMS library for Azure Communication Services integration in .NET applications.**

## Overview

**Cirreum.Communications.Sms.Azure** provides a comprehensive, enterprise-grade SMS communication solution built on Azure Communication Services. It offers seamless integration with .NET applications through a clean, type-safe API with built-in health checks, bulk messaging capabilities, and advanced features like retry logic and phone number validation.

## Features

- **🚀 Simple API** - Clean, intuitive methods for sending SMS messages
- **📱 Phone Number Validation** - Built-in libphonenumber integration for reliable validation
- **⚡ Bulk Messaging** - Efficient parallel processing for large message batches
- **🔄 Retry Logic** - Exponential backoff with jittered delays for rate limiting
- **🏥 Health Checks** - Comprehensive health monitoring with configurable validation
- **🔐 Flexible Authentication** - Support for connection string or managed identity
- **🔧 DI Integration** - First-class dependency injection support with keyed services
- **📊 Structured Logging** - Rich logging with proper correlation and error details

## Quick Start

### Installation

```bash
dotnet add package Cirreum.Communications.Sms.Azure
```

### Basic Usage

```csharp
// Register with DI container
builder.AddAzureSmsClient("default", settings => {
    settings.ConnectionString = "endpoint=https://your-resource.communication.azure.com/;accesskey=...";
    settings.From = "+15551234567";
});

// Inject and use
public class NotificationService {
    private readonly ISmsService _sms;

    public NotificationService(ISmsService sms) => _sms = sms;

    public async Task SendWelcomeMessage(string phoneNumber) {
        var result = await _sms.SendFromAsync(
            from: "+15551234567",
            to: phoneNumber,
            message: "Welcome to our service!");

        if (result.Success) {
            Console.WriteLine($"Message ID: {result.MessageId}");
        }
    }
}
```

### Bulk Messaging

```csharp
var phoneNumbers = new[] { "+15551234567", "+15559876543", "+15551122334" };

var response = await _sms.SendBulkAsync(
    message: "System maintenance scheduled for tonight.",
    phoneNumbers: phoneNumbers,
    from: "+15550001111");

Console.WriteLine($"Sent: {response.Sent}, Failed: {response.Failed}");
```

### Using Managed Identity

```csharp
builder.AddAzureSmsClient("default", settings => {
    settings.Endpoint = "https://your-resource.communication.azure.com";
    settings.From = "+15551234567";
});
```

## Configuration

### Via appsettings.json

```json
{
  "ServiceProviders": {
    "Communications": {
      "Sms.Azure": {
        "Instances": {
          "default": {
            "ConnectionString": "endpoint=https://...;accesskey=...",
            "From": "+15551234567",
            "MaxRetries": 3,
            "BulkOptions": {
              "MaxConcurrency": 10
            }
          }
        }
      }
    }
  }
}
```

### Via Connection String (Key Vault)

```csharp
builder.AddAzureSmsClient("production",
    connectionString: """{"connectionString":"endpoint=https://...","from":"+15551234567"}""");
```

## Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<AzureSmsHealthCheck>("azure-sms");

// Configure health check options
builder.AddAzureSmsClient("default", settings, healthOptions => {
    healthOptions.TestSending = false; // Set to true for production validation
    healthOptions.PhoneNumber = "+15551234567"; // Test phone number
    healthOptions.CachedResultTimeout = TimeSpan.FromHours(6);
});
```

## Azure Communication Services Limitations

Azure Communication Services SMS has some limitations compared to other providers:

- **No Messaging Service** - ACS sends from a specific phone number only (no messaging service concept)
- **No Scheduled Sending** - Messages are sent immediately; scheduling is not supported
- **No MMS** - Only plain text SMS is supported; media attachments are not available
- **No Custom Callbacks** - Use Azure Event Grid for delivery reports instead

Attempting to use unsupported features will throw `NotSupportedException`.

## Contribution Guidelines

1. **Be conservative with new abstractions**
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Communications.Sms.Azure follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

Given its foundational role, major version bumps are rare and carefully considered.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**
*Layered simplicity for modern .NET*
