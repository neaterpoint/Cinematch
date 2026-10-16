namespace Cinematch.Models;

public record Movie(
    int Id,
    string Title,
    int Year,
    string Director,
    List<string> Genres,
    double Rating,
    double Popularity,
    string Overview);