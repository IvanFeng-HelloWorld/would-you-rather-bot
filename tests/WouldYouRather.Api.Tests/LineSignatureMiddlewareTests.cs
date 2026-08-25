using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using WouldYouRather.Api.Middleware;
using WouldYouRather.Api.Models;

namespace WouldYouRather.Api.Tests;

[TestFixture]
public class Given_LineSignatureMiddleware
{
    private static byte[] ComputeHmacSha256(string key, byte[] body)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(body);
    }

    [Test]
    public async Task Given_ValidSignature_When_Invoke_Then_AllowsRequest()
    {
        // Given
        var secret = "test-secret";
        var body = Encoding.UTF8.GetBytes("{\"hello\":\"world\"}");

        var signature = Convert.ToBase64String(ComputeHmacSha256(secret, body));

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/webhook";
        context.Request.Body = new MemoryStream(body);
        context.Request.Headers["X-Line-Signature"] = signature;

        var options = Options.Create(new LineBotSetting { ChannelSecret = secret });

        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LineSignatureMiddleware(next, options);

        // When
        await middleware.InvokeAsync(context);

        // Then
        Assert.IsTrue(nextCalled, "Next delegate should be called for valid signature");
        Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode, "Response status should be 200 when allowed");
    }

    [Test]
    public async Task Given_InvalidSignature_When_Invoke_Then_Returns401()
    {
        // Given
        var secret = "test-secret";
        var body = Encoding.UTF8.GetBytes("{\"hello\":\"world\"}");

        var badSignature = Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid"));

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/webhook";
        context.Request.Body = new MemoryStream(body);
        context.Request.Headers["X-Line-Signature"] = badSignature;

        var options = Options.Create(new LineBotSetting { ChannelSecret = secret });

        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LineSignatureMiddleware(next, options);

        // When
        await middleware.InvokeAsync(context);

        // Then
        Assert.IsFalse(nextCalled, "Next delegate should not be called for invalid signature");
        Assert.AreEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode, "Response status should be 401 for invalid signature");
    }

    [Test]
    public async Task Given_MissingSignatureHeader_When_Invoke_Then_Returns401()
    {
        // Given
        var secret = "test-secret";
        var body = Encoding.UTF8.GetBytes("{\"hello\":\"world\"}");

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/webhook";
        context.Request.Body = new MemoryStream(body);

        var options = Options.Create(new LineBotSetting { ChannelSecret = secret });

        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LineSignatureMiddleware(next, options);

        // When
        await middleware.InvokeAsync(context);

        // Then
        Assert.IsFalse(nextCalled, "Next delegate should not be called when header is missing");
        Assert.AreEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode, "Response status should be 401 when header missing");
    }
}
