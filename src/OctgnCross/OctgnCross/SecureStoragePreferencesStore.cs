using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using OctgnCross.Core;

namespace Octgn;

public class SecureStoragePreferencesStore:IPreferencesStore
{
    public async Task<T> GetValue<T>(string key, T defaultValue,bool decrypt)
    {
        var value= await SecureStorage.Default.GetAsync(key);
        if (value != null)
        {
            TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
            return (T)typeConverter.ConvertFromString(value);
        }
        else if(defaultValue is not null)
        {
            await SecureStorage.Default.SetAsync(key,defaultValue.ToString());
        }
        return defaultValue;
    }

    public async Task SetValue<T>(string key, T value,bool encrypt)
    {
        await SecureStorage.Default.SetAsync(key,value?.ToString());
    }
}