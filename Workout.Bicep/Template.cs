using Azure.Deployments.Core.Configuration;
using Azure.Deployments.Core.Converters;
using Azure.Deployments.Core.Definitions;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Entities;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Workout.Bicep;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
public class Template : TemplatePropertySerializable
{
    private readonly Lazy<TemplateLanguageVersion> lazyParsedLanguageVersion;

    [JsonProperty(PropertyName = "$schema", Required = Required.Always)]
    public TemplateGenericProperty<string> Schema { get; set; }

    [JsonProperty(Required = Required.Default)]
    public TemplateGenericProperty<string> LanguageVersion { get; set; }

    [JsonProperty(Required = Required.Always)]
    public TemplateGenericProperty<string> ContentVersion { get; set; }

    [JsonProperty(Required = Required.Default)]
    public TemplateGenericProperty<string> ApiProfile { get; set; }

    [JsonProperty(Required = Required.Default)]
    public OrdinalDictionary<TemplateImport> Imports { get; set; }

    [JsonProperty(Required = Required.Default)]
    public OrdinalDictionary<TemplateExtension> Extensions { get; set; }

    public InsensitiveDictionary<TemplateTypeDefinition> Definitions { get; set; }

    [JsonProperty(Required = Required.Default)]
    public InsensitiveDictionary<TemplateInputParameter> Parameters { get; set; }

    [JsonProperty(Required = Required.Default)]
    public InsensitiveDictionary<TemplateGenericProperty<JToken>> Variables { get; set; }

    [JsonProperty(Required = Required.Default)]
    public TemplateFunctionNamespace[] Functions { get; set; }

    [JsonProperty(Required = Required.Always)]
    [JsonConverter(typeof(TemplateResourceConverter))]
    public TemplateResource[] Resources { get; set; }

    [JsonProperty(Required = Required.Default)]
    [JsonConverter(typeof(AssertConverter))]
    public InsensitiveDictionary<TemplateGenericProperty<JToken>> Asserts { get; set; }

    [JsonProperty(Required = Required.Default)]
    public InsensitiveDictionary<TemplateOutputParameter> Outputs { get; set; }

    [JsonProperty(Required = Required.Default)]
    public InsensitiveDictionary<TemplateGenericProperty<JToken>> Metadata { get; set; }

    [JsonIgnore]
    public TemplateReference[] OutputsReferences { get; set; }

    [JsonIgnore]
    public TemplateResource[] EmptyResourceCopies { get; set; }

    [JsonIgnore]
    public TemplateLanguageVersion ParsedLanguageVersion => lazyParsedLanguageVersion.Value;


    public Template()
    {
        lazyParsedLanguageVersion = new Lazy<TemplateLanguageVersion>(delegate
        {
            if (LanguageVersion == null)
            {
#pragma warning disable CS8603 // Possible null reference return.
                return null;
#pragma warning restore CS8603 // Possible null reference return.
            }

            TemplateLanguageVersion version;
#pragma warning disable CS8603 // Possible null reference return.
            return TemplateLanguageVersion.TryParse(LanguageVersion.Value, out version) ? version : null;
#pragma warning restore CS8603 // Possible null reference return.
        });
    }

    public bool SupportsLanguageFeature(TemplateLanguageFeature feature)
    {
        if (ParsedLanguageVersion != null)
        {
            return ParsedLanguageVersion.HasFeature(feature);
        }

        return false;
    }

    public bool HasSymbolicName()
    {
        return Resources.FirstOrDefault()?.SymbolicName != null;
    }

    public bool TryFindImportByAlias(string importAlias, out TemplateImport? import)
    {
        if (Imports == null)
        {
            import = null;
            return false;
        }

        return Imports.TryGetValue(importAlias, out import);
    }

    public TemplateImport FindImportByAlias(string importAlias)
    {
        if (!TryFindImportByAlias(importAlias, out var import))
        {
            throw new InvalidOperationException("Unable to find import with alias: " + importAlias + ".");
        }

        return import!;
    }

    public bool TryFindExtensionByAlias(string extensionAlias, out TemplateExtension? extension)
    {
        if (Extensions == null)
        {
            extension = null;
            return false;
        }

        return Extensions.TryGetValue(extensionAlias, out extension);
    }

    public TemplateExtension FindExtensionByAlias(string extensionAlias)
    {
        if (!TryFindExtensionByAlias(extensionAlias, out var extension))
        {
            throw new InvalidOperationException("Unable to find provider with alias: " + extensionAlias + ".");
        }

        return extension!;
    }

    public bool MayHaveProviderRequiringOAuthOboFlowImported(bool searchNestedTemplates = true)
    {
        if (Extensions != null && Extensions.Values.Any(TemplateExtensionFacts.RequiresOAuthOboFlow))
        {
            return true;
        }

        if (Imports != null && Imports.Values.Any(TemplateExtensionFacts.RequiresOAuthOboFlow))
        {
            return true;
        }

        if (searchNestedTemplates)
        {
            return MayHaveProviderRequiringOAuthOboFlowImportedInNestedTemplates();
        }

        return false;
    }

    public bool MayHaveProviderRequiringOAuthOboFlowImportedInNestedTemplates()
    {
        foreach (TemplateResource item in EnumerateAllResources())
        {
            if (item.Type.Value.EqualsOrdinalInsensitively("Microsoft.Resources/deployments") && item.Properties != null && item.Properties.Value != null && item.Properties.Value.SelectTokens("$..imports.*.provider").Concat(item.Properties.Value.SelectTokens("$..extensions.*.name")).Any((JToken x) => x.Type == JTokenType.String && TemplateExtensionFacts.RequiresOAuthOboFlow(x.Value<string>())))
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<TemplateResource> EnumerateAllResources()
    {
        Stack<TemplateResource> resourceStack = new Stack<TemplateResource>();
        foreach (TemplateResource item in Resources.CoalesceEnumerable().Reverse())
        {
            if (item != null)
            {
                resourceStack.Push(item);
            }
        }

        while (resourceStack.Any())
        {
            TemplateResource current = resourceStack.Pop();
            yield return current;
            foreach (TemplateResource item2 in current.Resources.CoalesceEnumerable().Reverse())
            {
                if (item2 != null)
                {
                    resourceStack.Push(item2);
                }
            }
        }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.