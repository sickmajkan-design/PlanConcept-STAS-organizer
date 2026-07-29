using Construction.Application.Common.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Construction.Infrastructure.Notifications;

/// <summary>
/// Firebase Cloud Messaging implementation of <see cref="IPushSender"/>.
/// When no service-account credentials are configured (local development),
/// pushes are logged instead of sent so flows stay fully testable.
/// FCM multicast is limited to 500 tokens per call, so batches are chunked.
/// </summary>
public class FcmPushSender : IPushSender
{
    private const int FcmMulticastLimit = 500;

    private readonly FirebaseSettings _settings;
    private readonly ILogger<FcmPushSender> _logger;
    private readonly Lazy<FirebaseApp?> _app;

    public FcmPushSender(IOptions<FirebaseSettings> settings, ILogger<FcmPushSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _app = new Lazy<FirebaseApp?>(CreateApp, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<PushSendResult> SendAsync(
        IReadOnlyList<string> deviceTokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        if (deviceTokens.Count == 0)
        {
            return PushSendResult.Empty;
        }

        if (_app.Value is null)
        {
            _logger.LogWarning(
                "Firebase is not configured; push '{Title}' to {Count} device(s) was not sent.",
                title, deviceTokens.Count);
            return PushSendResult.Empty;
        }

        var messaging = FirebaseMessaging.GetMessaging(_app.Value);

        var successCount = 0;
        var failureCount = 0;
        var invalidTokens = new List<string>();

        foreach (var chunk in deviceTokens.Chunk(FcmMulticastLimit))
        {
            var messages = chunk
                .Select(token => new Message
                {
                    Token = token,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = new Dictionary<string, string>(data)
                })
                .ToList();

            var response = await messaging.SendEachAsync(messages, cancellationToken);

            successCount += response.SuccessCount;
            failureCount += response.FailureCount;

            for (var i = 0; i < response.Responses.Count; i++)
            {
                var send = response.Responses[i];

                if (send.IsSuccess)
                {
                    continue;
                }

                if (send.Exception?.MessagingErrorCode
                        is MessagingErrorCode.Unregistered
                        or MessagingErrorCode.InvalidArgument)
                {
                    invalidTokens.Add(chunk[i]);
                }
            }
        }

        _logger.LogInformation(
            "FCM push '{Title}': {Success} delivered, {Failure} failed, {Invalid} invalid token(s)",
            title, successCount, failureCount, invalidTokens.Count);

        return new PushSendResult(successCount, failureCount, invalidTokens);
    }

    private FirebaseApp? CreateApp()
    {
        if (!_settings.IsConfigured)
        {
            return null;
        }

        try
        {
            var credential = !string.IsNullOrWhiteSpace(_settings.CredentialsJson)
                ? CredentialFactory.FromJson<ServiceAccountCredential>(_settings.CredentialsJson)
                    .ToGoogleCredential()
                : CredentialFactory.FromFile<ServiceAccountCredential>(_settings.CredentialsPath!)
                    .ToGoogleCredential();

            return FirebaseApp.DefaultInstance
                   ?? FirebaseApp.Create(new AppOptions { Credential = credential });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase; pushes will not be sent.");
            return null;
        }
    }
}
