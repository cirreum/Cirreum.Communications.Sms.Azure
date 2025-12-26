namespace Cirreum.Communications.Sms.Health;

using Cirreum.Health;

/// <summary>
/// Represents a collection of settings that configure an
/// <see cref="AzureSmsHealthCheck"> health check</see>.
/// </summary>
public class AzureSmsHealthCheckOptions :
	ServiceProviderHealthCheckOptions {

	/// <summary>
	/// Gets or sets the cache duration for health check results.
	/// Default is 6 hours. Set to null to disable caching.
	/// </summary>
	public override TimeSpan? CachedResultTimeout { get; set; } = TimeSpan.FromHours(6);

	/// <summary>
	/// Test phone number used for validation during health checks.
	/// Should be a valid E.164 format number (e.g., "+15551234567").
	/// </summary>
	public string PhoneNumber { get; set; } = "";

	/// <summary>
	/// Set to true to test sending an actual message using the From number.
	/// Default is false.
	/// </summary>
	/// <remarks>
	/// Caution, depending on the cache settings, this could be expensive.
	/// </remarks>
	public bool TestSending { get; set; } = false;

}
