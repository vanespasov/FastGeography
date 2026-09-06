namespace FastGeography.Shared;

/// <summary>
/// Assigns a stable explorer avatar and background color from a user id (or display name fallback).
/// </summary>
public static class PlayerAvatar
{
    public const int StyleCount = 8;

    private static readonly string[] BackgroundColors =
    [
        "#2d6a4f", "#1d3557", "#7b2cbf", "#bc4749",
        "#0077b6", "#e85d04", "#6a994e", "#9b2226",
        "#3a5a40", "#4361ee", "#6d597a", "#40916c",
    ];

    public static PlayerAvatarInfo Resolve(string? userId, string? displayName = null)
    {
        var seed = string.IsNullOrWhiteSpace(userId) ? displayName ?? string.Empty : userId;
        var hash = Hash(seed);
        return new PlayerAvatarInfo(
            (int)(hash % (uint)StyleCount),
            BackgroundColors[(int)(hash % (uint)BackgroundColors.Length)]);
    }

    private static uint Hash(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var ch in value)
            {
                hash ^= ch;
                hash *= 16777619;
            }

            return hash;
        }
    }
}

public readonly record struct PlayerAvatarInfo(int Index, string BackgroundColor);
