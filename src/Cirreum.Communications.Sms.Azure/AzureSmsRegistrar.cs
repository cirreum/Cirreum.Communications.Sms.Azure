namespace Cirreum.Communications.Sms;

using Cirreum.Communications.Sms.Configuration;
using Cirreum.Communications.Sms.Extensions;
using Cirreum.Communications.Sms.Health;
using Cirreum.Providers;
using Cirreum.ServiceProvider;
using Cirreum.ServiceProvider.Health;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registrar responsible for auto-registering any configured SMS services for the
/// 'Azure' SMS Service Provider in the Communications section of application settings.
/// </summary>
public sealed class AzureSmsRegistrar() :
	ServiceProviderRegistrar<
		AzureSmsSettings,
		AzureSmsInstanceSettings,
		AzureSmsHealthCheckOptions> {

	/// <inheritdoc/>
	public override ProviderType ProviderType => ProviderType.Communications;

	/// <inheritdoc/>
	public override string ProviderName => "Azure";

	/// <inheritdoc/>
	public override string[] ActivitySourceNames { get; } = ["Azure.Communication.*", "Azure.Core.*"];

	/// <inheritdoc/>
	public override void ValidateSettings(AzureSmsInstanceSettings settings) {

		// Must have either connection string or endpoint
		if (string.IsNullOrWhiteSpace(settings.ConnectionString) && string.IsNullOrWhiteSpace(settings.Endpoint)) {
			throw new InvalidOperationException("Azure Communication Services ConnectionString or Endpoint is required");
		}

		// From is required
		if (string.IsNullOrWhiteSpace(settings.From)) {
			throw new InvalidOperationException("From phone number is required");
		}

	}

	/// <inheritdoc/>
	protected override void AddServiceProviderInstance(
		IServiceCollection services,
		string serviceKey,
		AzureSmsInstanceSettings settings) {
		services.AddAzureSmsService(serviceKey, settings);
	}

	/// <inheritdoc/>
	protected override IServiceProviderHealthCheck<AzureSmsHealthCheckOptions> CreateHealthCheck(
		IServiceProvider serviceProvider,
		string serviceKey,
		AzureSmsInstanceSettings settings) {
		return serviceProvider.CreateAzureSmsHealthCheck(serviceKey, settings);
	}

}
