using LimpaFacil.Mobile.Services;

namespace LimpaFacil.Mobile;

public sealed class App : Application
{
    private readonly AppNavigator navigator;
    public App(AppNavigator navigator) => this.navigator = navigator;
    protected override Window CreateWindow(IActivationState? activationState) => new(navigator.CreateInitialPage())
    {
        Title = "LimpaFácil - App Cliente",
        Width = 420,
        Height = 840,
        MinimumWidth = 380,
        MinimumHeight = 640,
        MaximumWidth = 520,
        MaximumHeight = 940
    };
}
