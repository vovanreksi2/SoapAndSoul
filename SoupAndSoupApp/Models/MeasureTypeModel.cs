using Avalonia.Media.Imaging;

namespace SoupAndSoupApp.Models;

public class MeasureTypeModel
{
    public MeasureTypeModel(int id, string title, string shortTitle, string displayTitle, Bitmap? icon = null)
    {
        Id = id;
        Title = title;
        ShortTitle = shortTitle;
        DisplayTitle = displayTitle;
        Icon = icon;
    }


    public int Id { get; set; }
    public string Title { get; set; }
    public string ShortTitle { get; set; }
    public string DisplayTitle { get; set; }

    public Bitmap? Icon { get; set; }

    public override string ToString() => Title;
}