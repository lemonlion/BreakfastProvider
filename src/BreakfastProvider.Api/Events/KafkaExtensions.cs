using Confluent.Kafka;

namespace BreakfastProvider.Api.Events;

public static class KafkaExtensions
{
    public static ProducerBuilder<TKey, TValue> SetDiagnosticsHandlers<TKey, TValue>(this ProducerBuilder<TKey, TValue> builder, ILogger logger)
    {
        builder.SetLogHandler((_, message) =>
        {
            var logMessage = $"Kafka: {message.Level} {message.Facility} {message.Name} {message.Message}";
            var logLevel = message.Level switch
            {
                SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical => LogLevel.Critical,
                SyslogLevel.Error => LogLevel.Error,
                SyslogLevel.Warning => LogLevel.Warning,
                SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
                SyslogLevel.Debug => LogLevel.Debug,
                _ => LogLevel.Information
            };

            logger.Log(logLevel, "{Message}", logMessage);
        });

        builder.SetErrorHandler((_, error) =>
            logger.LogError("Kafka error: {Error}", error));

        builder.SetStatisticsHandler((_, statisticsJson) =>
            logger.LogInformation("Kafka statistics: {Statistics}", statisticsJson));

        return builder;
    }

    public static ConsumerBuilder<TKey, TValue> SetDiagnosticsHandlers<TKey, TValue>(this ConsumerBuilder<TKey, TValue> builder, ILogger logger)
    {
        builder.SetLogHandler((_, message) =>
        {
            var logMessage = $"Kafka: {message.Level} {message.Facility} {message.Name} {message.Message}";
            var logLevel = message.Level switch
            {
                SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical => LogLevel.Critical,
                SyslogLevel.Error => LogLevel.Error,
                SyslogLevel.Warning => LogLevel.Warning,
                SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
                SyslogLevel.Debug => LogLevel.Debug,
                _ => LogLevel.Information
            };

            logger.Log(logLevel, "{Message}", logMessage);
        });

        builder.SetErrorHandler((_, error) =>
            logger.LogError("Kafka error: {Error}", error));

        builder.SetStatisticsHandler((_, statisticsJson) =>
            logger.LogInformation("Kafka statistics: {Statistics}", statisticsJson));
        
        return builder;
    }
}
