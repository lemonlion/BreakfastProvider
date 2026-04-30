using System.ComponentModel;
using System.Reflection;
using Bielu.AspNetCore.AsyncApi.Transformers;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Events;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Filters;

internal class AsyncApiDynamicConfigurationProviderFilter(
    IOptions<KafkaConfig> kafkaSettings,
    IOptions<PubSubConfig> pubSubSettings) : IAsyncApiDocumentTransformer
{
    private readonly KafkaConfig _kafkaSettings = kafkaSettings.Value;
    private readonly PubSubConfig _pubSubSettings = pubSubSettings.Value;

    public Task TransformAsync(AsyncApiDocument document, AsyncApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        PopulateChannels(document);
        RewriteKafkaTopicName(document);
        RewritePubSubTopicName(document);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Discovers all IKafkaEvent implementations and populates channels, messages, schemas, and operations.
    /// Required because the beta version of Bielu.AspNetCore.AsyncApi uses ApiDescription-based
    /// scanning which does not discover non-controller classes decorated with AsyncAPI attributes.
    /// </summary>
    private static void PopulateChannels(AsyncApiDocument document)
    {
        if (document.Channels.Count > 0)
            return;

        document.Components ??= new AsyncApiComponents();
        document.Components.Schemas ??= new Dictionary<string, AsyncApiMultiFormatSchema>();
        document.Components.Messages ??= new Dictionary<string, AsyncApiMessage>();

        // Register shared message headers schema once
        const string headersSchemaId = "messageHeaders";
        if (!document.Components.Schemas.ContainsKey(headersSchemaId))
        {
            document.Components.Schemas[headersSchemaId] = new AsyncApiMultiFormatSchema
            {
                Schema = BuildSchemaFromType(typeof(MessageHeaders), headersSchemaId)
            };
        }

        // Discover all IKafkaEvent types in the same assembly
        var kafkaEventTypes = typeof(IKafkaEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IKafkaEvent).IsAssignableFrom(t));

        foreach (var eventType in kafkaEventTypes)
        {
            var meta = IKafkaEvent.GetAsyncApiMetadata(eventType);
            RegisterEvent(document, eventType, meta, headersSchemaId);
        }

        // Discover all IPubSubEvent types in the same assembly
        var pubSubEventTypes = typeof(IPubSubEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IPubSubEvent).IsAssignableFrom(t));

        foreach (var eventType in pubSubEventTypes)
        {
            var meta = IPubSubEvent.GetAsyncApiMetadata(eventType);
            RegisterPubSubEvent(document, eventType, meta);
        }
    }

    private static void RegisterEvent(AsyncApiDocument document, Type eventType, KafkaEventAsyncApiMetadata meta, string headersSchemaId)
    {
        // Schema
        document.Components!.Schemas![meta.SchemaId] = new AsyncApiMultiFormatSchema
        {
            Schema = BuildSchemaFromType(eventType, meta.SchemaId)
        };

        // Message
        var message = new AsyncApiMessage
        {
            Name = meta.MessageId,
            Title = meta.MessageTitle,
            Summary = meta.MessageSummary,
            Payload = new AsyncApiJsonSchemaReference($"#/components/schemas/{meta.SchemaId}"),
            Headers = new AsyncApiJsonSchemaReference($"#/components/schemas/{headersSchemaId}")
        };
        document.Components.Messages![meta.MessageId] = message;

        // Channel
        var channel = new AsyncApiChannel
        {
            Address = meta.ChannelName,
            Description = meta.ChannelDescription,
            Messages = new Dictionary<string, AsyncApiMessage>
            {
                [meta.MessageId] = new AsyncApiMessageReference($"#/components/messages/{meta.MessageId}")
            }
        };
        document.Channels[meta.ChannelName] = channel;

        // Operation
        var operation = new AsyncApiOperation
        {
            Title = meta.OperationId,
            Summary = meta.OperationSummary,
            Description = meta.OperationDescription,
            Action = AsyncApiAction.Send,
            Channel = new AsyncApiChannelReference($"#/channels/{meta.ChannelName}"),
            Messages = [new AsyncApiMessageReference($"#/channels/{meta.ChannelName}/messages/{meta.MessageId}")]
        };
        document.Operations[meta.OperationId] = operation;
    }

    private static void RegisterPubSubEvent(AsyncApiDocument document, Type eventType, PubSubEventAsyncApiMetadata meta)
    {
        // Schema
        document.Components!.Schemas![meta.SchemaId] = new AsyncApiMultiFormatSchema
        {
            Schema = BuildSchemaFromType(eventType, meta.SchemaId)
        };

        // Message (Pub/Sub messages use CloudEvents attributes, not Kafka headers)
        var message = new AsyncApiMessage
        {
            Name = meta.MessageId,
            Title = meta.MessageTitle,
            Summary = meta.MessageSummary,
            Payload = new AsyncApiJsonSchemaReference($"#/components/schemas/{meta.SchemaId}")
        };
        document.Components.Messages![meta.MessageId] = message;

        // Channel
        var channel = new AsyncApiChannel
        {
            Address = meta.ChannelName,
            Description = meta.ChannelDescription,
            Messages = new Dictionary<string, AsyncApiMessage>
            {
                [meta.MessageId] = new AsyncApiMessageReference($"#/components/messages/{meta.MessageId}")
            }
        };
        document.Channels[meta.ChannelName] = channel;

        // Operation
        var operation = new AsyncApiOperation
        {
            Title = meta.OperationId,
            Summary = meta.OperationSummary,
            Description = meta.OperationDescription,
            Action = AsyncApiAction.Send,
            Channel = new AsyncApiChannelReference($"#/channels/{meta.ChannelName}"),
            Messages = [new AsyncApiMessageReference($"#/channels/{meta.ChannelName}/messages/{meta.MessageId}")]
        };
        document.Operations[meta.OperationId] = operation;
    }

    /// <summary>
    /// Builds a JSON schema from a CLR type by reflecting over its public instance properties.
    /// Uses [JsonPropertyName] for the property key and [Description] for the description.
    /// </summary>
    private static AsyncApiJsonSchema BuildSchemaFromType(Type type, string schemaId)
    {
        var typeDescription = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
        var properties = new Dictionary<string, AsyncApiJsonSchema>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var (jsonName, description, schemaType, format, isArray) = IKafkaEvent.GetPropertySchema(prop);

            if (isArray)
            {
                properties[jsonName] = new AsyncApiJsonSchema
                {
                    Type = SchemaType.Array,
                    Items = new AsyncApiJsonSchema { Type = ParseSchemaType(schemaType) },
                    Description = description
                };
            }
            else
            {
                var schema = new AsyncApiJsonSchema { Type = ParseSchemaType(schemaType), Description = description };
                if (format is not null) schema.Format = format;
                properties[jsonName] = schema;
            }
        }

        return new AsyncApiJsonSchema
        {
            Type = SchemaType.Object,
            Description = typeDescription,
            Properties = properties,
            Extensions = new Dictionary<string, IAsyncApiExtension>
            {
                ["additionalProperties"] = new AsyncApiAny(false)
            }
        };
    }

    private static SchemaType ParseSchemaType(string type) => type switch
    {
        "string" => SchemaType.String,
        "integer" => SchemaType.Integer,
        "number" => SchemaType.Number,
        "boolean" => SchemaType.Boolean,
        "array" => SchemaType.Array,
        "object" => SchemaType.Object,
        _ => SchemaType.String
    };

    /// <summary>
    /// Rewrites Kafka channel names from compile-time defaults to runtime topic names.
    /// Also updates operation channel/message references to match the new channel keys.
    /// </summary>
    private void RewriteKafkaTopicName(AsyncApiDocument document)
    {
        var kafkaChannelKeys = _kafkaSettings.ProducerConfigurations.Keys.ToHashSet();

        foreach (var channel in document.Channels.ToArray())
        {
            if (!kafkaChannelKeys.Contains(channel.Key))
                continue;

            var topicConfiguration = _kafkaSettings.ProducerConfigurations[channel.Key];

            var oldKey = channel.Key;
            var newKey = topicConfiguration.TopicName;

            document.Channels.Remove(oldKey);
            channel.Value.Address = newKey;
            document.Channels.Add(newKey, channel.Value);

            // Update operation references that point to the old channel key
            foreach (var operation in document.Operations.Values)
            {
                if (operation.Channel is AsyncApiChannelReference channelRef &&
                    channelRef.Reference?.Reference == $"#/channels/{oldKey}")
                {
                    operation.Channel = new AsyncApiChannelReference($"#/channels/{newKey}");
                }

                if (operation.Messages is not null)
                {
                    for (var i = 0; i < operation.Messages.Count; i++)
                    {
                        if (operation.Messages[i] is AsyncApiMessageReference msgRef &&
                            msgRef.Reference?.Reference is not null &&
                            msgRef.Reference.Reference.StartsWith($"#/channels/{oldKey}/"))
                        {
                            var suffix = msgRef.Reference.Reference[$"#/channels/{oldKey}/".Length..];
                            operation.Messages[i] = new AsyncApiMessageReference($"#/channels/{newKey}/{suffix}");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Rewrites Pub/Sub channel names from compile-time event type names to runtime topic IDs.
    /// </summary>
    private void RewritePubSubTopicName(AsyncApiDocument document)
    {
        var pubSubChannelKeys = _pubSubSettings.PublisherConfigurations.Keys.ToHashSet();

        foreach (var channel in document.Channels.ToArray())
        {
            if (!pubSubChannelKeys.Contains(channel.Key))
                continue;

            var topicConfig = _pubSubSettings.PublisherConfigurations[channel.Key];

            var oldKey = channel.Key;
            var newKey = topicConfig.TopicId;

            document.Channels.Remove(oldKey);
            channel.Value.Address = newKey;
            document.Channels.Add(newKey, channel.Value);

            foreach (var operation in document.Operations.Values)
            {
                if (operation.Channel is AsyncApiChannelReference channelRef &&
                    channelRef.Reference?.Reference == $"#/channels/{oldKey}")
                {
                    operation.Channel = new AsyncApiChannelReference($"#/channels/{newKey}");
                }

                if (operation.Messages is not null)
                {
                    for (var i = 0; i < operation.Messages.Count; i++)
                    {
                        if (operation.Messages[i] is AsyncApiMessageReference msgRef &&
                            msgRef.Reference?.Reference is not null &&
                            msgRef.Reference.Reference.StartsWith($"#/channels/{oldKey}/"))
                        {
                            var suffix = msgRef.Reference.Reference[$"#/channels/{oldKey}/".Length..];
                            operation.Messages[i] = new AsyncApiMessageReference($"#/channels/{newKey}/{suffix}");
                        }
                    }
                }
            }
        }
    }
}
