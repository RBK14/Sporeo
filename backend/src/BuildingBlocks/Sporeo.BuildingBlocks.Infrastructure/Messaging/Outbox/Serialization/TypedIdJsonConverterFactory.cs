using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Serialization;

/// <summary>
/// JSON converter factory that serializes <see cref="TypedIdBase"/> subtypes as GUID strings.
/// Used by outbox (and other infrastructure serializers), not by HTTP API contracts.
/// </summary>
public sealed class TypedIdJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        !typeToConvert.IsAbstract
        && typeof(TypedIdBase).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(TypedIdJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class TypedIdJsonConverter<TId> : JsonConverter<TId>
        where TId : TypedIdBase
    {
        private static readonly Func<Guid, TId> Factory = CreateFactory();

        private static Func<Guid, TId> CreateFactory()
        {
            var method = typeof(TId).GetMethod(
                "FromValue",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(Guid)],
                modifiers: null)
                ?? throw new InvalidOperationException(
                    $"{typeof(TId).Name} must expose a public static FromValue(Guid) factory method.");

            return (Func<Guid, TId>)Delegate.CreateDelegate(typeof(Func<Guid, TId>), method);
        }

        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return Factory(reader.GetGuid());
            }

            // Compatibility with payloads serialized without this converter: { "value": "..." }
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var document = JsonDocument.ParseValue(ref reader);
                if (document.RootElement.TryGetProperty("value", out var valueProperty)
                    || document.RootElement.TryGetProperty("Value", out valueProperty))
                {
                    return Factory(valueProperty.GetGuid());
                }
            }

            throw new JsonException($"Expected a GUID string for {typeof(TId).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }
}
