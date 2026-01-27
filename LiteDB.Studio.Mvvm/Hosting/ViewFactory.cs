using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LiteDB.Studio.Mvvm.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LiteDB.Studio.Mvvm.Hosting;

internal sealed class ViewFactory : IViewFactory
{
    private readonly IServiceProvider _services;
    private readonly IReadOnlyDictionary<Type, Type> _viewModelToViewMap;

    public ViewFactory(IServiceProvider services, IEnumerable<ViewRegistration> registrations)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        Dictionary<Type, Type> map = new();
        foreach (ViewRegistration registration in registrations)
        {
            if (map.ContainsKey(registration.ViewModelType))
            {
                throw new InvalidOperationException(
                    $"A view is already registered for view model type '{registration.ViewModelType.FullName}'.");
            }

            map[registration.ViewModelType] = registration.ViewType;
        }

        _viewModelToViewMap = map;
    }

    public FrameworkElement CreateView(Type viewType)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        return (FrameworkElement)_services.GetRequiredService(viewType);
    }

    public FrameworkElement CreateViewFor(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);

        if (!_viewModelToViewMap.TryGetValue(viewModelType, out Type? viewType))
        {
            throw new InvalidOperationException($"No view registered for view model type '{viewModelType.FullName}'.");
        }

        return CreateView(viewType);
    }

    public TView CreateView<TView>() where TView : FrameworkElement => (TView)CreateView(typeof(TView));

    public FrameworkElement CreateViewFor<TViewModel>() where TViewModel : class, IViewModel
        => CreateViewFor(typeof(TViewModel));
}
