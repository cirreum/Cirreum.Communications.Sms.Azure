namespace Microsoft.Extensions.Hosting;

using Cirreum.Communications.Sms;
using Cirreum.Communications.Sms.Configuration;
using Cirreum.Communications.Sms.Health;

public static class HostingExtensions {

	/// <summary>
	/// Adds a manually configured <see cref="ISmsService"/> instance for Azure Communication Services SMS.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="settings">The configured instance settings.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IHostApplicationBuilder"/>.</returns>
	public static IHostApplicationBuilder AddAzureSmsClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		AzureSmsInstanceSettings settings,
		Action<AzureSmsHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		// Configure health options
		settings.HealthOptions ??= new AzureSmsHealthCheckOptions();
		configureHealthCheckOptions?.Invoke(settings.HealthOptions);

		// Reuse our Registrar...
		var registrar = new AzureSmsRegistrar();
		registrar.RegisterInstance(
			serviceKey,
			settings,
			builder.Services,
			builder.Configuration);

		return builder;

	}

	/// <summary>
	/// Adds a manually configured <see cref="ISmsService"/> instance for Azure Communication Services SMS.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="configure">The callback to configure the instance settings.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IHostApplicationBuilder"/>.</returns>
	public static IHostApplicationBuilder AddAzureSmsClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		Action<AzureSmsInstanceSettings> configure,
		Action<AzureSmsHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		var settings = new AzureSmsInstanceSettings();
		configure?.Invoke(settings);
		if (string.IsNullOrWhiteSpace(settings.Name)) {
			settings.Name = serviceKey;
		}

		return AddAzureSmsClient(builder, serviceKey, settings, configureHealthCheckOptions);

	}

	/// <summary>
	/// Adds a manually configured <see cref="ISmsService"/> instance for Azure Communication Services SMS.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="azureConfiguration">The JSON string containing connectionString/endpoint and optional from settings.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IHostApplicationBuilder"/>.</returns>
	public static IHostApplicationBuilder AddAzureSmsClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		string azureConfiguration,
		Action<AzureSmsHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		var settings = new AzureSmsInstanceSettings() {
			ConnectionString = azureConfiguration,
			Name = serviceKey
		};

		return AddAzureSmsClient(builder, serviceKey, settings, configureHealthCheckOptions);

	}

}
