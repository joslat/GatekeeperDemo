// SPDX-License-Identifier: MIT
// Copyright (c) 2026 AgentEval Contributors

using AgentEval.PartnerDeskDemo.Providers;
using Xunit;

namespace AgentEval.PartnerDeskDemo.Tests;

/// <summary>
/// The provider resolver, exercised through its variable-reader delegate.
/// </summary>
/// <remarks>
/// Every test here passes a dictionary rather than touching the process environment. Environment variables are
/// global mutable state: a suite that sets them interferes with itself, and an ambient <c>OPENAI_API_KEY</c> on a
/// developer's machine would silently configure a provider a test believes is absent.
/// </remarks>
public sealed class InferenceProviderTests
{
    private static Func<string, string?> Env(params (string Name, string Value)[] variables)
    {
        var map = variables.ToDictionary(v => v.Name, v => v.Value, StringComparer.Ordinal);
        return name => map.TryGetValue(name, out var value) ? value : null;
    }

    [Fact]
    public void BitdeerNeedsOnlyItsKey_AndDefaultsToTheNamedModel()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(("BITDEER_API_KEY", "k")));

        Assert.True(settings.IsConfigured);
        Assert.Equal(InferenceProvider.Bitdeer, settings.Provider);
        Assert.Equal("bitdeer", settings.ProviderTag);
        Assert.Equal(InferenceProviderEnvironment.BitdeerDefaultModel, settings.Model);
        Assert.Equal(
            new Uri(InferenceProviderEnvironment.BitdeerDefaultEndpoint),
            settings.Endpoint);
        Assert.False(settings.UsesAzureProtocol);
        Assert.Equal($"{InferenceProviderEnvironment.BitdeerDefaultModel}@bitdeer", settings.ModelIdentity);
    }

    [Fact]
    public void ExplicitSelectorBeatsAnotherProviderWithCredentials()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AI_INFERENCE_PROVIDER", "bitdeer"),
            ("AZURE_OPENAI_ENDPOINT", "https://example.openai.azure.com/"),
            ("AZURE_OPENAI_API_KEY", "azure-key"),
            ("AZURE_OPENAI_DEPLOYMENT", "gpt-5.5"),
            ("BITDEER_API_KEY", "bitdeer-key")));

        Assert.Equal(InferenceProvider.Bitdeer, settings.Provider);
        Assert.Equal(InferenceProviderSelection.Explicit, settings.Selection);
    }

    [Fact]
    public void ExplicitProviderWithoutItsVariables_FailsClosedAndNamesOnlyWhatIsMissing()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AI_INFERENCE_PROVIDER", "foundry"),
            ("FOUNDRY_ENDPOINT", "https://example.services.ai.azure.com/"),
            // Another host is fully configured; choosing it anyway would run against a model nobody asked for.
            ("BITDEER_API_KEY", "bitdeer-key")));

        Assert.False(settings.IsConfigured);
        Assert.Contains("FOUNDRY_API_KEY", settings.Diagnostic, StringComparison.Ordinal);
        Assert.Contains("FOUNDRY_MODEL", settings.Diagnostic, StringComparison.Ordinal);
        Assert.DoesNotContain("FOUNDRY_ENDPOINT is missing", settings.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownSelectorValue_FailsClosedAndListsTheAcceptedOnes()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AI_INFERENCE_PROVIDER", "anthropic"),
            ("BITDEER_API_KEY", "bitdeer-key")));

        Assert.False(settings.IsConfigured);
        Assert.Contains("bitdeer", settings.Diagnostic, StringComparison.Ordinal);
        Assert.Contains("openai-compatible", settings.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void BitdeerIsDetectedFirst_SoStaleAzureVariablesCannotWin()
    {
        // The demo's Azure resource was retired and its endpoint no longer resolves, but the variables stayed set
        // in the maintainer's environment and kept winning detection, putting a dead deployment on the dropdown.
        // Detecting a host nobody can reach ahead of the one that works is the failure this order prevents.
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AZURE_OPENAI_ENDPOINT", "https://retired.openai.azure.com/"),
            ("AZURE_OPENAI_API_KEY", "azure-key"),
            ("AZURE_OPENAI_DEPLOYMENT", "gpt-5.5"),
            ("BITDEER_API_KEY", "bitdeer-key")));

        Assert.Equal(InferenceProvider.Bitdeer, settings.Provider);
        Assert.Equal(InferenceProviderSelection.AutoDetected, settings.Selection);
        Assert.False(settings.UsesAzureProtocol);
        Assert.Equal(InferenceProviderEnvironment.BitdeerDefaultModel, settings.Model);
    }

    [Fact]
    public void AzureStillResolves_WhenItIsTheOnlyHostConfigured()
    {
        // Demoting Azure in the detection order must not make it unreachable: a machine with only the three
        // AZURE_OPENAI_* variables still gets Azure, exactly as before.
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AZURE_OPENAI_ENDPOINT", "https://example.openai.azure.com/"),
            ("AZURE_OPENAI_API_KEY", "azure-key"),
            ("AZURE_OPENAI_DEPLOYMENT", "gpt-5.5")));

        Assert.Equal(InferenceProvider.AzureOpenAI, settings.Provider);
        Assert.True(settings.UsesAzureProtocol);
        Assert.Equal("gpt-5.5", settings.Model);
    }

    [Fact]
    public void TheSelectorStillPinsAzureOverBitdeer()
    {
        // Explicit beats detected, in both directions: the new order changes detection, never an outright choice.
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AI_INFERENCE_PROVIDER", "azure"),
            ("AZURE_OPENAI_ENDPOINT", "https://example.openai.azure.com/"),
            ("AZURE_OPENAI_API_KEY", "azure-key"),
            ("AZURE_OPENAI_DEPLOYMENT", "gpt-5.5"),
            ("BITDEER_API_KEY", "bitdeer-key")));

        Assert.Equal(InferenceProvider.AzureOpenAI, settings.Provider);
        Assert.Equal(InferenceProviderSelection.Explicit, settings.Selection);
    }

    [Fact]
    public void HalfConfiguredProvider_IsReportedAsThatProvidersTypo()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AZURE_OPENAI_ENDPOINT", "https://example.openai.azure.com/"),
            ("AZURE_OPENAI_API_KEY", "azure-key")));

        Assert.False(settings.IsConfigured);
        Assert.Contains("Azure OpenAI is partially configured", settings.Diagnostic, StringComparison.Ordinal);
        Assert.Contains("AZURE_OPENAI_DEPLOYMENT", settings.Diagnostic, StringComparison.Ordinal);
        // Naming every provider's full requirements would tell an operator that AZURE_OPENAI_ENDPOINT is missing
        // when they have just set it.
        Assert.DoesNotContain("AZURE_OPENAI_ENDPOINT", settings.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void NothingConfigured_ListsEveryProvidersRequirements()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env());

        Assert.False(settings.IsConfigured);
        Assert.Equal(InferenceProviderSelection.None, settings.Selection);
        foreach (var tag in InferenceProviderEnvironment.KnownValues)
        {
            Assert.Contains(tag, settings.Diagnostic, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AlternateModelsFallBackToThePrimary_NeverToSomethingUnasked()
    {
        var withAlternates = InferenceProviderEnvironment.Resolve(Env(
            ("BITDEER_API_KEY", "k"),
            ("BITDEER_MODEL", "zai-org/GLM-5.3-Flash"),
            ("BITDEER_MODEL_2", "meta-llama/Llama-4-70B")));

        Assert.Equal(["zai-org/GLM-5.3-Flash", "meta-llama/Llama-4-70B"], withAlternates.Models);

        var withoutAlternates = InferenceProviderEnvironment.Resolve(Env(("BITDEER_API_KEY", "k")));

        Assert.Equal(InferenceProviderEnvironment.BitdeerDefaultModel, withoutAlternates.SecondaryModel);
        Assert.Single(withoutAlternates.Models);
    }

    [Fact]
    public void KeylessLocalHost_GetsTheSentinelCredential()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("OPENAI_COMPATIBLE_ENDPOINT", "http://localhost:11434/v1"),
            ("OPENAI_COMPATIBLE_MODEL", "llama3.3")));

        Assert.True(settings.IsConfigured);
        Assert.Equal(InferenceProviderEnvironment.KeylessSentinel, settings.ApiKey);
    }

    [Theory]
    [InlineData("http://example.com/v1")]
    [InlineData("ftp://example.com/v1")]
    [InlineData("not-a-url")]
    public void AnEndpointThatWouldLeakTheKey_IsRefused(string endpoint)
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("OPENAI_COMPATIBLE_ENDPOINT", endpoint),
            ("OPENAI_COMPATIBLE_MODEL", "llama3.3"),
            ("OPENAI_COMPATIBLE_API_KEY", "secret-key")));

        Assert.False(settings.IsConfigured);
        Assert.Contains("OPENAI_COMPATIBLE_ENDPOINT", settings.Diagnostic, StringComparison.Ordinal);
        // The diagnostic names the variable and the reason, never the value: a configured URL can carry the key.
        Assert.DoesNotContain(endpoint, settings.Diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void PlainHttpToLoopback_IsAllowedSoALocalServerStillWorks()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("OPENAI_COMPATIBLE_ENDPOINT", "http://127.0.0.1:8000/v1"),
            ("OPENAI_COMPATIBLE_MODEL", "qwen3")));

        Assert.True(settings.IsConfigured);
    }

    [Theory]
    [InlineData("https://user:tok3n@host.example.com/v1", "tok3n")]
    [InlineData("https://host.example.com/v1/tok3n", "tok3n")]
    [InlineData("https://host.example.com/v1?api-key=tok3n", "tok3n")]
    [InlineData("https://host.example.com/v1#tok3n", "tok3n")]
    public void SafeEndpoint_DropsEveryPartThatCanCarryACredential(string url, string token)
    {
        var safe = InferenceProviderEnvironment.SafeEndpoint(new Uri(url));

        Assert.DoesNotContain(token, safe, StringComparison.Ordinal);
        Assert.Equal("https://host.example.com", safe);
    }

    [Fact]
    public void SafeEndpoint_KeepsANonDefaultPort()
    {
        Assert.Equal(
            "http://localhost:11434",
            InferenceProviderEnvironment.SafeEndpoint(new Uri("http://localhost:11434/v1")));
        Assert.Equal("(none)", InferenceProviderEnvironment.SafeEndpoint(null));
    }

    [Fact]
    public void ToString_RedactsTheKey_AndTheEndpointThatMayCarryOne()
    {
        const string secret = "not-a-real-secret";
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("BITDEER_API_KEY", secret),
            ("BITDEER_ENDPOINT", $"https://host.example.com/v1?api-key={secret}")));

        var rendered = settings.ToString();

        Assert.DoesNotContain(secret, rendered, StringComparison.Ordinal);
        Assert.Contains("[redacted]", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void AnyConfigurationAttempted_SeesTheSelectorAndEveryOptionalVariable()
    {
        Assert.False(InferenceProviderEnvironment.AnyConfigurationAttempted(Env()));
        Assert.True(InferenceProviderEnvironment.AnyConfigurationAttempted(
            Env(("AI_INFERENCE_PROVIDER", "bitdeer"))));
        // Optional variables count too: a machine with only BITDEER_MODEL set has attempted something.
        Assert.True(InferenceProviderEnvironment.AnyConfigurationAttempted(
            Env(("BITDEER_MODEL", "zai-org/GLM-5.3-Flash"))));
    }

    [Fact]
    public void AllVariables_StaysInStepWithThePerProviderSwitches()
    {
        // If this drifts, a newly added variable stops counting as configuration and the fail-closed rule in
        // AnyConfigurationAttempted quietly develops a hole.
        var expected = new List<string> { InferenceProviderEnvironment.SelectorVariable };
        foreach (var provider in Enum.GetValues<InferenceProvider>().Where(p => p != InferenceProvider.None))
        {
            expected.AddRange(InferenceProviderEnvironment.RequiredVariables(provider));
            expected.AddRange(InferenceProviderEnvironment.OptionalVariables(provider));
        }

        Assert.Equal(
            expected.Order(StringComparer.Ordinal),
            InferenceProviderEnvironment.AllVariables.Order(StringComparer.Ordinal));
        Assert.Equal(
            InferenceProviderEnvironment.AllVariables.Count,
            InferenceProviderEnvironment.AllVariables.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Builder_ProducesAClientForBothProtocolFamilies()
    {
        var bitdeer = InferenceProviderEnvironment.Resolve(Env(("BITDEER_API_KEY", "k")));
        var (bitdeerClient, bitdeerModel, bitdeerDiagnostic) = ProviderChatClientFactory.TryCreate(bitdeer);

        Assert.NotNull(bitdeerClient);
        Assert.Null(bitdeerDiagnostic);
        Assert.Equal(InferenceProviderEnvironment.BitdeerDefaultModel, bitdeerModel);
        bitdeerClient.Dispose();

        var azure = InferenceProviderEnvironment.Resolve(Env(
            ("AZURE_OPENAI_ENDPOINT", "https://example.openai.azure.com/"),
            ("AZURE_OPENAI_API_KEY", "k"),
            ("AZURE_OPENAI_DEPLOYMENT", "gpt-5.5")));
        var (azureClient, azureModel, azureDiagnostic) = ProviderChatClientFactory.TryCreate(azure);

        Assert.NotNull(azureClient);
        Assert.Null(azureDiagnostic);
        Assert.Equal("gpt-5.5", azureModel);
        azureClient.Dispose();
    }

    [Fact]
    public void Builder_HandsBackTheDiagnosticRatherThanAClient()
    {
        var (client, model, diagnostic) = ProviderChatClientFactory.TryCreate(
            InferenceProviderSettings.NotConfigured("nothing is set."));

        Assert.Null(client);
        Assert.Null(model);
        Assert.Equal("nothing is set.", diagnostic);
    }

    [Fact]
    public void Builder_TakesAModelOverrideForAComparisonRun()
    {
        var settings = InferenceProviderEnvironment.Resolve(Env(("BITDEER_API_KEY", "k")));
        var (client, model, _) = ProviderChatClientFactory.TryCreate(settings, "zai-org/GLM-5.3-Air");

        Assert.NotNull(client);
        Assert.Equal("zai-org/GLM-5.3-Air", model);
        client.Dispose();
    }

    [Fact]
    public void Banner_NamesTheHostAndModelWithoutTheCredential()
    {
        const string secret = "not-a-real-secret";
        var settings = InferenceProviderEnvironment.Resolve(Env(
            ("AI_INFERENCE_PROVIDER", "bitdeer"),
            ("BITDEER_API_KEY", secret)));

        var banner = ProviderChatClientFactory.Banner(settings);

        Assert.Contains("Bitdeer AI Model Studio", banner, StringComparison.Ordinal);
        Assert.Contains(InferenceProviderEnvironment.BitdeerDefaultModel, banner, StringComparison.Ordinal);
        Assert.Contains("https://api-inference.bitdeer.ai", banner, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, banner, StringComparison.Ordinal);
    }

    [Fact]
    public void NetworkTimeout_DefaultsHighForASlowSubjectUnderTest()
    {
        Assert.Equal(
            ProviderChatClientFactory.DefaultNetworkTimeout,
            ProviderChatClientFactory.NetworkTimeout(Env()));
        Assert.Equal(
            TimeSpan.FromSeconds(42),
            ProviderChatClientFactory.NetworkTimeout(Env(("GATEKEEPER_NETWORK_TIMEOUT_S", "42"))));
        Assert.Equal(
            ProviderChatClientFactory.DefaultNetworkTimeout,
            ProviderChatClientFactory.NetworkTimeout(Env(("GATEKEEPER_NETWORK_TIMEOUT_S", "nonsense"))));
    }
}
