namespace Cirreum.Communications.Sms;

using Azure;
using Azure.Communication.Sms;
using Cirreum.Communications.Sms.Configuration;
using Cirreum.Communications.Sms.Logging;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

internal sealed class AzureSmsService(
	SmsClient client,
	AzureSmsInstanceSettings settings,
	ILogger<AzureSmsService> logger
) : ISmsService {

	private const string LogHeader = "Azure SMS";
	private readonly PhoneNumbers.PhoneNumberUtil _phoneUtil = PhoneNumbers.PhoneNumberUtil.GetInstance();

	public async Task<MessageResult> SendFromAsync(
		string from,
		string to,
		string message,
		SmsOptions? options = null,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(from);
		ArgumentException.ThrowIfNullOrWhiteSpace(to);
		ArgumentException.ThrowIfNullOrWhiteSpace(message);

		// Validate options - throw NotSupportedException for unsupported features
		ValidateSmsOptions(options);

		logger.LogSendingFromMessage(LogHeader, to, from, message.Length);

		// Check for cancellation before expensive API call
		cancellationToken.ThrowIfCancellationRequested();

		try {

			var sendOptions = new SmsSendOptions(enableDeliveryReport: true) {
				Tag = settings.Tag
			};

			var response = await client.SendAsync(
				from: from,
				to: to,
				message: message,
				options: sendOptions,
				cancellationToken: cancellationToken);

			return this.ProcessSendResult(response.Value, to);

		} catch (RequestFailedException ex) when (IsNonRetryableError(ex)) {
			// Don't retry hard faults we know will not recover
			logger.LogNonRetryableError(ex, to);
			return new MessageResult(to, false, ErrorMessage: ex.Message);
		} catch (Exception ex) {
			logger.LogErrorSendingMessage(LogHeader, ex);
			return new MessageResult(to, false, ErrorMessage: ex.Message);
		}
	}

	public Task<MessageResult> SendViaServiceAsync(
		string serviceId,
		string to,
		string message,
		SmsOptions? options = null,
		CancellationToken cancellationToken = default) {

		// Azure Communication Services does not support messaging services
		throw new NotSupportedException(
			"Azure Communication Services does not support messaging services. " +
			"Use SendFromAsync with a specific phone number instead.");
	}

	public async Task<MessageResponse> SendBulkAsync(
		string message,
		IEnumerable<string> phoneNumbers,
		string? from = null,
		string? serviceId = null,
		string countryCode = "US",
		bool validateOnly = false,
		SmsOptions? options = null,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(message);

		// Azure Communication Services does not support messaging services
		if (!string.IsNullOrWhiteSpace(serviceId)) {
			throw new NotSupportedException(
				"Azure Communication Services does not support messaging services. " +
				"Use the 'from' parameter with a specific phone number instead.");
		}

		var numbers = phoneNumbers?.ToList() ?? [];
		if (numbers.Count == 0) {
			throw new ArgumentException("Phone number list cannot be empty", nameof(phoneNumbers));
		}

		// Default to configuration if not provided
		from ??= settings.From;

		// Validate that we have a from number
		if (string.IsNullOrWhiteSpace(from)) {
			throw new InvalidOperationException("A 'from' phone number must be provided");
		}

		// Preemptively validate options and fail early
		ValidateSmsOptions(options);

		var results = new ConcurrentBag<MessageResult>();
		var sent = 0;
		var failed = 0;

		// Process phone numbers in parallel
		await Parallel.ForEachAsync(
			numbers,
			new ParallelOptions {
				MaxDegreeOfParallelism = settings.BulkOptions.MaxConcurrency,
				CancellationToken = cancellationToken
			},
			async (phoneNumber, token) => {
				try {
					// Parse and validate the phone number
					var parsedNumber = this.ParsePhoneNumber(phoneNumber, countryCode);
					if (parsedNumber == null) {
						var result = new MessageResult(
							phoneNumber,
							false,
							ErrorMessage: "Invalid phone number format");
						results.Add(result);
						Interlocked.Increment(ref failed);
						return;
					}

					// If validate only, just count it as sent
					if (validateOnly) {
						results.Add(new MessageResult(parsedNumber, true));
						Interlocked.Increment(ref sent);
						return;
					}

					// Send the message with retry
					var messageResult = await this.SendWithRetryAsync(
						() => this.SendFromAsync(from, parsedNumber, message, options, token),
						target: parsedNumber,
						maxRetries: settings.MaxRetries,
						ct: token);

					results.Add(messageResult);
					if (messageResult.Success) {
						Interlocked.Increment(ref sent);
					} else {
						Interlocked.Increment(ref failed);
					}

				} catch (Exception ex) {
					logger.LogErrorProcessingPhoneNumber(ex, phoneNumber);
					results.Add(new MessageResult(phoneNumber, false, ErrorMessage: ex.Message));
					Interlocked.Increment(ref failed);
				}
			});

		return new MessageResponse(sent, failed, [.. results]);
	}

	private async Task<MessageResult> SendWithRetryAsync(
		Func<Task<MessageResult>> sendFunc,
		string target,
		int maxRetries = 3,
		CancellationToken ct = default) {

		for (var attempt = 0; attempt <= maxRetries; attempt++) {
			try {
				return await sendFunc();
			} catch (RequestFailedException ex) when (ex.Status == 429 && attempt < maxRetries) {

				// Check for Retry-After header
				TimeSpan delay;
				if (ex.GetRawResponse()?.Headers.TryGetValue("Retry-After", out var retryAfterValue) == true
					&& int.TryParse(retryAfterValue, out var retryAfterSeconds)) {
					delay = TimeSpan.FromSeconds(retryAfterSeconds);
				} else {
					// Exponential backoff with decorrelated jitter
					var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, Math.Min(attempt, 6))); // cap growth
					var jitterMs = Random.Shared.Next(250, 1000);
					delay = baseDelay + TimeSpan.FromMilliseconds(jitterMs);
				}

				logger.LogRateLimitRetry(target, (int)delay.TotalMilliseconds, attempt + 1, maxRetries, ex.ErrorCode, ex.Status);

				await Task.Delay(delay, ct);
				continue;
			} catch (RequestFailedException ex) when (IsNonRetryableError(ex)) {
				// Don't retry hard faults
				logger.LogNonRetryableError(ex, target);
				return new MessageResult(target, false, ErrorMessage: ex.Message);
			} catch (Exception ex) {
				logger.LogNonRetryableError(ex, target);
				return new MessageResult(target, false, ErrorMessage: ex.Message);
			}
		}

		return new MessageResult(target, false, ErrorMessage: "Rate limit exceeded, all retries exhausted");
	}

	private static void ValidateSmsOptions(SmsOptions? options) {
		if (options == null) {
			return;
		}

		// Azure Communication Services does not support scheduled sending
		if (options.ScheduledSendTime.HasValue) {
			throw new NotSupportedException(
				"Azure Communication Services does not support scheduled SMS sending.");
		}

		// Azure Communication Services does not support MMS
		if (options.MediaUrls?.Any() == true) {
			throw new NotSupportedException(
				"Azure Communication Services does not support MMS (media URLs).");
		}

		// StatusCallbackUrl is not directly supported in Azure SMS
		// Azure uses delivery reports which are handled differently
		if (options.StatusCallbackUrl != null) {
			throw new NotSupportedException(
				"Azure Communication Services does not support custom status callback URLs. " +
				"Use Azure Event Grid for delivery reports instead.");
		}

		// ValidityPeriod is not supported in Azure SMS
		if (options.ValidityPeriod.HasValue) {
			throw new NotSupportedException(
				"Azure Communication Services does not support message validity period.");
		}
	}

	private static bool IsNonRetryableError(RequestFailedException ex) {
		// Non-retryable status codes
		return ex.Status switch {
			400 => true,  // Bad Request
			401 => true,  // Unauthorized
			403 => true,  // Forbidden
			404 => true,  // Not Found
			422 => true,  // Unprocessable Entity
			_ => false
		};
	}

	private MessageResult ProcessSendResult(SmsSendResult result, string to) {

		if (!result.Successful) {
			var errorCode = result.ErrorMessage ?? "Unknown error";
			var statusCode = result.HttpStatusCode.ToString();
			logger.LogFailedWithStatus(LogHeader, statusCode, errorCode);
			return new MessageResult(to, false, result.MessageId, errorCode);
		}

		logger.LogSuccess(LogHeader, result.MessageId);
		return new MessageResult(to, true, result.MessageId);
	}

	private string? ParsePhoneNumber(string phoneNumber, string countryCode) {
		try {
			var phone = this._phoneUtil.Parse(phoneNumber, countryCode);
			if (!this._phoneUtil.IsValidNumber(phone)) {
				logger.LogInvalidPhoneNumber(phoneNumber);
				return null;
			}

			return this._phoneUtil.Format(phone, PhoneNumbers.PhoneNumberFormat.E164);
		} catch (Exception ex) {
			logger.LogErrorParsingPhoneNumber(ex, phoneNumber);
			return null;
		}
	}

}
