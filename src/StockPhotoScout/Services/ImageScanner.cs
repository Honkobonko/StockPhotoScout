using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media.Imaging;
using StockPhotoScout.Models;

namespace StockPhotoScout.Services;

public sealed class ImageScanner
{
    public List<PhotoItem> ScanFolder(
        string sourceFolder,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<PhotoItem>();

        var imageFiles = Directory
            .EnumerateFiles(
                sourceFolder,
                "*.*",
                SearchOption.AllDirectories)
            .Where(IsSupportedImage)
            .OrderBy(
                path => path,
                StringComparer.OrdinalIgnoreCase);

        foreach (string filePath in imageFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BitmapImage? thumbnail = null;

            // A damaged image must not stop the entire scan.
            try
            {
                thumbnail = LoadBitmap(filePath, 180);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Keep the file in the results without a thumbnail.
            }

            cancellationToken.ThrowIfCancellationRequested();

            results.Add(new PhotoItem
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Thumbnail = thumbnail,

                StockPotential = 0,

                PeopleStatus = "Identifiability Unclear",
                ReleaseStatus = "Unclear",
                Recommendation = "Not Analyzed Yet",

                IsAnalyzed = false,
                HasPeople = false
            });
        }

        cancellationToken.ThrowIfCancellationRequested();

        return results;
    }

    private static bool IsSupportedImage(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        return extension.Equals(
                   ".jpg",
                   StringComparison.OrdinalIgnoreCase)
               || extension.Equals(
                   ".jpeg",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static BitmapImage LoadBitmap(
        string filePath,
        int decodePixelWidth)
    {
        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        var bitmap = new BitmapImage();

        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = decodePixelWidth;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }
}