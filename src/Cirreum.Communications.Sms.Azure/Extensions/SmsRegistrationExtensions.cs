namespace Cirreum.Communications.Sms.Extensions;

using Azure.Communication.Sms;
using Azure.Identity;
using Cirreum.Communications.Sms.Configuration;
using Cirreum.Communications.Sms.Health;
using Cirreum.ServiceProvider.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal static class SmsRegistrationExtensions {

	public static void AddAzureSmsService(
		this IServiceCollection services,
		string serviceKey,
		AzureSmsInstanceSettings settings) {

		// Register Keyed Service Factory
		services.AddKeyedSingleton<ISmsService>(
			serviceKey,
			(sp, key) => sp.CreateAzureSmsClient(settings));

		// Register Default (non-Keyed) Service Factory (wraps the keyed registration)
		if (serviceKey.Equals(ServiceProviderSettings.DefaultKey, StringComparison.OrdinalIgnoreCase)) {
			services.TryAddSingleton(sp => sp.GetRequiredKeyedService<ISmsService>(serviceKey));
		}

	}

	private static AzureSmsService CreateAzureSmsClient(
		this IServiceProvider serviceProvider,
		AzureSmsInstanceSettings settings) {

		var logger = serviceProvider.GetRequiredService<ILogger<AzureSmsService>>();

		// Create client based on authentication method
		SmsClient client;
		if (!string.IsNullOrWhiteSpace(settings.ConnectionString)) {
			// Connection string authentication
			client = new SmsClient(settings.ConnectionString);
		} else if (!string.IsNullOrWhiteSpace(settings.Endpoint)) {
			// Endpoint authentication with managed identity
			client = new SmsClient(new Uri(settings.Endpoint), new DefaultAzureCredential());
		} else {
			throw new InvalidOperationException("Either ConnectionString or Endpoint must be configured");
		}

		return new AzureSmsService(
			client,
			settings,
			logger);

	}

	public static AzureSmsHealthCheck CreateAzureSmsHealthCheck(
		this IServiceProvider serviceProvider,
		string serviceKey,
		AzureSmsInstanceSettings settings) {
		var env = serviceProvider.GetRequiredService<IHostEnvironment>();
		var cache = serviceProvider.GetRequiredService<IMemoryCache>();
		var client = serviceProvider.GetRequiredKeyedService<ISmsService>(serviceKey);
		return new AzureSmsHealthCheck(
			client,
			env.IsProduction(),
			cache,
			settings);
	}

}
