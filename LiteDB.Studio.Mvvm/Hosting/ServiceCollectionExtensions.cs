using System;
using System.Windows;
using LiteDB.Studio.Mvvm.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LiteDB.Studio.Mvvm.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddView<TView, TViewModel>(
        this IServiceCollection services,
        ServiceLifetime viewLifetime = ServiceLifetime.Singleton,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Singleton)
        where TView : FrameworkElement
        where TViewModel : class, IViewModel
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(TView), typeof(TView), viewLifetime));
        services.Add(new ServiceDescriptor(typeof(TViewModel), typeof(TViewModel), viewModelLifetime));
        services.AddSingleton(new ViewRegistration(typeof(TView), typeof(TViewModel)));

        return services;
    }

    public static IServiceCollection AddViewFactory(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IViewFactory, ViewFactory>();
        return services;
    }
}
