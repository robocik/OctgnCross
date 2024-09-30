namespace Octgn.Scripting.Wrappers;

using System;
using System.Text.Json;
using System.Collections.Generic;

public class JavaScriptSerializer
{
    private JsonSerializerOptions _options;

    public JavaScriptSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    // Serializacja obiektu na JSON
    public string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    // Deserializacja JSON na obiekt określonego typu
    public T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options);
    }

    // Deserializacja JSON na obiekt dynamiczny (Dictionary<string, object>)
    public object Deserialize(string json, Type targetType)
    {
        return JsonSerializer.Deserialize(json, targetType, _options);
    }

    // Można też dodać obsługę dla innych typów, podobnie jak w oryginalnej klasie
    public Dictionary<string, object> DeserializeObject(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);
    }

    // Opcjonalnie można dodać wsparcie dla dostosowywania ustawień serializacji
    public void SetOptions(JsonSerializerOptions options)
    {
        _options = options;
    }

    // Funkcja konfiguracyjna, np. do ustawienia czy JSON ma być sformatowany (wcięcia)
    public void SetIndentation(bool indented)
    {
        _options.WriteIndented = indented;
    }

    // Funkcja konfiguracyjna, np. do ustawienia konwencji nazewnictwa (CamelCase)
    public void SetPropertyNamingPolicy(JsonNamingPolicy policy)
    {
        _options.PropertyNamingPolicy = policy;
    }
}
