using System.Net;
using System.Reflection;
namespace OcharoBlog.Application.Blog.Site;
/// <summary>Identifies the build that rendered the public publication without claiming a release for local builds.</summary>
internal static class BuildVersion
{
    internal static string Value => typeof(BuildVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
    internal static string Badge => $"<small class=\"app-version\" data-app-version aria-label=\"App version\" title=\"{WebUtility.HtmlEncode(Value)}\">{WebUtility.HtmlEncode(Value.Split('+')[0])}</small>";
}
