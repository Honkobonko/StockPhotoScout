using System.IO;
using StockPhotoScout.Models;

namespace StockPhotoScout.Services;

public sealed class ImageCopyService
{
    public CopyResult CopyFiles(
        IEnumerable<PhotoItem> photos,
        string destinationFolder)
    {
        int copiedCount = 0;
        int failedCount = 0;

        foreach (PhotoItem photo in photos)
        {
            try
            {
                string targetPath = GetUniqueDestinationPath(
                    destinationFolder,
                    photo.FileName);

                File.Copy(
                    photo.FilePath,
                    targetPath,
                    overwrite: false);

                copiedCount++;
            }
            catch (Exception)
            {
                failedCount++;
            }
        }

        return new CopyResult(copiedCount, failedCount);
    }

    private static string GetUniqueDestinationPath(
        string destinationFolder,
        string fileName)
    {
        string targetPath = Path.Combine(destinationFolder, fileName);

        if (!File.Exists(targetPath))
            return targetPath;

        string nameWithoutExtension =
            Path.GetFileNameWithoutExtension(fileName);

        string extension = Path.GetExtension(fileName);

        int index = 2;

        do
        {
            string newFileName =
                $"{nameWithoutExtension}_{index}{extension}";

            targetPath = Path.Combine(
                destinationFolder,
                newFileName);

            index++;
        }
        while (File.Exists(targetPath));

        return targetPath;
    }
}

public sealed record CopyResult(
    int CopiedCount,
    int FailedCount);
