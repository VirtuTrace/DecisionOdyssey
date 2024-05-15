using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Client.Utility;

namespace Client.Singletons;

public partial class HttpUtility(ApplicationState applicationState)
{
    [GeneratedRegex(@"entry_(\d+)_(\d+)_(image|audio|video|text)")]
    private static partial Regex EntryRegex();
    
    [GeneratedRegex("prompt_(image|audio|video|text)")]
    private static partial Regex PromptRegex();

    private static JsonSerializerOptions? _options;
    
    private static JsonSerializerOptions Options =>
        _options ??= new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

    
}