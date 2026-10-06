using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogsViewer.Models
{
    /// <summary>
    /// Cadena que se calcula la primera vez que se accede (y luego se cachea).
    /// Se usa para PropertiesJson: la serialización solo ocurre si alguien
    /// realmente lee el valor (la vista, para los logs paginados que se muestran).
    /// </summary>
    [JsonConverter(typeof(LazyStringJsonConverterFactory))]
    public class LazyString
    {
        private readonly Func<string?> _factory;
        private string? _value;
        private bool _resolved;

        public LazyString(Func<string?> factory) => _factory = factory;

        public string? Value
        {
            get
            {
                if (!_resolved)
                {
                    _value = _factory();
                    _resolved = true;
                }
                return _value;
            }
        }

        public static implicit operator string?(LazyString? lazy) => lazy?.Value;

        public override string? ToString() => Value;
    }

    /// <summary>
    /// Serializa LazyString como la cadena resuelta (o null), sin exponer sus campos internos.
    /// </summary>
    public sealed class LazyStringJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(LazyString);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => new LazyStringJsonConverter();

        private sealed class LazyStringJsonConverter : JsonConverter<LazyString>
        {
            public override LazyString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => reader.TokenType == JsonTokenType.Null ? null : new LazyString(() => reader.GetString());

            public override void Write(Utf8JsonWriter writer, LazyString value, JsonSerializerOptions options)
            {
                var str = value.Value;
                if (str is null) writer.WriteNullValue();
                else writer.WriteStringValue(str);
            }
        }
    }
}
