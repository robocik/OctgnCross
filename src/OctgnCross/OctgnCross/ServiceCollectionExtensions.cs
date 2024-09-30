using Microsoft.Extensions.DependencyInjection;
using NuGet.Protocol.Core.Types;
using Octgn.JodsEngine.Play;
using Octgn.Scripting;
using Octgn.ViewModels;

namespace Octgn;

public static class ServiceCollectionExtensions {
    public static void AddCommonServices(this IServiceCollection collection) {
        collection.AddSingleton<IConfiguratorLoader,DefaultConfiguratorLoader>();

        collection.AddTransient<MainViewModel>();
        collection.AddTransient<PlayWindow>();
        collection.AddSingleton<Engine>();
    }
}