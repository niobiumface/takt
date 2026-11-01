using System.Net.Http;
using System.Text.Json;
using Takt.Helpers;

namespace Takt.Services;

internal static class UpdateChecker
{
    public const string ReleasesUrl = "https://api.github.com/repos/niobiumface/takt/releases/latest";

    public static async Task<string?> FindNewerTagAsync(Version current, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Takt");
        using var response = await client.GetAsync(ReleasesUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("tag_name", out var tag))
            return null;
        var text = tag.GetString();
        return ReleaseVersion.IsNewer(text, current) ? text : null;
    }
}
