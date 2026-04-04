using System.ComponentModel.DataAnnotations;

namespace AgriApp.Web.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? CenterId { get; set; }
}

public class ApiErrorResponse
{
    public string? Error { get; set; }
    public string? Title { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }

    public IEnumerable<string> GetMessages()
    {
        var messages = new List<string>();
        if (!string.IsNullOrWhiteSpace(Error))
            messages.Add(Error);
        if (!string.IsNullOrWhiteSpace(Title))
            messages.Add(Title);
        if (Errors != null)
            messages.AddRange(Errors.SelectMany(e => e.Value));
        return messages.Count > 0 ? messages : ["An unexpected error occurred."];
    }
}
