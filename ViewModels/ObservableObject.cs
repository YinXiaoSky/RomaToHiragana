using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RomaToHiragana.ViewModels;

/// <summary>
/// 实现 <see cref="INotifyPropertyChanged"/> 的最小基类，
/// 供各 ViewModel 继承，避免引入额外的 MVVM 依赖包。
///</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
