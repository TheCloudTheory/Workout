using System;
using System.Text.Json.Serialization;
using Azure.Deployments.Core.Converters;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Entities;
using Azure.Deployments.Core.Json;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Workout.Bicep;

public class TemplateResource : JTokenMetadata
{
    [JsonProperty(Required = Required.Default, Order = -53)]
    public TemplateGenericProperty<string> Extension { get; set; }

    [JsonProperty(Required = Required.Default, Order = -52)]
    public TemplateGenericProperty<string> Import { get; set; }

    [JsonProperty(Required = Required.Always, Order = -51)]
    public TemplateGenericProperty<string> Type { get; set; }

    [JsonProperty(Required = Required.Default, Order = -50)]
    public TemplateGenericProperty<string> ApiVersion { get; set; }

    [JsonProperty(Required = Required.Default, Order = -49)]
    public TemplateGenericProperty<string> Name { get; set; }

    [JsonProperty(Required = Required.Default, Order = -48)]
    public TemplateGenericProperty<bool> Existing { get; set; }

    [JsonProperty(Required = Required.Default, Order = -47)]
    public TemplateGenericProperty<string> Comments { get; set; }

    [JsonProperty(Required = Required.Default, Order = -46)]
    public TemplateGenericProperty<string> Location { get; set; }

    [JsonProperty(Required = Required.Default, Order = -46)]
    public TemplateGenericProperty<JToken> ExtendedLocation { get; set; }

    [JsonProperty(Required = Required.Default, Order = -45)]
    public TemplateGenericProperty<string>[] DependsOn { get; set; }

    [JsonProperty(Required = Required.Default, Order = -44)]
    public TemplateGenericProperty<JToken> Tags { get; set; }

    [JsonProperty(Required = Required.Default, Order = -43)]
    public TemplateGenericProperty<JToken> Sku { get; set; }

    [JsonProperty(Required = Required.Default, Order = -42)]
    public TemplateGenericProperty<string> Kind { get; set; }

    [JsonProperty(Required = Required.Default, Order = -41)]
    public TemplateGenericProperty<JToken> Zones { get; set; }

    [JsonProperty(Required = Required.Default, Order = -40)]
    public TemplateGenericProperty<JToken> Scale { get; set; }

    [JsonProperty(Required = Required.Default, Order = -39)]
    public TemplateGenericProperty<JToken> Identity { get; set; }

    [JsonProperty(Required = Required.Default)]
    public TemplateGenericProperty<string> Scope { get; set; }

    [JsonProperty(Required = Required.Default, Order = -38)]
    public TemplateGenericProperty<JToken> Plan { get; set; }

    [JsonProperty(Required = Required.Default, Order = -37)]
    public TemplateGenericProperty<JToken> Properties { get; set; }

    [JsonProperty(Required = Required.Default, Order = -36)]
    [Newtonsoft.Json.JsonConverter(typeof(TemplateResourceConverter))]
    public TemplateResource[] Resources { get; set; }

    public TemplateGenericProperty<string> ManagedBy { get; set; }

    public TemplateGenericProperty<JToken> ManagedByExtended { get; set; }

    [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Ignore)]
    public TemplateGenericProperty<JToken> Metadata { get; set; }

    [JsonProperty(Required = Required.Default)]
    public TemplateGenericProperty<string> Id { get; set; }

    [JsonProperty(Required = Required.Default)]
    public TemplateGenericProperty<string> SubscriptionId { get; set; }

    [JsonProperty(Required = Required.Default)]
    public TemplateGenericProperty<string> ResourceGroup { get; set; }

    [JsonProperty(Required = Required.Default)]
    public TemplateResourceCopy Copy { get; set; }

    [JsonProperty(Required = Required.Default)]
    public TemplateGenericProperty<JToken> Condition { get; set; }

    [JsonProperty(Required = Required.Default)]
    [Newtonsoft.Json.JsonConverter(typeof(AssertConverter))]
    public InsensitiveDictionary<TemplateGenericProperty<JToken>> Asserts { get; set; }

    [JsonProperty(Required = Required.Default, PropertyName = "__workout_id")]
    public TemplateGenericProperty<string> WorkoutResourceId { get; set; }

    public TemplateGenericProperty<JToken> Placement { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public string SymbolicName { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public string CopyLoopSymbolicName { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public TemplateCopyContext CopyContext { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public TemplateReference[] DependsOnReferences { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public TemplateReference[] PropertyReferences { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public string OriginalName { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public InsensitiveHashSet SkipEvaluationPaths { get; set; } = new InsensitiveHashSet();


    public bool IsConditionTrue()
    {
        if (Condition != null && Condition.Value != null)
        {
            return Condition.Value.ToObject<bool>();
        }

        return true;
    }

    public bool IsExistingResource()
    {
        if (Existing != null)
        {
            return Existing.Value;
        }

        return false;
    }

    public TemplateResource DeepCopy()
    {
        TemplateResource? templateResource = JsonConvert.DeserializeObject<TemplateResource>(JsonConvert.SerializeObject(this, SerializerSettings.SerializerMediaTypeSettingsWithIgnoredAttributes), SerializerSettings.SerializerMediaTypeSettingsWithIgnoredAttributes);
        templateResource.SymbolicName = SymbolicName;
        templateResource.CopyLoopSymbolicName = CopyLoopSymbolicName;
        return templateResource;
    }
}
