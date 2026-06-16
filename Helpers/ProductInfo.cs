using System.Reflection;

namespace Tenlux.Helpers;

internal static class ProductInfo
{
    public const string Name = "Tenlux";
    public const string ChineseName = "执光";
    public const string RepositoryUrl = "https://github.com/Q1KO-Official/Tenlux";
    public const string Publisher = "Q1KO";
    public const string LicenseName = "CC BY-NC-SA 4.0";
    public const string ShortDescription = "Lightweight Windows theme switcher";
    public const string StoreDescription = "Tray-first Windows theme switcher with wallpaper automation";

    public static string Version =>
        (typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
         ?? typeof(App).Assembly.GetName().Version?.ToString(3)
         ?? "0.0.0").Split('+')[0];

    public static bool IsPreview =>
        Version.Contains("preview", StringComparison.OrdinalIgnoreCase);
}
