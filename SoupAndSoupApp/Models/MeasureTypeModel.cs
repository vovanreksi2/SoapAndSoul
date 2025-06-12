using Avalonia.Media.Imaging;

namespace SoupAndSoupApp.Models;

public class MeasureTypeModel
{
    public MeasureTypeModel(int id, string title, string shortTitle, Bitmap? icon = null)
    {
        Id = id;
        Title = title;
        ShortTitle = shortTitle;
        Icon = icon;
    }

    public string ShortTitle { get; set; }

    public int Id { get; set; }
    public string Title { get; set; }

    public Bitmap? Icon { get; set; }

    public override string ToString() => Title;
}