using Microsoft.Maui.Controls.Shapes;

namespace LimpaFacil.Mobile.Ui;

public static class Theme
{
    public static readonly Color Blue = Color.FromArgb("#1E40AF");
    public static readonly Color Cyan = Color.FromArgb("#0284C7");
    public static readonly Color Green = Color.FromArgb("#16A34A");
    public static readonly Color Background = Color.FromArgb("#F8FAFC");
    public static readonly Color Surface = Colors.White;
    public static readonly Color Border = Color.FromArgb("#E2E8F0");
    public static readonly Color Text = Color.FromArgb("#0F172A");
    public static readonly Color Muted = Color.FromArgb("#64748B");

    public static Button PrimaryButton(string text) => new()
    {
        Text = text, BackgroundColor = Blue, TextColor = Colors.White,
        FontAttributes = FontAttributes.Bold, FontSize = 15, CornerRadius = 14, HeightRequest = 50
    };

    public static Button SecondaryButton(string text) => new()
    {
        Text = text, BackgroundColor = Color.FromArgb("#EFF6FF"), TextColor = Blue,
        FontAttributes = FontAttributes.Bold, CornerRadius = 14, HeightRequest = 46
    };

    public static Border Card(View content, double padding = 18) => new()
    {
        BackgroundColor = Surface, Stroke = Border, StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 22 }, Padding = padding, Content = content,
        Shadow = new Shadow { Brush = Color.FromArgb("#334155"), Opacity = .10f, Radius = 16, Offset = new Point(0, 6) }
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
            StrokeShape = new RoundRectangle { CornerRadius = 14 }, HeightRequest = 50,
            Padding = new Thickness(12, 0), Content = grid
        };
    }

    public static Entry Entry(string placeholder, Keyboard? keyboard = null, bool password = false) => new()
    {
        Placeholder = placeholder, Keyboard = keyboard ?? Keyboard.Default, IsPassword = password,
        BackgroundColor = Colors.Transparent, TextColor = Text, PlaceholderColor = Color.FromArgb("#94A3B8"),
        FontSize = 14, VerticalOptions = LayoutOptions.Center,
        ClearButtonVisibility = password ? ClearButtonVisibility.Never : ClearButtonVisibility.WhileEditing
    };
}
