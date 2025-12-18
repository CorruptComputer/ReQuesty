using System.Reflection;

namespace ReQuesty.Core;

/// <summary>
///   The version class
/// </summary>
public static class ReQuestyVersion
{
    private static Version? _version = Assembly.GetAssembly(typeof(ReQuestyVersion))?.GetName().Version;

    /// <summary>
    ///   The current version string
    /// </summary>
    public static string Current()
    {
        return _version?.ToString(3) ?? "0.0.0";
    }

    /// <summary>
    ///   The current major version string
    /// </summary>
    public static string CurrentMajor()
    {
        return _version?.Major.ToString() ?? "0";
    }
}