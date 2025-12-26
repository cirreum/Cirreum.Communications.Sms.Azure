namespace Cirreum.Communications.Sms.Configuration;

using Cirreum.Communications.Sms.Health;
using Cirreum.ServiceProvider.Configuration;
using System.Text.Json;

/// <summary>
/// Configuration settings for an Azure Communication Services SMS instance.
/// Provides comprehensive configuration options for Azure SMS integration including
/// authentication, default settings, batching, and health monitoring.
/// </summary>
public sealed class AzureSmsInstanceSettings
	: ServiceProviderInstanceSettings<AzureSmsHealthCheckOptions> {

	/// <summary>
	/// Gets or sets the Azure Communication Services endpoint.
	/// Used when authenticating with Azure AD instead of connection string.
	/// </summary>
	/// <value>The endpoint URI, or null if using connection string.</value>
	public string? Endpoint { get; set; }

	/// <summary>
	/// Gets or sets the phone number to send SMS messages from.
	/// Must be a valid E.164 format number (e.g., "+15551234567") provisioned in Azure Communication Services.
	/// </summary>
	public string From { get; set; } = "";

	/// <summary>
	/// Gets or sets the maximum number of retry attempts for failed requests.
	/// Valid range: 0-10.
	/// </summary>
	private int _maxRetries = 3;
	public int MaxRetries {
		get => _maxRetries;
		set => _maxRetries = Math.Clamp(value, 0, 10);
	}

	/// <summary>
	/// Gets or sets the bulk sending options.
	/// </summary>
	public AzureSmsBulkSettings BulkOptions { get; set; } = new();

	/// <summary>
	/// Gets or sets the tag to include with SMS messages for tracking purposes.
	/// </summary>
	public string? Tag { get; set; }

	/// <summary>
	/// Gets or sets the health check options for monitoring the Azure SMS service instance.
	/// </summary>
	public override AzureSmsHealthCheckOptions? HealthOptions { get; set; } = new();

	/// <summary>
	/// Parses a JSON connection string to populate the ConnectionString, Endpoint, and From properties.
	/// Allows KV/Env secret to be provided as JSON with connection details.
	/// Expected JSON format: { "connectionString":"...", "from":"+15551234567" }
	/// or: { "endpoint":"https://...", "from":"+15551234567" }
	/// </summary>
	/// <param name="jsonValue">The JSON string containing the configuration data.</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the JSON is invalid, missing required fields, or cannot be parsed.
	/// </exception>
	public override void ParseConnectionString(string jsonValue) {

		// Store the original JSON for debugging purposes
		this.ConnectionString = jsonValue;

		try {
			var options =
				JsonSerializer.Deserialize<AzureConnectionData>(jsonValue, JsonSerializerOptions.Web)
				?? throw new InvalidOperationException("Invalid Azure SMS configuration JSON.");

			// Must have either ConnectionString or Endpoint
			if (string.IsNullOrWhiteSpace(options.ConnectionString) && string.IsNullOrWhiteSpace(options.Endpoint)) {
				throw new InvalidOperationException("Either ConnectionString or Endpoint must be provided in Azure configuration JSON.");
			}

			// Set the appropriate authentication properties
			if (!string.IsNullOrWhiteSpace(options.ConnectionString)) {
				this.ConnectionString = options.ConnectionString;
			}

			if (!string.IsNullOrWhiteSpace(options.Endpoint)) {
				this.Endpoint = options.Endpoint;
			}

			// local appsettings takes precedence over KeyVault
			if (string.IsNullOrWhiteSpace(this.From) && !string.IsNullOrWhiteSpace(options.From)) {
				this.From = options.From;
			}

		} catch (JsonException ex) {
			throw new InvalidOperationException("Invalid Azure SMS configuration format.", ex);
		}
	}

	/// <summary>
	/// Internal record used for deserializing connection string JSON data.
	/// </summary>
	private sealed record AzureConnectionData(
		string? ConnectionString,
		string? Endpoint,
		string? From
	);

}
