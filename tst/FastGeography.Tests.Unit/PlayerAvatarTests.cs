namespace FastGeography.Tests.Unit;

using FastGeography.Shared;

public sealed class PlayerAvatarTests
{
    [Fact]
    public void Resolve_SameUserId_ReturnsSameAvatar()
    {
        var first = PlayerAvatar.Resolve("user-123");
        var second = PlayerAvatar.Resolve("user-123");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Resolve_DifferentUserIds_CanDiffer()
    {
        var avatars = Enumerable.Range(0, 20)
            .Select(i => PlayerAvatar.Resolve($"user-{i}"))
            .Select(a => a.Index)
            .Distinct()
            .Count();

        Assert.True(avatars > 1);
    }

    [Fact]
    public void Resolve_UsesDisplayNameWhenUserIdMissing()
    {
        var avatar = PlayerAvatar.Resolve(null, "Explorer");

        Assert.InRange(avatar.Index, 0, PlayerAvatar.StyleCount - 1);
        Assert.False(string.IsNullOrWhiteSpace(avatar.BackgroundColor));
    }
}
