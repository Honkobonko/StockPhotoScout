namespace StockPhotoScout.Interfaces;

public interface IOllamaClient
{
    Task<bool> IsReadyAsync(
        CancellationToken cancellationToken = default);

    Task<string> AnalyzeImageAsync(
        string imagePath,
        CancellationToken cancellationToken = default);
}