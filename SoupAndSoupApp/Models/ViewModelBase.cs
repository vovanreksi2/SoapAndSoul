using System;
using System.Reactive.Disposables;
using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class ViewModelBase : ReactiveObject, IDisposable
{
    protected CompositeDisposable Disposables { get; } = new();

    public void Dispose()
    {
        Disposables.Dispose();
    }
}
