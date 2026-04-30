using System.Globalization;
using System.Text;
using Confluent.Kafka;

namespace BreakfastProvider.Api.Events;

public static class KafkaHeadersExtensions
{
    public static Headers SetUtf8(this Headers headers, string key, object value)
    {
        var valueString = value switch
        {
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value?.ToString()
        };

        if (valueString is not null)
            headers.Add(key, Encoding.UTF8.GetBytes(valueString));
        else
            headers.Remove(key);

        return headers;
    }
}
