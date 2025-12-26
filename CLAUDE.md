# Cirreum.Communications.Sms.Azure

Azure Communication Services SMS provider implementation for the Cirreum framework.

## Project Structure

```
src/Cirreum.Communications.Sms.Azure/
├── Configuration/
│   ├── AzureSmsBulkSettings.cs      - Bulk sending options
│   ├── AzureSmsInstanceSettings.cs  - Instance-specific configuration
│   └── AzureSmsSettings.cs          - Provider-level settings
├── Extensions/
│   ├── Hosting/
│   │   └── HostingExtensions.cs     - IHostApplicationBuilder extensions
│   └── SmsRegistrationExtensions.cs - DI registration helpers
├── Health/
│   ├── AzureSmsHealthCheck.cs       - Health check implementation
│   └── AzureSmsHealthCheckOptions.cs- Health check configuration
├── Logging/
│   └── AzureSmsServiceLogging.cs    - Structured logging extensions
├── AzureSmsRegistrar.cs             - Service provider registrar
└── AzureSmsService.cs               - ISmsService implementation
```

## Key Patterns

- Extends `ServiceProviderRegistrar<TSettings, TInstanceSettings, THealthOptions>` from Cirreum.ServiceProvider
- Implements `ISmsService` from Cirreum.Communications.Sms
- Uses source-generated logging via `[LoggerMessage]` attribute
- Supports keyed DI services for multi-instance scenarios

## Building

```bash
dotnet build Cirreum.Communications.Sms.Azure.slnx
```

## Dependencies

- Azure.Communication.Sms - Azure Communication Services SDK
- Azure.Identity - Azure AD/Managed Identity authentication
- libphonenumber-csharp.extensions - Phone number parsing/validation
- Cirreum.Communications.Sms - Core SMS abstractions
- Cirreum.ServiceProvider - Service provider framework
