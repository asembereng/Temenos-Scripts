using System.Net.Mail;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Enums;
using TemenosAlertManager.Core.Interfaces;

namespace TemenosAlertManager.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<EmailService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            var smtpSettings = GetSmtpSettings();
            
            using var smtpClient = new SmtpClient(smtpSettings.Host, smtpSettings.Port)
            {
                EnableSsl = smtpSettings.EnableSsl,
                Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password)
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings.FromAddress, smtpSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                Priority = MailPriority.Normal
            };

            mailMessage.To.Add(recipient);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            
            _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            return false;
        }
    }

    public async Task<bool> SendAlertEmailAsync(Alert alert, string recipient, CancellationToken cancellationToken = default)
    {
        var subject = GenerateAlertSubject(alert);
        var body = GenerateAlertBody(alert);
        
        return await SendEmailAsync(recipient, subject, body, cancellationToken);
    }

    private SmtpSettings GetSmtpSettings()
    {
        return new SmtpSettings
        {
            Host = _configuration["Email:Smtp:Host"] ?? "localhost",
            Port = int.Parse(_configuration["Email:Smtp:Port"] ?? "25"),
            EnableSsl = bool.Parse(_configuration["Email:Smtp:EnableSsl"] ?? "false"),
            Username = _configuration["Email:Smtp:Username"] ?? "",
            Password = _configuration["Email:Smtp:Password"] ?? "",
            FromAddress = _configuration["Email:FromAddress"] ?? "noreply@temenosalerts.local",
            FromName = _configuration["Email:FromName"] ?? "Temenos Alert Manager"
        };
    }

    private static string GenerateAlertSubject(Alert alert)
    {
        var severityPrefix = alert.Severity switch
        {
            AlertSeverity.Critical => "[CRITICAL]",
            AlertSeverity.Warning => "[WARNING]",
            _ => "[INFO]"
        };

        return $"{severityPrefix}[{alert.Domain}] {alert.Title}";
    }

    private string GenerateAlertBody(Alert alert)
    {
        var dashboardUrl = _configuration["Application:DashboardUrl"] ?? "http://localhost:5000";
        var alertUrl = $"{dashboardUrl}/alerts/{alert.Id}";
        var acknowledgeUrl = $"{dashboardUrl}/alerts/{alert.Id}/acknowledge";

        var bodyBuilder = new StringBuilder();
        bodyBuilder.AppendLine("<!DOCTYPE html>");
        bodyBuilder.AppendLine("<html>");
        bodyBuilder.AppendLine("<head>");
        bodyBuilder.AppendLine("<style>");
        bodyBuilder.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        bodyBuilder.AppendLine(".alert-header { background-color: " + GetSeverityColor(alert.Severity) + "; color: white; padding: 10px; border-radius: 5px; }");
        bodyBuilder.AppendLine(".alert-content { margin: 20px 0; }");
        bodyBuilder.AppendLine(".alert-details { background-color: #f5f5f5; padding: 10px; border-radius: 5px; }");
        bodyBuilder.AppendLine(".button { display: inline-block; padding: 10px 20px; margin: 5px; background-color: #007cba; color: white; text-decoration: none; border-radius: 5px; }");
        bodyBuilder.AppendLine("</style>");
        bodyBuilder.AppendLine("</head>");
        bodyBuilder.AppendLine("<body>");
        
        bodyBuilder.AppendLine($"<div class='alert-header'>");
        bodyBuilder.AppendLine($"<h2>{alert.Title}</h2>");
        bodyBuilder.AppendLine($"<p>Severity: {alert.Severity} | Domain: {alert.Domain} | Source: {alert.Source}</p>");
        bodyBuilder.AppendLine("</div>");
        
        bodyBuilder.AppendLine("<div class='alert-content'>");
        bodyBuilder.AppendLine($"<p><strong>Description:</strong></p>");
        bodyBuilder.AppendLine($"<p>{alert.Description.Replace("\n", "<br/>")}</p>");
        
        if (!string.IsNullOrEmpty(alert.MetricValue))
        {
            bodyBuilder.AppendLine($"<p><strong>Current Value:</strong> {alert.MetricValue}</p>");
        }
        
        if (!string.IsNullOrEmpty(alert.Threshold))
        {
            bodyBuilder.AppendLine($"<p><strong>Threshold:</strong> {alert.Threshold}</p>");
        }
        
        bodyBuilder.AppendLine($"<p><strong>Time:</strong> {alert.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");
        bodyBuilder.AppendLine("</div>");
        
        bodyBuilder.AppendLine("<div class='alert-details'>");
        bodyBuilder.AppendLine("<h3>Actions</h3>");
        bodyBuilder.AppendLine($"<a href='{alertUrl}' class='button'>View Alert Details</a>");
        bodyBuilder.AppendLine($"<a href='{acknowledgeUrl}' class='button'>Acknowledge Alert</a>");
        bodyBuilder.AppendLine("</div>");
        
        bodyBuilder.AppendLine("<p><small>This is an automated message from Temenos Alert Manager.</small></p>");
        bodyBuilder.AppendLine("</body>");
        bodyBuilder.AppendLine("</html>");

        return bodyBuilder.ToString();
    }

    private static string GetSeverityColor(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Critical => "#dc3545", // Red
            AlertSeverity.Warning => "#fd7e14",  // Orange
            _ => "#17a2b8"                       // Blue
        };
    }

    private class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }
}