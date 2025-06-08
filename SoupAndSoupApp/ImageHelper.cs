using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace SoupAndSoupApp;

public static class ImageHelper
{
    public static Bitmap LoadFromResource(string resourceUri)
    {
        if (string.IsNullOrWhiteSpace(resourceUri))
        {
            throw new ArgumentException("Resource URI cannot be null or empty.", nameof(resourceUri));
        }

        if (!resourceUri.StartsWith("Assets"))
        {
            resourceUri = resourceUri[resourceUri.IndexOf("Assets")..];
        }

        return new Bitmap(AssetLoader.Open(new Uri("avares://SoupAndSoupApp/" + resourceUri)));
    }
 

    public static async Task<Bitmap?> LoadFromWeb(Uri url)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsByteArrayAsync();
            return new Bitmap(new MemoryStream(data));
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"An error occurred while downloading image '{url}' : {ex.Message}");
            return null;
        }
    }
}