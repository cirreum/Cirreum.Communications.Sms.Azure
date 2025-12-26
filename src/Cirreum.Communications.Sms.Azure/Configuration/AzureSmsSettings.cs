namespace Cirreum.Communications.Sms.Configuration;

using Cirreum.Communications.Sms.Health;
using Cirreum.ServiceProvider.Configuration;

public sealed class AzureSmsSettings
	: ServiceProviderSettings<
		AzureSmsInstanceSettings,
		AzureSmsHealthCheckOptions>;
