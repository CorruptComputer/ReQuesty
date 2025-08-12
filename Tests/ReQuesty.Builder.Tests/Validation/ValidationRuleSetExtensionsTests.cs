using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Validation;
using Microsoft.OpenApi;
using Xunit;

namespace ReQuesty.Builder.Tests.Validation;

public class ValidationRuleSetExtensionsTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => ValidationRuleSetExtensions.AddReQuestyValidationRules(null!, new()));
        ValidationRuleSetExtensions.AddReQuestyValidationRules(new(), null!);
    }
    [Fact]
    public void DisablesAllRules()
    {
        ValidationRuleSet ruleSet = new();
        GenerationConfiguration configuration = new() { DisabledValidationRules = ["all"] };
        ruleSet.AddReQuestyValidationRules(configuration);
        Assert.Empty(ruleSet.Rules);
    }
    [Fact]
    public void DisablesNoRule()
    {
        ValidationRuleSet ruleSet = new();
        GenerationConfiguration configuration = new() { DisabledValidationRules = [] };
        ruleSet.AddReQuestyValidationRules(configuration);
        Assert.NotEmpty(ruleSet.Rules);
    }
    [Fact]
    public void DisablesOneRule()
    {
        ValidationRuleSet ruleSet = new();
        GenerationConfiguration configuration = new() { DisabledValidationRules = [nameof(NoServerEntry)] };
        ruleSet.AddReQuestyValidationRules(configuration);
        Assert.NotEmpty(ruleSet.Rules);
        Assert.DoesNotContain(ruleSet.Rules, static x => x.GetType() == typeof(NoServerEntry));
    }
}
