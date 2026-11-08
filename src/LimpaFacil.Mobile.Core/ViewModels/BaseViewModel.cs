using LimpaFacil.Mobile.Mvvm;

namespace LimpaFacil.Mobile.ViewModels;

public abstract class BaseViewModel : ObservableObject
{
    private readonly List<AsyncCommand> commands = [];
    private string message = "";
    private bool isBusy;
    public string Message { get => message; protected set => SetProperty(ref message, value); }
    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (!SetProperty(ref isBusy, value)) return;
            OnPropertyChanged(nameof(IsNotBusy));
            foreach (var command in commands) command.NotifyCanExecuteChanged();
        }
    }
    public bool IsNotBusy => !IsBusy;
    protected AsyncCommand Command(Func<Task> action, Func<bool>? allowed = null)
    {
        var command = new AsyncCommand(() => RunAsync(action), () => !IsBusy && (allowed?.Invoke() ?? true));
        commands.Add(command);
        return command;
    }
    protected async Task RunAsync(Func<Task> action)
    {
        IsBusy = true;
        try { await action(); }
        catch (Exception ex) { Message = ex.Message; }
        finally { IsBusy = false; }
    }
}
