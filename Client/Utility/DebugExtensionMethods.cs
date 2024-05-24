namespace Client.Utility;

public static class DebugExtensionMethods
{
    public static void Dump<T>(this IEnumerable<T> enumerable)
    {
        Console.WriteLine($"[{string.Join(", ", enumerable.Select(x => x?.ToString()).ToArray())}]");
    }
}