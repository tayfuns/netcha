using NetCha.Api.Admin;

namespace NetCha.Api.Tests;

public sealed class AdminPasswordTests
{
    [Fact]
    public void Password_hash_is_salted_and_verifiable()
    {
        const string password = "a-long-admin-password";
        var first = AdminPassword.Hash(password);
        var second = AdminPassword.Hash(password);

        Assert.NotEqual(first, second);
        Assert.True(AdminPassword.Verify(password, first));
        Assert.False(AdminPassword.Verify("wrong-password", first));
    }

    [Fact]
    public void Session_tokens_are_random_and_hashable()
    {
        var first = AdminPassword.NewToken();
        var second = AdminPassword.NewToken();

        Assert.NotEqual(first, second);
        Assert.Equal(64, AdminPassword.TokenHash(first).Length);
    }
}
