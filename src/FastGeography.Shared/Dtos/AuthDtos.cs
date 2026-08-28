namespace FastGeography.Shared.Dtos;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record UserInfoResponse(string UserId, string Email, string DisplayName);
