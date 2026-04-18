using HMS.Service.Contracts;
using Microsoft.Extensions.Logging;

namespace HMS.LoggerService
{
    public class LoggerService : ILoggerService
    {
        private readonly ILogger<LoggerService> _logger;

        public LoggerService(ILogger<LoggerService> logger)
        {
            _logger = logger;
        }

        public void LogInfo(string message)  => _logger.LogInformation(message);
        public void LogWarn(string message)  => _logger.LogWarning(message);
        public void LogError(string message) => _logger.LogError(message);
        public void LogDebug(string message) => _logger.LogDebug(message);
    }
}
