using System.Threading.Tasks;
using Octgn.Extentions;
using Octgn.Library;
using OctgnCross.Core;

namespace Octgn;

public class DefaultPreferencesStore:IPreferencesStore
{
    public Task<T> GetValue<T>(string key, T defaultValue,bool decrypt)
    {
        var value=Config.Instance.ReadValue(key, defaultValue);
        return Task.FromResult(value);
    }


    public Task SetValue<T>(string key, T value,bool encrypt)
    {
        Config.Instance.WriteValue(key,value);
        return Task.CompletedTask;
    }
}