using OcharoMarketing.Api.Configurations;

// Build and configure the host.
var builder = WebApplication.CreateBuilder(args);
builder.Configure();

// Build and configure the app.
var app = builder.Build();
app.Configure();

// Run.
app.Run();

public partial class Program { }
