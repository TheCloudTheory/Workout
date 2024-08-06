using System;
using Newtonsoft.Json;

namespace Workout.Bicep;

public class TemplateResourceConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
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

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartArray:
                return serializer.Deserialize<TemplateResource[]>(reader);
            case JsonToken.StartObject:
                {
                    List<TemplateResource> list = new List<TemplateResource>();
                    int depth = reader.Depth;
                    while (reader.Depth != depth || reader.TokenType != JsonToken.EndObject)
                    {
                        reader.Read();
                        if (reader.TokenType == JsonToken.PropertyName)
                        {
                            string symbolicName = (string)reader.Value;
                            reader.Read();
                            TemplateResource templateResource = serializer.Deserialize<TemplateResource>(reader);
                            if (templateResource == null)
                            {
                                throw new JsonSerializationException($"Cannot deserialize the current JSON object in type {typeof(TemplateResource)}");
                            }

                            templateResource.SymbolicName = symbolicName;
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
