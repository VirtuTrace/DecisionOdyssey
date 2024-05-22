using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Client.Models.DecisionElements.DecisionMatrix;
using Client.Utility;
using Common.DataStructures.Dtos.DecisionElements;
using Common.DataStructures.Dtos.DecisionElements.Stats;
using Common.DataStructures.Http.Requests;
using Common.DataStructures.Http.Responses;

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

    public async Task<List<DecisionMatrixDto>> GetMatrices(HttpClient http)
    {
        while (true)
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", applicationState.AccessToken);
            var response = await http.GetAsync("api/DecisionMatrix");
            var matrices = new List<DecisionMatrixDto>();
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                #if DEBUG
                Console.WriteLine(content);
                #endif

                var deserializedMatrices = JsonSerializer.Deserialize<List<DecisionMatrixDto>>(content, Options);

                if (deserializedMatrices is not null)
                {
                    matrices.AddRange(deserializedMatrices);
                }
                else
                {
                    await Console.Error.WriteLineAsync("Failed to deserialize matrices");
                }
            }
            else
            {
                if (await CheckAndRefreshToken(http, response))
                {
                    Console.WriteLine("Retrying");
                    continue;
                }

                await Console.Error.WriteLineAsync("Failed to get matrices");
            }

            return matrices;
        }
    }

    private async Task<bool> CheckAndRefreshToken(HttpClient http, HttpResponseMessage response)
    {
        var headers = response.Headers;
        if (headers.TryGetValues("token-expired", out var values) && values.Contains("true"))
        {
            return await RefreshToken(http);
        }

        return false;
    }
    
    private async Task<bool> RefreshToken(HttpClient http)
    {
        var tokenRequest = new TokenRequest {
            AccessToken = applicationState.RefreshToken
        };
        var response = await http.PostAsJsonAsync("api/users/refresh", tokenRequest);
        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse is null)
            {
                await Console.Error.WriteLineAsync("Failed to deserialize refresh token");
                return false;
            }
            
            await applicationState.StoreCredentials(authResponse, applicationState.Email);
        }
        else
        {
            await Console.Error.WriteLineAsync("Failed to refresh token");
            return false;
        }
        
        return true;
    }
    
    public async Task<Matrix> GetMatrix(HttpClient http, Guid matrixGuid)
    {
        var endpoint = $"api/DecisionMatrix/{matrixGuid}/data";
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", applicationState.AccessToken);
        var response = await http.GetAsync(endpoint);

        var matrix = new Matrix();
        if (response.IsSuccessStatusCode)
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            matrix = await ReadArchive(archive);
        }
        else
        {
            await Console.Error.WriteLineAsync("Failed to get matrix data");
        }

        return matrix;
    }
    
    private static async Task<Matrix> ReadArchive(ZipArchive archive)
    {
        var metadataStream = archive.GetEntry("metadata.json")!.Open();
        var metadataBytes = await metadataStream.GetBytesAsync();
        var metadata = JsonSerializer.Deserialize<DecisionMatrixMetadata>(metadataBytes);
        if (metadata is null)
        {
            throw new Exception("Failed to deserialize metadata");
        }

        var matrix = new Matrix();
        matrix.FromMetadata(metadata);

        foreach (var entry in archive.Entries)
        {
            if (entry.Name == "metadata.json")
            {
                continue;
            }
            
            var match = EntryRegex().Match(entry.Name);
            if (match.Success)
            {
                var row = int.Parse(match.Groups[1].Value);
                var column = int.Parse(match.Groups[2].Value);
                var dataType = match.Groups[3].Value;
                await ParseEntry(entry, matrix[row, column], dataType);
                continue;
            }
            
            match = PromptRegex().Match(entry.Name);
            if (match.Success)
            {
                var dataType = match.Groups[1].Value;
                await ParseEntry(entry, matrix.Prompt, dataType);
            }
            else
            {
                await Console.Error.WriteLineAsync($"Failed to match entry: {entry.Name}");
                return new Matrix();
            }
        }

        return matrix;
    }

    private static async Task ParseEntry(ZipArchiveEntry entry, MatrixCell cell, string dataType)
    {
        await using var entryStream = entry.Open();
        switch (dataType) // TODO: Ensure that files have extensions
        {
            case "image":
                var image = cell.Image;
                image.Data = await entryStream.GetBytesAsync();
                image.Extension = Path.GetExtension(entry.Name);
                break;
            case "audio":
                var audio = cell.Audio;
                audio.Data = await entryStream.GetBytesAsync();
                audio.Extension = Path.GetExtension(entry.Name);
                break;
            case "video":
                var video = cell.Video;
                video.Data = await entryStream.GetBytesAsync();
                video.Extension = Path.GetExtension(entry.Name);
                break;
            case "text":
                var stringBytes = await entryStream.GetBytesAsync();
                cell.Text = Encoding.UTF8.GetString(stringBytes);
                break;
        }
    }
    
    public static (MultipartContent, MemoryStream) CreateMultiPartContent(byte[] archive, string jsonContent, string filename)
    {
        var formContent = new MultipartFormDataContent("f84f617add024da5a7dbc216a37dae7f"); // Randomly generated
        var memoryStream = new MemoryStream(archive);
        memoryStream.Position = 0;
        
        var streamContent = new StreamContent(memoryStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"file\"",
            FileName = $"\"{filename}.zip\""
        };

        var jsonStringContent = new StringContent(jsonContent);
        jsonStringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        formContent.Add(new StreamContent(memoryStream), "file", $"{filename}.zip");
        formContent.Add(jsonStringContent, "metadata");
        return (formContent, memoryStream);
    }

    public static async Task<List<DecisionMatrixStatsDto>> GetMatrixStats(HttpClient http, Guid matrixGuid)
    {
        var response = await http.GetAsync($"api/DecisionMatrix/{matrixGuid}/stats/data");
        if(!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync("Failed to get matrix stats");
            return [];
        }
        
        var content = await response.Content.ReadFromJsonAsync<List<DecisionMatrixStatsDto>>();
        if (content is null)
        {
            await Console.Error.WriteLineAsync("Failed to deserialize matrix stats");
            return [];
        }
        
        return content;
    }

    public async Task Logout(HttpClient http)
    {
        var tokenRequest = new TokenRequest
        {
            AccessToken = applicationState.AccessToken
        };
        var response = await http.PostAsJsonAsync("logout", tokenRequest);
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync("Failed to logout");
        }
        await applicationState.ClearCredentials();
    }
}