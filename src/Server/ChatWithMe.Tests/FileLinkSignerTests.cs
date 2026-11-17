using ChatWithMe.Application.Abstractions;
using ChatWithMe.Infrastructure.Auth;
using ChatWithMe.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace ChatWithMe.Tests;

public sealed class FileLinkSignerTests
{
    private static readonly DateTime Now = new(2026, 11, 17, 1, 0, 0, DateTimeKind.Utc);

    private static FileLinkSigner CreateSigner(string signingKey)
    {
        var options = new JwtOptions
        {
            SigningKey = Convert.ToBase64String(Convert.FromBase64String(signingKey)),
            Issuer = "ChatWithMe.Api",
            Audience = "ChatWithMe.Client",
        };

        return new FileLinkSigner(Options.Create(options), new FixedClock(Now));
    }

    private static string NewKey() => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    [Fact]
    public void Verify_AcceptsSignatureForSameFileBeforeExpiry()
    {
        var signer = CreateSigner(NewKey());
        var fileId = Guid.NewGuid();
        var expiresAtUtc = Now.AddMinutes(5);

        var signature = signer.CreateSignature(fileId, expiresAtUtc);

        Assert.True(signer.Verify(fileId, expiresAtUtc, signature));
    }

    [Fact]
    public void Verify_RejectsExpiredSignature()
    {
        var signer = CreateSigner(NewKey());
        var fileId = Guid.NewGuid();
        var expiresAtUtc = Now.AddSeconds(-1);

        var signature = signer.CreateSignature(fileId, expiresAtUtc);

        Assert.False(signer.Verify(fileId, expiresAtUtc, signature));
    }

    [Fact]
    public void Verify_RejectsTamperedFileIdOrSignature()
    {
        var signer = CreateSigner(NewKey());
        var fileId = Guid.NewGuid();
        var expiresAtUtc = Now.AddMinutes(5);
        var signature = signer.CreateSignature(fileId, expiresAtUtc);

        Assert.False(signer.Verify(Guid.NewGuid(), expiresAtUtc, signature));
        Assert.False(signer.Verify(fileId, expiresAtUtc, signature[..^1]));
        Assert.False(signer.Verify(fileId, expiresAtUtc, null));
    }

    [Fact]
    public void Verify_RejectsSignatureProducedWithAnotherKey()
    {
        var expiresAtUtc = Now.AddMinutes(5);
        var fileId = Guid.NewGuid();

        var foreignSignature = CreateSigner(NewKey()).CreateSignature(fileId, expiresAtUtc);
        var verifier = CreateSigner(NewKey());

        Assert.False(verifier.Verify(fileId, expiresAtUtc, foreignSignature));
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
