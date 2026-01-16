using System;
using UnityEngine;

public static class JsonHelper
{
    // Parse từ JSON sang mảng object
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{\"array\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    // Convert từ mảng object sang JSON string (trả thẳng mảng, không bọc "array")
    public static string ToJson<T>(T[] array, bool prettyPrint = false)
    {
        // JsonUtility chỉ serialize object, không trực tiếp mảng => bọc rồi lấy phần bên trong.
        Wrapper<T> wrapper = new Wrapper<T> { array = array };
        string wrapped = JsonUtility.ToJson(wrapper, prettyPrint);
        // wrapped looks like: {"array":[{...},{...}]}
        // Trả về chỉ phần mảng: [{...},{...}]
        const string prefix = "{\"array\":";
        if (wrapped.StartsWith(prefix))
        {
            string result = wrapped.Substring(prefix.Length);
            // remove trailing } at end
            if (result.EndsWith("}"))
                result = result.Substring(0, result.Length - 1);
            return result;
        }
        return wrapped;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}
