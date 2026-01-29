using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfGame
{
    public class BigDoubleConverter : JsonConverter<BigDouble>
    {
        public override BigDouble Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject for BigDouble");
            }

            double mantissa = 0;
            long exponent = 0;

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    return new BigDouble(mantissa, exponent);
                }

                if (reader.TokenType == JsonTokenType.PropertyName) {
                    var prop = reader.GetString();
                    reader.Read();

                    switch (prop) {
                        case "m":
                        case "Mantissa":
                            mantissa = reader.GetDouble();
                            break;
                        case "e":
                        case "Exponent":
                            exponent = reader.GetInt64();
                            break;
                    }
                }
            }

            throw new JsonException("Expected EndObject for BigDouble");
        }

        public override void Write(Utf8JsonWriter writer, BigDouble value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteNumber("m", value.Mantissa);
            writer.WriteNumber("e", value.Exponent);
            writer.WriteEndObject();
        }
    }
}