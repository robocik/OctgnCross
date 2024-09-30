using Octgn.JodsEngine.Windows;

namespace Octgn.JodsEngine.Loaders;

public interface ILoader
{
    string Name { get; }

    Task Load(ILoadingView view);
}