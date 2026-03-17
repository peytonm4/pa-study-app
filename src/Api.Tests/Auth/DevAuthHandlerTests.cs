using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using StudyApp.Api.Auth;
using System.Security.Claims;

namespace StudyApp.Api.Tests.Auth;

public class DevAuthHandlerTests
{
    private static async Task<AuthenticateResult> AuthenticateWithHeader(string? headerValue)
    {
        var options = new OptionsMonitorStub<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
        var loggerFactory = LoggerFactory.Create(_ => { });
        var encoder = new UrlTestEncoder();

        var handler = new DevAuthHandler(options, loggerFactory, encoder);

        var context = new DefaultHttpContext();
        if (headerValue is not null)
            context.Request.Headers["X-Dev-UserId"] = headerValue;

        var scheme = new AuthenticationScheme("DevAuth", "DevAuth", typeof(DevAuthHandler));
        await handler.InitializeAsync(scheme, context);

        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task MissingHeader_ReturnsNoResult()
    {
        var result = await AuthenticateWithHeader(null);

        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task InvalidGuid_ReturnsFailResult()
    {
        var result = await AuthenticateWithHeader("not-a-guid");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Equal("Invalid Guid", result.Failure!.Message);
    }

    [Fact]
    public async Task ValidGuid_ReturnsSuccess_WithNameIdentifierClaim()
    {
        var guid = "00000000-0000-0000-0000-000000000001";
        var result = await AuthenticateWithHeader(guid);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);

        var claim = result.Principal!.FindFirst(ClaimTypes.NameIdentifier);
        Assert.NotNull(claim);
        Assert.Equal(guid, claim!.Value);
    }

    [Fact]
    public async Task ValidGuid_AuthenticatedPrincipal_HasDevAuthSchemeName()
    {
        var result = await AuthenticateWithHeader("00000000-0000-0000-0000-000000000002");

        Assert.True(result.Succeeded);
        Assert.Equal("DevAuth", result.Ticket!.AuthenticationScheme);
    }

    private sealed class OptionsMonitorStub<TOptions>(TOptions options) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue => options;
        public TOptions Get(string? name) => options;
        public IDisposable OnChange(Action<TOptions, string?> listener) => new NullDisposable();
        private sealed class NullDisposable : IDisposable { public void Dispose() { } }
    }
}
