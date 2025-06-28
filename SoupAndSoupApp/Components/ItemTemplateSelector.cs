using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Components
{
    public class ItemTemplateSelector : IDataTemplate
    {
        public DataTemplate CheckBoxTemplate { get; set; }
        public DataTemplate ButtonTemplate { get; set; }

        public Control Build(object? param)
        {
            if (param is ComponentModel item)
            {
                if (item.IsButton)
                    return ButtonTemplate?.Build(param) ?? new TextBlock { Text = "No ButtonTemplate" };

                return CheckBoxTemplate?.Build(param) ?? new TextBlock { Text = "No CheckBoxTemplate" };
            }

            return new TextBlock { Text = "Unknown Item" };
        }

        public bool Match(object? data)
        {
            return data is ComponentModel;
        }
    }

}
