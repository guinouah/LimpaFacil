using LimpaFacil.Mobile.Ui;
using LimpaFacil.Mobile.ViewModels;

namespace LimpaFacil.Mobile.Pages;

public sealed class StartupPage : ContentPage
{
    private readonly StartupViewModel viewModel;
    public StartupPage(StartupViewModel viewModel)
    {
        this.viewModel = viewModel;
        BindingContext = viewModel;
        BackgroundColor = Theme.Background;
        var indicator = new ActivityIndicator { Color = Theme.Blue };
        indicator.SetBinding(ActivityIndicator.IsRunningProperty, nameof(StartupViewModel.IsBusy));
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Spacing = 16,
            Children = { indicator, BoundUi.Text(nameof(StartupViewModel.Message)),
                BoundUi.Button("Tentar novamente", nameof(StartupViewModel.InitializeCommand), true) }
        };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await viewModel.InitializeCommand.ExecuteAsync(); }
}
