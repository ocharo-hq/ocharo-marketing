namespace OcharoBlog.Tests.E2E;

/// <summary>A built Studio in a temporary web root: its shell and one hashed asset.</summary>
internal sealed class StudioBundle : IDisposable
{
    public string Root { get; } =
        Path.Combine(Path.GetTempPath(), "ocharo-blog-studio", Guid.NewGuid().ToString("N"));

    public StudioBundle()
    {
        Directory.CreateDirectory(Path.Combine(Root, "studio", "assets"));
        File.WriteAllText(Path.Combine(Root, "studio", "index.html"), "<!doctype html><title>Studio</title>");
        File.WriteAllText(Path.Combine(Root, "studio", "assets", "app.js"), "export {};");
    }

    public void Dispose()
    {
        try { Directory.Delete(Root, true); } catch (IOException) { }
    }
}
