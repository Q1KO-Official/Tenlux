using System.Windows.Input;

namespace Tenlux.Helpers;

internal sealed class SimpleCommand : ICommand
{
    private readonly Action _execute;
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public SimpleCommand(Action execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}
