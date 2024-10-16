﻿using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Client.Models.DecisionElements;
using Client.Models.DecisionElements.DecisionMatrix;
using Client.Utility;
using Common.DataStructures;
using Common.DataStructures.Dtos;
using Common.DataStructures.Dtos.DecisionElements;
using Common.DataStructures.Dtos.DecisionElements.Stats;
using Common.DataStructures.Http.Requests;
using Common.DataStructures.Http.Responses;
using Common.Enums;

namespace Client.Singletons;

public partial class HttpUtility
{
    private ApplicationState _applicationState = null!;

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
            PropertyNameCaseInsensitive = true
    };

    [GeneratedRegex(@"entry_(\d+)_(\d+)_(image|audio|video|text)")]
    private static partial Regex EntryRegex();

    [GeneratedRegex("prompt_(image|audio|video|text)")]
    private static partial Regex PromptRegex();

    public void Initialize(ApplicationState applicationState)
    {
        _applicationState = applicationState;
    }
    
    private async Task<HttpResponseMessage> ExecuteGetRequest(HttpClient http, string endpoint)
    {
        while (true)
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _applicationState.AccessToken);
            var response = await http.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            Console.WriteLine("Failed to execute get request");
            if (await CheckAndRefreshToken(http, response))
            {
                Console.WriteLine("Retrying request");
                continue;
            }

            return response;
        }
    }

    private async Task<HttpResponseMessage> ExecutePostRequest<T>(HttpClient http, string endpoint, T payload)
    {
        while (true)
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _applicationState.AccessToken);
            var response = await http.PostAsJsonAsync(endpoint, payload);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            Console.WriteLine("Failed to execute get request");
            if (await CheckAndRefreshToken(http, response))
            {
                Console.WriteLine("Retrying request");
                continue;
            }

            return response;
        }
    }
    
    public async Task<UserDto?> GetUser(HttpClient http)
    {
        var response = await ExecuteGetRequest(http, "api/users");
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync("Failed to get user");
            return null;
        }
        
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        return user;
    }

    public async Task<List<DecisionMatrixDto>> GetMatrices(HttpClient http)
    {
        var response = await ExecuteGetRequest(http, "api/DecisionMatrix");
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync("Failed to get matrices");
            return [];
        }
        
        var matrices = new List<DecisionMatrixDto>();
        var deserializedMatrices = await response.Content.ReadFromJsonAsync<List<DecisionMatrixDto>>(JsonOptions);
        if (deserializedMatrices is not null)
        {
            matrices.AddRange(deserializedMatrices);
        }
        else
        {
            await Console.Error.WriteLineAsync("Failed to deserialize matrices");
        }
        
        return matrices;
    }

    private async Task<bool> CheckAndRefreshToken(HttpClient http, HttpResponseMessage response)
    {
        //Console.WriteLine("Checking token");
        var headers = response.Headers;
        if (headers.TryGetValues("token-expired", out var values) && values.Contains("true"))
        {
            //Console.WriteLine("Token expired");
            return await RefreshToken(http);
        }

        return false;
    }

    private async Task<bool> RefreshToken(HttpClient http)
    {
        Console.WriteLine("Refreshing token");
        var tokenRequest = new TokenRequest {
            AccessToken = _applicationState.RefreshToken
        };
        var response = await http.PostAsJsonAsync("api/users/refresh", tokenRequest);
        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
            if (authResponse is null)
            {
                await Console.Error.WriteLineAsync("Failed to deserialize refresh token");
                return false;
            }
            
            await _applicationState.StoreCredentials(authResponse, _applicationState.Email);
        }
        else
        {
            await Console.Error.WriteLineAsync("Failed to refresh token");
            return false;
        }
        
        return true;
    }

    public async Task<DecisionMatrix> GetMatrix(HttpClient http, Guid matrixGuid)
    {
        var endpoint = $"api/DecisionMatrix/{matrixGuid}/data";
        var response = await ExecuteGetRequest(http, endpoint);
        var matrix = new DecisionMatrix();
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

    private static async Task<DecisionMatrix> ReadArchive(ZipArchive archive)
    {
        var metadataStream = archive.GetEntry("metadata.json")!.Open();
        var metadataBytes = await metadataStream.GetBytesAsync();
        var metadata = JsonSerializer.Deserialize<DecisionMatrixMetadata>(metadataBytes, JsonOptions);
        if (metadata is null)
        {
            throw new Exception("Failed to deserialize metadata");
        }

        var matrix = new DecisionMatrix();
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
                return new DecisionMatrix();
            }
        }

        return matrix;
    }

    private static async Task ParseEntry(ZipArchiveEntry entry, MatrixCell cell, string dataType)
    {
        switch (dataType) // TODO: Ensure that files have extensions
        {
            case "image":
                var image = cell.Image;
                await PopulateMediaData(image, entry);
                break;
            case "audio":
                var audio = cell.Audio;
                await PopulateMediaData(audio, entry);
                break;
            case "video":
                var video = cell.Video;
                await PopulateMediaData(video, entry);
                break;
            case "text":
            {
                await using var entryStream = entry.Open();
                var stringBytes = await entryStream.GetBytesAsync();
                cell.Text = Encoding.UTF8.GetString(stringBytes);
                break;
            }
        }
    }

    private static async Task PopulateMediaData(MediaData media, ZipArchiveEntry entry)
    {
        await using var entryStream = entry.Open();
        media.Data = await entryStream.GetBytesAsync();
        media.Extension = Path.GetExtension(entry.Name);
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

    public async Task<List<DecisionMatrixStatsData>> GetMatrixStats(HttpClient http, Guid matrixGuid)
    {
        var response = await ExecuteGetRequest(http, $"api/DecisionMatrix/{matrixGuid}/stats/data");
        if(!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync("Failed to get matrix stats");
            return [];
        }
        
        var content = await response.Content.ReadFromJsonAsync<List<DecisionMatrixStatsData>>(JsonOptions);
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
            AccessToken = _applicationState.AccessToken
        };
        await http.PostAsJsonAsync("api/users/logout", tokenRequest);
        // Don't check response status code because the token will be invalidated (on success) or is already invalid (on failure)
        // if (!response.IsSuccessStatusCode)
        // {
        //     await Console.Error.WriteLineAsync("Failed to logout");
        // }
        await _applicationState.ClearCredentials();
    }

    public async Task<string> GetUserRole(HttpClient http)
    {
        var response = await ExecuteGetRequest(http, "api/users/role");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Failed to get user role");
            return "";
        }
        
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }
    
    public async Task<UserStatusResponse?> GetUserStatus(HttpClient http, Guid userGuid)
    {
        var response = await ExecuteGetRequest(http, $"api/admin/user/{userGuid}/status");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Failed to get user status");
            return null;
        }
        
        var content = await response.Content.ReadFromJsonAsync<UserStatusResponse>();
        return content;
    }
    
    public async Task<List<AdvanceUserDto>> GetUsersForAdmin(HttpClient http)
    {
        var response = await ExecuteGetRequest(http, "api/admin/users");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Failed to get users");
            return [];
        }
        
        var content = await response.Content.ReadFromJsonAsync<List<AdvanceUserDto>>();
        return content ?? [];
    }
    
    public static async Task<bool> LockUser(HttpClient http, Guid userGuid)
    {
        var response = await http.PostAsync($"api/admin/user/{userGuid}/lock", null);
        return response.IsSuccessStatusCode;
    }
    
    public static async Task<bool> UnlockUser(HttpClient http, Guid userGuid)
    {
        var response = await http.PostAsync($"api/admin/user/{userGuid}/unlock", null);
        return response.IsSuccessStatusCode;
    }
    
    public static async Task<bool> UpdateUserRole(HttpClient http, Guid userGuid, string role)
    {
        var response = await http.PostAsJsonAsync($"api/admin/user/{userGuid}/role", role);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> UpdateUserEmail(HttpClient http, string email)
    {
        var emailRequest = new ChangeEmailRequest
        {
            NewEmail = email
        };
        var response = await ExecutePostRequest(http, "api/users/email", emailRequest);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> UpdateUserSecondaryEmail(HttpClient http, string? secondaryEmail)
    {
        var emailRequest = new ChangeEmailRequest
        {
            NewEmail = secondaryEmail
        };
        var response = await ExecutePostRequest(http, "api/users/secondary-email", emailRequest);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<PasswordError> UpdateUserPassword(HttpClient http, string currentPassword, string newPassword)
    {
        var passwordRequest = new ChangePasswordRequest
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        };
        var response = await ExecutePostRequest(http, "api/users/password", passwordRequest);
        if (response.IsSuccessStatusCode)
        {
            return PasswordError.None;
        }
        
        var errorCodes = await response.Content.ReadFromJsonAsync<List<string>>();
        if (errorCodes is null || errorCodes.Count == 0)
        {
            return PasswordError.UnknownError;
        }
        
        return errorCodes[0] switch
        {
            "PasswordTooShort" => PasswordError.PasswordTooShort,
            "PasswordRequiresNonAlphanumeric" => PasswordError.PasswordRequiresNonAlphanumeric,
            "PasswordRequiresDigit" => PasswordError.PasswordRequiresDigit,
            "PasswordRequiresLower" => PasswordError.PasswordRequiresLower,
            "PasswordRequiresUpper" => PasswordError.PasswordRequiresUpper,
            "PasswordRequiresUniqueChars" => PasswordError.PasswordRequiresUniqueChars,
            _ => PasswordError.PasswordRequirementsNotMet
        };
    }
}