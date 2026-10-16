namespace Cinematch.Models;

public record UserGrid(
    string Id,
    string User,
    string Topic,
    List<int> MovieIds,
    DateTime CreatedUtc);