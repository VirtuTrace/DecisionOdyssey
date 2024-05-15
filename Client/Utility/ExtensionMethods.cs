namespace Client.Utility;

public static class ExtensionMethods
{
    public async static Task<byte[]> GetBytesAsync(this Stream stream)
    {
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
    
    public static string RemoveChar(this string str, char charToRemove)
    {
        return str.Replace(charToRemove.ToString(), string.Empty);
    }
    
    public static int ToAmPmHour(this TimeSpan time)
    {
        var h = time.Hours % 12;
        if (h == 0)
        {
            h = 12;
        }

        return h;
    }

    public static int ToSeconds(this TimeSpan time)
    {
        return time.Hours * 3600 + time.Minutes * 60 + time.Seconds;
    }

    public static void Initialize<T>(this List<List<T>> list, int rows, int columns) where T : new()
    {
        for(var row = 0; row < rows; row++)
        {
            list.Add([]);
            for(var column = 0; column < columns; column++)
            {
                list[row].Add(new T());
            }
        }
    }
    
    public static IEnumerable<(int i, T)> Enumerate<T>(this List<T> list)
    {
        return list.Select((t, i) => (i, t));
    }
    
    public static IEnumerable<(int i, T)> Enumerate<T>(this T[] array)
    {
        return array.Select((t, i) => (i, t));
    }
    
    public static string ToFormattedString<T>(this IEnumerable<T> list)
    {
        return $"[{string.Join(", ", list)}]";
    }
}