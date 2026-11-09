using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using OcharoBlog.Api.Access;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Infrastructure.Blog;
using WoW.Two.Sdk.Beta.Be.Http.Auth.OAuth2;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;
using WoW.Two.Sdk.Beta.Be.Identity.Signals;
using WoW.Two.Sdk.Beta.Be.Identity.Testing;
using static OcharoBlog.Tests.Support.EditorialSignIn;
using Xunit;

namespace OcharoBlog.Tests.E2E;

/// <summary>Tests that take the media editor's process-wide capacity, which one test fills on purpose, so they don't
/// run together.</summary>
internal static class MediaEditingCapacity
{
    public const string Name = "Media editing capacity";
}
