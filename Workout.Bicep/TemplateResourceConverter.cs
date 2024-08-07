using Newtonsoft.Json;

namespace Workout.Bicep;

public class TemplateResourceConverter : JsonConverter
{
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    {
        if (value != null)
        {
            if (!(value is TemplateResource[] array))
            {
                throw new JsonSerializationException($"resources property must be in type '{typeof(TemplateResource)}'");
            }

            if (string.IsNullOrWhiteSpace(array.FirstOrDefault()?.SymbolicName))
            {
                serializer.Serialize(writer, value);
                return;
            }

            writer.WriteStartObject();
            TemplateResource[] array2 = array;
            foreach (TemplateResource templateResource in array2)
            {
                writer.WritePropertyName(templateResource.SymbolicName);
                serializer.Serialize(writer, templateResource);
            }

            writer.WriteEndObject();
        }
        else
        {
            serializer.Serialize(writer, value);
        }
    }

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartArray:
#pragma warning disable CS8603 // Possible null reference return.
                return serializer.Deserialize<TemplateResource[]>(reader);
#pragma warning restore CS8603 // Possible null reference return.
            case JsonToken.StartObject:
                {
                    List<TemplateResource> list = new List<TemplateResource>();
                    int depth = reader.Depth;
                    while (reader.Depth != depth || reader.TokenType != JsonToken.EndObject)
                    {
                        reader.Read();
                        if (reader.TokenType == JsonToken.PropertyName)
                        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                            string symbolicName = (string)reader.Value;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                            reader.Read();
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                            TemplateResource templateResource = serializer.Deserialize<TemplateResource>(reader);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                            if (templateResource == null)
                            {
                                throw new JsonSerializationException($"Cannot deserialize the current JSON object in type {typeof(TemplateResource)}");
                            }

#pragma warning disable CS8601 // Possible null reference assignment.
                            templateResource.SymbolicName = symbolicName;
#pragma warning restore CS8601 // Possible null reference assignment.
                            list.Add(templateResource);
                        }
                    }

                    return list.ToArray();
                }
            default:
                throw new JsonSerializationException(string.Format("{0} encountered JSON token of type {1} but expected {2} or {3}", "TemplateResourceConverter", reader.TokenType, JsonToken.StartArray, JsonToken.StartObject));
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TemplateResource[]);
    }
}
