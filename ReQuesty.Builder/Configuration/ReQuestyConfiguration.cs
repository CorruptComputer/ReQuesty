namespace ReQuesty.Builder.Configuration;

#pragma warning disable CA2227
#pragma warning disable CA1002
public class ReQuestyConfiguration : ICloneable
{
    public GenerationConfiguration Generation { get; set; } = new();
    public LanguagesInformation Languages { get; set; } = [];

    public object Clone()
    {
        return new ReQuestyConfiguration
        {
            Generation = (GenerationConfiguration)Generation.Clone(),
            Languages = (LanguagesInformation)Languages.Clone(),
        };
    }
}
#pragma warning restore CA1002
#pragma warning restore CA2227
