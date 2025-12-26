namespace Cirreum.Communications.Sms.Configuration;

/// <summary>
/// Configuration settings for bulk SMS operations via Azure Communication Services.
/// </summary>
public class AzureSmsBulkSettings {

	/// <summary>
	/// Gets or sets the maximum number of concurrent SMS operations during bulk sending.
	/// </summary>
	/// <value>The maximum concurrency level. Defaults to 10.</value>
	public int MaxConcurrency { get; set; } = 10;

}
