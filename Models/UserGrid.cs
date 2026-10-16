namespace Cinematch.Models;

public record UserGrid(
    string Id,
    string User,
    string Topic,
    List<int> MovieIds,
    DateTime CreatedUtc)
{
    public string Initial
    {
        get
        {
            string name = User.TrimStart('@');
            return name.Length > 0 ? char.ToUpper(name[0]).ToString() : "?";
        }
    }

    // "@reelmaya" -> "/profile/reelmaya" (escaped in case the handle has spaces)
    public string ProfileUrl => "/profile/" + Uri.EscapeDataString(User.TrimStart('@'));
}