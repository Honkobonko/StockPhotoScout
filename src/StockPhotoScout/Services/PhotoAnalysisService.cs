using System.Text.Json;
using StockPhotoScout.Models;
using StockPhotoScout.Interfaces;

namespace StockPhotoScout.Services;

public sealed class PhotoAnalysisService
{
    private readonly IOllamaClient _ollamaClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> ValidPeopleStatuses =
    [
        "None Detected",
        "People Detected",
        "Identifiability Unclear"
    ];

    private static readonly HashSet<string> ValidReleaseStatuses =
    [
        "Not Required (Provisional)",
        "Release Needs Review",
        "Release Available",
        "Unclear"
    ];

    private static readonly HashSet<string> ValidRecommendations =
    [
        "Candidate",
        "Manual Review",
        "Likely Unsuitable"
    ];

    public PhotoAnalysisService(IOllamaClient ollamaClient)
    {
        _ollamaClient = ollamaClient;
    }

    public Task<bool> IsReadyAsync(
        CancellationToken cancellationToken = default)
    {
        return _ollamaClient.IsReadyAsync(cancellationToken);
    }

    public async Task<PhotoAnalysisResult> AnalyzeAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        var json = await _ollamaClient.AnalyzeImageAsync(
            imagePath, cancellationToken);

        PhotoAnalysisResult? result;

        try
        {
            result = JsonSerializer.Deserialize<PhotoAnalysisResult>(
                json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Ollama hat kein gültiges Analyse-JSON zurückgegeben.", ex);
        }

        if (result is null)
        {
            throw new InvalidOperationException(
                "Die Analyse von Ollama war leer.");
        }

        ValidateResult(result);

        return result;
    }

    private static void ValidateResult(PhotoAnalysisResult result)
    {
        result.StockPotential = Math.Clamp(result.StockPotential, 0, 100);

        if (!ValidPeopleStatuses.Contains(result.PeopleStatus))
            result.PeopleStatus = "Identifiability Unclear";

        if (!ValidReleaseStatuses.Contains(result.ReleaseStatus))
            result.ReleaseStatus = "Unclear";

        if (!ValidRecommendations.Contains(result.Recommendation))
            result.Recommendation = "Manual Review";

        result.Explanation ??= string.Empty;
    }

    public async Task AnalyzePhotoAsync(
    PhotoItem photo,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(photo);
        
        var result = await AnalyzeAsync(
            photo.FilePath, cancellationToken);

        photo.StockPotential = result.StockPotential;
        photo.PeopleStatus = result.PeopleStatus;
        photo.ReleaseStatus = result.ReleaseStatus;
        photo.Recommendation = result.Recommendation;
        photo.Explanation = result.Explanation;
        photo.HasPeople = result.PeopleStatus == "People Detected";
        photo.IsAnalyzed = true;
    }
}