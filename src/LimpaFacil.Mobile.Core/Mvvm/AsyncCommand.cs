using System.Windows.Input;

namespace LimpaFacil.Mobile.Mvvm;

/// <summary>Comando aguardável que impede cliques concorrentes.</summary>
public sealed class AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    public bool IsRunning { get; private set; }
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !IsRunning && (canExecute?.Invoke() ?? true);
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    public async Task ExecuteAsync()
    {
        if (!CanExecute(null)) return;
        IsRunning = true;
        NotifyCanExecuteChanged();
        try { await execute(); }
        finally { IsRunning = false; NotifyCanExecuteChanged(); }
    }
    // A ação criada pelo BaseViewModel captura erros e os expõe na propriedade Message.
    public async void Execute(object? parameter) => await ExecuteAsync();
}
