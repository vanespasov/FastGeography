namespace FastGeography.Shared.Dtos;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record UserInfoResponse(string UserId, string Email, string DisplayName, string PreferredLanguage = "en");

public record SetLanguageRequest(string LanguageCode);
