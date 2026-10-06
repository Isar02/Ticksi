using System.ComponentModel.DataAnnotations;
using Ticksi.Application.Options;

namespace Ticksi.Tests.AuthTests.UnitTests;

public class JwtOptionsTests
{
    [Fact]
    public void Validate_Defaults_Pass()
    {
        Assert.Empty(Validate(Options()));
    }

    [Theory]
    [InlineData(1, 4)]
    [InlineData(15, 1441)]
    public void Validate_IdleWindowOutOfRange_Fails(int accessTokenMinutes, int sessionIdleMinutes)
    {
        var errors = Validate(Options(accessTokenMinutes, sessionIdleMinutes));

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(JwtOptions.SessionIdleMinutes)));
    }

    [Theory]
    [InlineData(30, 30)]
    [InlineData(45, 30)]
    public void Validate_AccessTokenOutlivingTheIdleWindow_Fails(int accessTokenMinutes, int sessionIdleMinutes)
    {
        var error = Assert.Single(Validate(Options(accessTokenMinutes, sessionIdleMinutes)));

        Assert.Equal([nameof(JwtOptions.AccessTokenMinutes), nameof(JwtOptions.SessionIdleMinutes)], error.MemberNames);
    }

    private static JwtOptions Options(int accessTokenMinutes = 15, int sessionIdleMinutes = 30) => new()
    {
        Issuer = "Ticksi.Tests",
        Audience = "Ticksi.Tests",
        Key = "TestSigningKeyThatIsAtLeast32CharactersLong",
        AccessTokenMinutes = accessTokenMinutes,
        SessionIdleMinutes = sessionIdleMinutes
    };

    private static List<ValidationResult> Validate(JwtOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
