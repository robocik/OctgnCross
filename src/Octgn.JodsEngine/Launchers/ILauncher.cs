using Octgn.JodsEngine.Windows;

namespace Octgn.Launchers
{
    public interface ILauncher
    {
        string Name { get; }
        Task<bool> Launch(ILoadingView view);
    }
}