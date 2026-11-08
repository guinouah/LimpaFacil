namespace LimpaFacil.Mobile.Ui;

/// <summary>Construção visual; nenhum acesso à API, banco ou regra de compra.</summary>
public static class BoundUi
{
    public static Label Text(string path, double size = 14, Color? color = null, string? format = null, bool bold = false)
    {
        var label = new Label { FontSize = size, TextColor = color ?? Theme.Text,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None, VerticalTextAlignment = TextAlignment.Center };
        label.SetBinding(Label.TextProperty, new Binding(path, stringFormat: format));
        return label;
    }
    public static Button Button(string text, string command, bool secondary = false)
    {
        var button = secondary ? Theme.SecondaryButton(text) : Theme.PrimaryButton(text);
        button.SetBinding(Microsoft.Maui.Controls.Button.CommandProperty, command);
        return button;
    }
    public static ToolbarItem Toolbar(string text, string command)
    {
        var item = new ToolbarItem { Text = text };
        item.SetBinding(MenuItem.CommandProperty, command);
        return item;
    }
    public static CollectionView Collection(string source, Func<View> template, string empty)
    {
        var collection = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 12 },
            ItemTemplate = new DataTemplate(template),
            EmptyView = new Label { Text = empty, TextColor = Theme.Muted, HorizontalTextAlignment = TextAlignment.Center, Margin = 20 }
        };
        collection.SetBinding(ItemsView.ItemsSourceProperty, source);
        return collection;
    }
    public static Grid ListPage(View heading, View list)
    {
        var grid = new Grid { Padding = 14, RowSpacing = 12,
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Star)] };
        grid.Add(heading, 0, 0);
        grid.Add(list, 0, 1);
        return grid;
    }
}
