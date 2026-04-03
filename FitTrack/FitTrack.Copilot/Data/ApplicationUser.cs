using Microsoft.AspNetCore.Identity;

namespace FitTrack.Copilot.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public UserProfile? Profile { get; set; }

    public List<ConversationThread> ConversationThreads { get; set; } = new();

    public List<RefreshToken> RefreshTokens { get; set; } = new();
}
