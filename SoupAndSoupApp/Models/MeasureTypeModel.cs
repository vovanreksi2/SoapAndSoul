using System;
using Avalonia.Media.Imaging;
using SoupAndSoupApp.Helpers;

namespace SoupAndSoupApp.Models;

public class MeasureTypeModel
{
    public MeasureTypeModel(int id, string title, string shortTitle, string displayTitle, Bitmap? icon = null)
    {
        Id = id;
        Title = title;
        ShortTitle = shortTitle;
        Icon = icon;
    }
    
    public MeasureTypeModel(SoupAndSoup.Data.Models.MeasureType measureType)
    {
        Id = measureType.Id;
        Title = measureType.Name;
        ShortTitle = measureType.ShortName;
        Icon = GetBitmapByMeasureType(measureType.Id); 
    }

    public int Id { get; set; }
    public string Title { get; set; }
    public string ShortTitle { get; set; }

    public MeasureType MeasureType => (MeasureType)Id;

    public Bitmap? Icon { get; set; }

    public override string ToString() => Title;

    private Bitmap GetBitmapByMeasureType(int id)
    {
        return (MeasureType)id switch
        {
            MeasureType.Gram => ImageHelper.LoadFromResource("Assets/Gram.png"),
            MeasureType.Milliliter => ImageHelper.LoadFromResource("Assets/Milliliter.png"),
            MeasureType.Drop => ImageHelper.LoadFromResource("Assets/Drop.png"),
            MeasureType.Piece => ImageHelper.LoadFromResource("Assets/Gram.png"),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

}