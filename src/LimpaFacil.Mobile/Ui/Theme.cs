using Microsoft.Maui.Controls.Shapes;

namespace LimpaFacil.Mobile.Ui;

public static class Theme
{
    // Alinhado ao tema visual do site (app.css): verde + limão sobre fundo claro.
    public static readonly Color Green = Color.FromArgb("#147D64");
    public static readonly Color Lime = Color.FromArgb("#DFF274");
    public static readonly Color Blue = Color.FromArgb("#147D64");
    public static readonly Color Cyan = Color.FromArgb("#147D64");
    public static readonly Color Background = Color.FromArgb("#F4F8F5");
    public static readonly Color Surface = Colors.White;
    public static readonly Color Border = Color.FromArgb("#DCE8E1");
    public static readonly Color Text = Color.FromArgb("#173C33");
    public static readonly Color Muted = Color.FromArgb("#53726A");

    public static Button PrimaryButton(string text) => new()
    {
        Text = text, BackgroundColor = Green, TextColor = Colors.White,
        FontAttributes = FontAttributes.Bold, FontSize = 15, CornerRadius = 8, HeightRequest = 48
    };

    public static Button SecondaryButton(string text) => new()
    {
        Text = text, BackgroundColor = Color.FromArgb("#E5EEE9"), TextColor = Color.FromArgb("#1A5B4B"),
        FontAttributes = FontAttributes.Bold, CornerRadius = 8, HeightRequest = 44
    };

    public static Border Card(View content, double padding = 18) => new()
    {
        BackgroundColor = Surface, Stroke = Border, StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = padding, Content = content,
        Shadow = new Shadow { Brush = Color.FromArgb("#153D2F"), Opacity = .08f, Radius = 16, Offset = new Point(0, 6) }
    };

    public static Border Field(Entry entry, string icon)
    {
        var grid = new Grid
        {
            ColumnDefinitions = [new(26), new(GridLength.Star)], ColumnSpacing = 8
        };
        grid.Add(new Label { Text = icon, FontSize = 14, VerticalOptions = LayoutOptions.Center }, 0, 0);
        grid.Add(entry, 1, 0);
        return new Border
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"), Stroke = Border, StrokeThickness = 1.5,
            StrokeShape = new RoundRectangle { CornerRadius = 8 }, HeightRequest = 50,
            Padding = new Thickness(12, 0), Content = grid
        };
    }

    public static Entry Entry(string placeholder, Keyboard? keyboard = null, bool password = false) => new()
    {
        Placeholder = placeholder, Keyboard = keyboard ?? Keyboard.Default, IsPassword = password,
        BackgroundColor = Colors.Transparent, TextColor = Text, PlaceholderColor = Color.FromArgb("#6C7C76"),
        FontSize = 14, VerticalOptions = LayoutOptions.Center,
        ClearButtonVisibility = password ? ClearButtonVisibility.Never : ClearButtonVisibility.WhileEditing
    };
}
