using TemenosAlertManager.Core.Enums;
using TemenosAlertManager.Core.Interfaces;

namespace TemenosAlertManager.Api.BackgroundServices;

public class EmailOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EmailOutboxWorker> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(1); // Check every minute

    public EmailOutboxWorker(IServiceScopeFactory serviceScopeFactory, ILogger<EmailOutboxWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Outbox Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEmailsAsync(stoppingToken);
                await ProcessRetryableEmailsAsync(stoppingToken);
                
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Email Outbox Worker cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Email Outbox Worker");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes on error
            }
        }

        _logger.LogInformation("Email Outbox Worker stopped");
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            var pendingEmails = await unitOfWork.AlertOutbox.GetPendingAlertsAsync(cancellationToken: cancellationToken);
            
            foreach (var emailOutbox in pendingEmails)
            {
                try
                {
                    _logger.LogDebug("Processing pending email {EmailId} for alert {AlertId}", 
                        emailOutbox.Id, emailOutbox.AlertId);

                    emailOutbox.Status = AlertDeliveryStatus.Retrying;
                    emailOutbox.Attempts++;
                    await unitOfWork.AlertOutbox.UpdateAsync(emailOutbox, cancellationToken);

                    bool success;
                    if (emailOutbox.Channel == AlertChannel.Email)
                    {
                        success = await emailService.SendEmailAsync(
                            emailOutbox.Recipient, 
                            emailOutbox.Subject, 
                            emailOutbox.Payload, 
                            cancellationToken);
                    }
                    else
                    {
                        // For future implementation of Slack/Teams
                        success = await SendToAlternativeChannelAsync(emailOutbox, cancellationToken);
                    }

                    if (success)
                    {
                        emailOutbox.Status = AlertDeliveryStatus.Sent;
                        emailOutbox.DeliveredAt = DateTime.UtcNow;
                        _logger.LogInformation("Successfully sent email {EmailId} for alert {AlertId}", 
                            emailOutbox.Id, emailOutbox.AlertId);
                    }
                    else
                    {
                        await HandleDeliveryFailure(emailOutbox, "Email delivery failed", cancellationToken);
                    }

                    await unitOfWork.AlertOutbox.UpdateAsync(emailOutbox, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email {EmailId} for alert {AlertId}", 
                        emailOutbox.Id, emailOutbox.AlertId);
                    
                    await HandleDeliveryFailure(emailOutbox, ex.Message, cancellationToken);
                    await unitOfWork.AlertOutbox.UpdateAsync(emailOutbox, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending emails");
        }
    }

    private async Task ProcessRetryableEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            var retryableEmails = await unitOfWork.AlertOutbox.GetRetryableAlertsAsync(cancellationToken);
            
            foreach (var emailOutbox in retryableEmails)
            {
                try
                {
                    _logger.LogDebug("Retrying email {EmailId} for alert {AlertId} (attempt {Attempt})", 
                        emailOutbox.Id, emailOutbox.AlertId, emailOutbox.Attempts + 1);

                    emailOutbox.Attempts++;

                    bool success;
                    if (emailOutbox.Channel == AlertChannel.Email)
                    {
                        success = await emailService.SendEmailAsync(
                            emailOutbox.Recipient, 
                            emailOutbox.Subject, 
                            emailOutbox.Payload, 
                            cancellationToken);
                    }
                    else
                    {
                        success = await SendToAlternativeChannelAsync(emailOutbox, cancellationToken);
                    }

                    if (success)
                    {
                        emailOutbox.Status = AlertDeliveryStatus.Sent;
                        emailOutbox.DeliveredAt = DateTime.UtcNow;
                        emailOutbox.NextRetryAt = null;
                        _logger.LogInformation("Successfully sent email {EmailId} for alert {AlertId} on retry", 
                            emailOutbox.Id, emailOutbox.AlertId);
                    }
                    else
                    {
                        await HandleDeliveryFailure(emailOutbox, "Email retry failed", cancellationToken);
                    }

                    await unitOfWork.AlertOutbox.UpdateAsync(emailOutbox, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrying email {EmailId} for alert {AlertId}", 
                        emailOutbox.Id, emailOutbox.AlertId);
                    
                    await HandleDeliveryFailure(emailOutbox, ex.Message, cancellationToken);
                    await unitOfWork.AlertOutbox.UpdateAsync(emailOutbox, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing retryable emails");
        }
    }

    private async Task HandleDeliveryFailure(TemenosAlertManager.Core.Entities.AlertOutbox emailOutbox, string errorMessage, CancellationToken cancellationToken)
    {
        emailOutbox.ErrorMessage = errorMessage;

        if (emailOutbox.Attempts >= emailOutbox.MaxAttempts)
        {
            emailOutbox.Status = AlertDeliveryStatus.Failed;
            emailOutbox.NextRetryAt = null;
            
            _logger.LogWarning("Email {EmailId} for alert {AlertId} failed permanently after {Attempts} attempts", 
                emailOutbox.Id, emailOutbox.AlertId, emailOutbox.Attempts);

            // TODO: Implement fallback to secondary channel (Slack/Teams)
            await CreateFallbackNotificationAsync(emailOutbox, cancellationToken);
        }
        else
        {
            // Calculate exponential backoff: 1min, 5min, 15min, 60min, 240min
            var delayMinutes = Math.Pow(2, emailOutbox.Attempts) * 1;
            if (delayMinutes > 240) delayMinutes = 240; // Cap at 4 hours
            
            emailOutbox.Status = AlertDeliveryStatus.Retrying;
            emailOutbox.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
            
            _logger.LogInformation("Scheduling email {EmailId} for retry in {DelayMinutes} minutes (attempt {Attempt})", 
                emailOutbox.Id, delayMinutes, emailOutbox.Attempts);
        }
    }

    private async Task<bool> SendToAlternativeChannelAsync(TemenosAlertManager.Core.Entities.AlertOutbox emailOutbox, CancellationToken cancellationToken)
    {
        // Placeholder for Slack/Teams integration
        _logger.LogInformation("Alternative channel delivery not yet implemented for {Channel}", emailOutbox.Channel);
        return false;
    }

    private async Task CreateFallbackNotificationAsync(TemenosAlertManager.Core.Entities.AlertOutbox failedEmail, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Create a fallback notification for administrators
            var fallbackNotification = new TemenosAlertManager.Core.Entities.AlertOutbox
            {
                AlertId = failedEmail.AlertId,
                Channel = AlertChannel.Email, // For now, try email to admin
                Recipient = "admin@company.local", // TODO: Get from configuration
                Subject = $"ALERT DELIVERY FAILURE - Original Alert {failedEmail.AlertId}",
                Payload = $"Failed to deliver alert {failedEmail.AlertId} to {failedEmail.Recipient} after {failedEmail.Attempts} attempts. Error: {failedEmail.ErrorMessage}",
                Status = AlertDeliveryStatus.Pending,
                Attempts = 0,
                MaxAttempts = 3 // Fewer attempts for fallback notifications
            };

            await unitOfWork.AlertOutbox.AddAsync(fallbackNotification, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Created fallback notification {FallbackId} for failed email {EmailId}", 
                fallbackNotification.Id, failedEmail.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create fallback notification for email {EmailId}", failedEmail.Id);
        }
    }
}