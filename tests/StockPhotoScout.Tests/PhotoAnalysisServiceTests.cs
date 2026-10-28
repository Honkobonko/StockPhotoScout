using StockPhotoScout.Interfaces;
using StockPhotoScout.Services;
using Xunit;

namespace StockPhotoScout.Tests;

public sealed class PhotoAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_ValidJson_ReturnsAnalysisResult()
    {
        var client = new FakeOllamaClient("""
            {
              "stockPotential": 8,
              "peopleStatus": "None Detected",
              "releaseStatus": "Not Required (Provisional)",
              "recommendation": "Candidate",
              "explanation": "Clear landscape with commercial potential."
            }
            """);

        var service = new PhotoAnalysisService(client);

        var result = await service.AnalyzeAsync("unused.jpg");

        Assert.Equal(8, result.StockPotential);
        Assert.Equal("None Detected", result.PeopleStatus);
        Assert.Equal("Not Required (Provisional)", result.ReleaseStatus);
        Assert.Equal("Candidate", result.Recommendation);
        Assert.Equal(
            "Clear landscape with commercial potential.",
            result.Explanation);
    }

    [Theory]
    [InlineData(12, 10)]
    [InlineData(-1, 0)]
    [InlineData(5, 5)]
    public async Task AnalyzeAsync_StockPotential_IsClamped(
        int input, int expected)
    {
        var client = new FakeOllamaClient($$"""
            {
              "stockPotential": {{input}},
              "peopleStatus": "None Detected",
              "releaseStatus": "Unclear",
              "recommendation": "Candidate",
              "explanation": "Test"
            }
            """);

        var service = new PhotoAnalysisService(client);

        var result = await service.AnalyzeAsync("unused.jpg");

        Assert.Equal(expected, result.StockPotential);
    }

    [Fact]
    public async Task AnalyzeAsync_InvalidStatuses_UsesSafeDefaults()
    {
        var client = new FakeOllamaClient("""
            {
              "stockPotential": 6,
              "peopleStatus": "Unknown",
              "releaseStatus": "Unknown",
              "recommendation": "Unknown",
              "explanation": "Test"
            }
            """);

        var service = new PhotoAnalysisService(client);

        var result = await service.AnalyzeAsync("unused.jpg");

        Assert.Equal("Identifiability Unclear", result.PeopleStatus);
        Assert.Equal("Unclear", result.ReleaseStatus);
        Assert.Equal("Manual Review", result.Recommendation);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    public async Task AnalyzeAsync_InvalidJson_ThrowsInvalidOperationException(
        string response)
    {
        var client = new FakeOllamaClient(response);
        var service = new PhotoAnalysisService(client);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync("unused.jpg"));
    }

    [Fact]
    public async Task AnalyzeAsync_JsonNull_ThrowsInvalidOperationException()
    {
        var client = new FakeOllamaClient("null");
        var service = new PhotoAnalysisService(client);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync("unused.jpg"));
    }

    private sealed class FakeOllamaClient(string response) : IOllamaClient
    {
        public Task<bool> IsReadyAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<string> AnalyzeImageAsync(
            string imagePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(response);
        }
    }
}