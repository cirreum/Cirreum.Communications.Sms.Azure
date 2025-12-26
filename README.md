# Cirreum.Communications.Sms.Azure

Azure Communication Services SMS provider for the Cirreum Communications framework.

## Installation

```bash
dotnet add package Cirreum.Communications.Sms.Azure
```

## Features

- Send SMS messages via Azure Communication Services
- Bulk SMS sending with parallel processing
- Phone number validation and E.164 formatting
- Health checks for monitoring
- Support for both connection string and managed identity authentication
- Configurable retry logic with exponential backoff
- Delivery status callbacks

## Configuration

### Using appsettings.json

```json
{
  "ConnectionStrings": {
    "azure-sms": "{\"connectionString\":\"endpoint=https://...\"}"
  },
  "Communications": {
    "Azure": {
      "Tracing": true,
      "Instances": {
        "default": {
          "Name": "azure-sms",
          "From": "+15551234567",
          "HealthChecks": true,
          "HealthOptions": {
            "IncludeInReadinessCheck": true,
            "PhoneNumber": "+15559876543"
          }
        }
      }
    }
  }
}
```

### Using Code Configuration

```csharp
builder.AddAzureSmsClient("default", settings => {
    settings.ConnectionString = "endpoint=https://your-resource.communication.azure.com/;accesskey=...";
    settings.From = "+15551234567";
});
```

### Using Managed Identity

```csharp
builder.AddAzureSmsClient("default", settings => {
    settings.Endpoint = "https://your-resource.communication.azure.com";
    settings.From = "+15551234567";
});
```

## Usage

```csharp
public class MyService {
    private readonly ISmsService _smsService;

    public MyService(ISmsService smsService) {
        _smsService = smsService;
    }

    public async Task SendNotification(string phoneNumber, string message) {
        var result = await _smsService.SendFromAsync(
            from: "+15551234567",
            to: phoneNumber,
            message: message);

        if (!result.Success) {
            Console.WriteLine($"Failed: {result.ErrorMessage}");
        }
    }

    public async Task SendBulkNotifications(IEnumerable<string> phoneNumbers, string message) {
        var response = await _smsService.SendBulkAsync(
            message: message,
            phoneNumbers: phoneNumbers,
            countryCode: "US");

        Console.WriteLine($"Sent: {response.Sent}, Failed: {response.Failed}");
    }
}
```

## Azure Communication Services Limitations

Azure Communication Services SMS has some limitations compared to other providers:

- **No Messaging Service**: ACS sends from a specific phone number only (no messaging service concept)
- **No Scheduled Sending**: Messages are sent immediately; scheduling is not supported
- **No MMS**: Only plain text SMS is supported; media attachments are not available

Attempting to use unsupported features will throw `NotSupportedException`.

## License

MIT
