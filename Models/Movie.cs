namespace Cinematch.Models;

public record Movie(
    int Id,
    string Title,
    int Year,
    string Director,
    List<string> Genres,
    double Rating,
    double Popularity,
    string Overview)
{
    // No posters in the dataset, so make a stable gradient from the movie id
    public string Gradient()
    {
        int hue = (Id * 47) % 360;
        int hue2 = (hue + 40) % 360;
        return $"background:linear-gradient(160deg,hsl({hue},45%,32%),hsl({hue2},40%,10%))";
    }
}