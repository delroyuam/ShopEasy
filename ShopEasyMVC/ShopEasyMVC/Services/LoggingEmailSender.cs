namespace ShopEasyMVC.Services
    {
    // ponytail: no SMTP configured; logs instead of sending. Swap in a real
    // IEmailSender (e.g. MailKit) once SMTP credentials exist.
    public class LoggingEmailSender : IEmailSender
        {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
            {
            _logger = logger;
            }

        public Task SendAsync(string to, string subject, string body)
            {
            _logger.LogInformation("Email a {To} - {Subject}: {Body}", to, subject, body);
            return Task.CompletedTask;
            }
        }
    }
