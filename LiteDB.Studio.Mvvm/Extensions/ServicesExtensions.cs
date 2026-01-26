using Microsoft.Extensions.DependencyInjection;

namespace LiteDB.Studio.Mvvm.Extensions;

public static class ServicesExtensions
{
    /// <summary>
    /// Attempts to retrieve a required service of type <typeparamref name="T"/> from the specified <see cref="IServiceProvider"/>.
    /// Returns <c>true</c> and sets <paramref name="service"/> if successful; otherwise returns <c>false</c> and sets <paramref name="service"/> to its default value.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve. Must be non-nullable.</typeparam>
    /// <param name="services">The <see cref="IServiceProvider"/> to retrieve the service from.</param>
    /// <param name="service">When this method returns, contains the service instance if found; otherwise, the default value for type <typeparamref name="T"/>.</param>
    /// <returns><c>true</c> if the service was successfully retrieved; otherwise, <c>false</c>.</returns>
    public static bool TryGetService<T>(this IServiceProvider services, out T service) where T : notnull {
        if (services is null) { throw new ArgumentNullException(nameof(services)); }
        service = default!;
        try {
            service = services.GetRequiredService<T>();
            return true;
        }
        catch (Exception) {
            // ignored
        }
        return false;
    }
}
