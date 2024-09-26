using Octgn.Library;
using Octgn.Library.ExtensionMethods;

namespace OctgnCross.Core;

public interface IPreferencesStore
{
    Task<T> GetValue<T>(string key, T defaultValue,bool decrypt=false);

    Task SetValue<T>(string key, T value,bool encrypt=false);
}

