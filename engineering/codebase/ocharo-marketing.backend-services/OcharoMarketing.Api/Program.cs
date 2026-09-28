using OcharoMarketing.Api.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Configure();

var app = builder.Build();
app.Configure();

app.Run();

/// <summary>Exposes the host entrypoint to the SDK integration-test harness.</summary>
public partial class Program { }
