namespace ReQuesty.Builder.Configuration;

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
