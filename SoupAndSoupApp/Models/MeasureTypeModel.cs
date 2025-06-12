namespace SoupAndSoupApp.Models;

public class MeasureTypeModel
{
    public MeasureTypeModel(int id, string title)
    {
        Id = id;
        Title = title;
    }

    public int Id { get; set; }
    public string Title { get; set; }
}