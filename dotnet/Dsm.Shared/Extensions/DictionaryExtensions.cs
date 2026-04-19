namespace Dsm.Shared.Extensions;

public static class DictionaryExtensions
{
    public static void AddToLookup<TKey, TValue>(this Dictionary<TKey, List<TValue>> @this, TKey key, TValue value)
    {
        if (@this.TryGetValue(key, out var values))
        {
            values.Add(value);
        }
        else
        {
            var newValues = new List<TValue>()
            {
                value
            };
            @this.Add(key, newValues);
        }
    }
}