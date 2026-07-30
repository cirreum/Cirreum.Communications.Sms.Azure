namespace Cirreum.Communications.Sms.Extensions;

using Azure.Communication.Sms;
using Azure.Core;
using Azure.Identity;
using Cirreum.Communications.Sms.Configuration;
using Cirreum.Communications.Sms.Health;
using Cirreum.Providers.Configuration;
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

		// Mirrors the client construction below: a non-blank connection string is key-based
		// authentication, which a Credential block cannot apply to.
		if (!string.IsNullOrWhiteSpace(settings.ConnectionString) && settings.Credential is not null) {
			throw new InvalidOperationException(
				"A Credential block is configured but the instance uses a connection string. " +
				"Identity-based authentication requires Endpoint without a connection string.");
		}

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
			// Endpoint authentication — identity selected by the instance Credential block
			client = new SmsClient(new Uri(settings.Endpoint), settings.GetCredential());
		} else {
			throw new InvalidOperationException("Either ConnectionString or Endpoint must be configured");
		}

		return new AzureSmsService(
			client,
			settings,
			logger);

	}

	private static TokenCredential GetCredential(
		this AzureSmsInstanceSettings settings) {

		var tenantId = string.IsNullOrWhiteSpace(settings.Identifier) ? null : settings.Identifier;
		var credential = settings.Credential ?? new CredentialSettings();
		var identityId = string.IsNullOrWhiteSpace(credential.IdentityId) ? null : credential.IdentityId;

		return credential.Mode switch {

			CredentialMode.Default => new DefaultAzureCredential(new DefaultAzureCredentialOptions {
				TenantId = tenantId,
				ManagedIdentityClientId = identityId,
			}),

			CredentialMode.ManagedIdentity => new ManagedIdentityCredential(
				identityId is null
					? ManagedIdentityId.SystemAssigned
					: ManagedIdentityId.FromUserAssignedClientId(identityId)),

			CredentialMode.Developer => new ChainedTokenCredential(
				new VisualStudioCredential(new VisualStudioCredentialOptions { TenantId = tenantId }),
				new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId }),
				new AzurePowerShellCredential(new AzurePowerShellCredentialOptions { TenantId = tenantId })),

			_ => throw new InvalidOperationException(
				$"CredentialMode '{credential.Mode}' is not supported by the Azure SMS provider."),

		};

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
