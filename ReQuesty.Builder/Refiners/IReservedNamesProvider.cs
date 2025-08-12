namespace ReQuesty.Builder.Refiners;
public interface IReservedNamesProvider
{
    HashSet<string> ReservedNames
    {
        get;
    }
}
