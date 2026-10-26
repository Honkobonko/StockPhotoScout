using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StockPhotoScout.Services;

namespace StockPhotoScout;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<PhotoItem> _photos = new();

    private readonly ImageCopyService _imageCopyService = new();
    private readonly ImageScanner _imageScanner = new();
    private readonly PhotoFilterService _photoFilterService = new();

    private System.ComponentModel.ICollectionView _photosView = null!;

    private string _sourceFolder = string.Empty;
    private string _destinationFolder = string.Empty;

    public MainWindow()
    {
        InitializeComponent();

        _photosView = CollectionViewSource.GetDefaultView(_photos);
        _photosView.Filter = PhotoMatchesFilters;

        ResultsGrid.ItemsSource = _photosView;

        UpdateCount();
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Source Folder",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        _sourceFolder = dialog.FolderName;
        SourceFolderTextBox.Text = _sourceFolder;
    }

    private void BrowseDestination_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Destination Folder",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        _destinationFolder = dialog.FolderName;
        DestinationFolderTextBox.Text = _destinationFolder;
    }

    private void SelectAllFiltered_Click(
        object sender,
        RoutedEventArgs e)
    {
        foreach (PhotoItem photo in _photosView)
        {
            photo.IsSelected = true;
        }

        UpdateCount();
    }

    private void DeselectAllFiltered_Click(
        object sender,
        RoutedEventArgs e)
    {
        foreach (PhotoItem photo in _photosView)
        {
            photo.IsSelected = false;
        }

        UpdateCount();
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_sourceFolder) ||
            !Directory.Exists(_sourceFolder))
        {
            MessageBox.Show(
                "Please select a valid source folder first.",
                "Source Folder Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_destinationFolder) &&
            IsSameOrChildPath(_destinationFolder, _sourceFolder))
        {
            MessageBox.Show(
                "The destination folder must not be inside the source folder.",
                "Invalid Destination",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            CountText.Text = "Scanning images...";

            PreviewImage.Source = null;
            PreviewFileText.Text = "No image selected";

            _photos.Clear();
            RefreshFilters();

            List<PhotoItem> scannedPhotos = await Task.Run(
                () => _imageScanner.ScanFolder(_sourceFolder));

            // Update the WPF-bound collection on the UI thread.
            foreach (PhotoItem photo in scannedPhotos)
            {
                _photos.Add(photo);
            }

            RefreshFilters();

            if (_photos.Count == 0)
            {
                MessageBox.Show(
                    "No supported images were found in the selected folder.",
                    "Scan Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(
                "Access was denied while scanning the source folder. " +
                "Check your folder permissions.",
                "Access Denied",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while scanning the folder:\n{ex.Message}",
                "Scan Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            UpdateCount();
        }
    }

    private bool PhotoMatchesFilters(object item)
    {
        if (item is not PhotoItem photo)
            return false;

        var options = new PhotoFilterOptions
        {
            ShowCandidates = RecCandidateCheck.IsChecked == true,
            ShowManualReview = RecReviewCheck.IsChecked == true,
            ShowUnsuitable = RecUnsuitableCheck.IsChecked == true,
            ShowNotAnalyzed = RecNotAnalyzedCheck.IsChecked == true,

            ShowNoPeople = PeopleNoneCheck.IsChecked == true,
            ShowPeopleDetected = PeopleDetectedCheck.IsChecked == true,
            ShowPeopleUnclear = PeopleUnclearCheck.IsChecked == true,

            ShowReleaseNotRequired = ReleaseNotRequiredCheck.IsChecked == true,
            ShowReleaseNeedsReview = ReleaseCheckCheck.IsChecked == true,
            ShowReleaseAvailable = ReleasePresentCheck.IsChecked == true,
            ShowReleaseUnclear = ReleaseUnclearCheck.IsChecked == true,
       
            MinimumStockPotential = MinStockBox.Text,          
        };

        return _photoFilterService.Matches(photo, options);
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        RefreshFilters();
    }

    private void ScoreFilter_Changed(
        object sender,
        TextChangedEventArgs e)
    {
        if (_photosView is not null)
            RefreshFilters();
    }

    private void RefreshFilters()
    {
        if (_photosView is null)
            return;

        _photosView.Refresh();
        UpdateCount();
    }

    private void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        RecCandidateCheck.IsChecked = true;
        RecReviewCheck.IsChecked = true;
        RecUnsuitableCheck.IsChecked = false;
        RecNotAnalyzedCheck.IsChecked = true;

        PeopleNoneCheck.IsChecked = true;
        PeopleDetectedCheck.IsChecked = true;
        PeopleUnclearCheck.IsChecked = true;

        ReleaseNotRequiredCheck.IsChecked = true;
        ReleaseCheckCheck.IsChecked = true;
        ReleasePresentCheck.IsChecked = true;
        ReleaseUnclearCheck.IsChecked = true;

        MinStockBox.Text = "0";
        
        RefreshFilters();
    }

    private void ResultsGrid_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (ResultsGrid.SelectedItem is not PhotoItem photo)
        {
            PreviewImage.Source = null;
            PreviewFileText.Text = "No image selected";
            return;
        }

        PreviewFileText.Text = photo.FilePath;

        try
        {
            PreviewImage.Source = LoadBitmap(photo.FilePath, 1600);
        }
        catch (Exception ex)
        {
            PreviewImage.Source = null;
            PreviewFileText.Text =
                $"{photo.FilePath}{Environment.NewLine}" +
                $"Preview could not be loaded: {ex.Message}";
        }
    }

    private void CopySelected_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_destinationFolder))
        {
            MessageBox.Show(
                "Please select a destination folder first.",
                "Destination Folder Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!Directory.Exists(_destinationFolder))
        {
            MessageBox.Show(
                "The selected destination folder does not exist.",
                "Invalid Destination",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_sourceFolder) ||
            !Directory.Exists(_sourceFolder))
        {
            MessageBox.Show(
                "Please select a valid source folder first.",
                "Source Folder Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (IsSameOrChildPath(_destinationFolder, _sourceFolder))
        {
            MessageBox.Show(
                "The destination folder must not be inside the source folder.",
                "Invalid Destination",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        List<PhotoItem> selectedPhotos = _photos
            .Where(photo => photo.IsSelected)
            .ToList();

        if (selectedPhotos.Count == 0)
        {
            MessageBox.Show(
                "Please select at least one image to copy.",
                "No Images Selected",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            CopyResult result = _imageCopyService.CopyFiles(
                selectedPhotos,
                _destinationFolder);

            string resultMessage =
                $"Copied {result.CopiedCount} image(s) to the destination folder.";

            if (result.FailedCount > 0)
            {
                resultMessage +=
                    $"{Environment.NewLine}{result.FailedCount} image(s) could not be copied.";
            }

            MessageBox.Show(
                resultMessage,
                "Copy Complete",
                MessageBoxButton.OK,
                result.FailedCount > 0
                    ? MessageBoxImage.Warning
                    : MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while copying images:\n{ex.Message}",
                "Copy Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static bool IsSameOrChildPath(
        string candidatePath,
        string parentPath)
    {
        string normalizedCandidate = Path.GetFullPath(candidatePath)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

        string normalizedParent = Path.GetFullPath(parentPath)
            .TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

        if (string.Equals(
            normalizedCandidate,
            normalizedParent,
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string parentPrefix = normalizedParent + Path.DirectorySeparatorChar;

        return normalizedCandidate.StartsWith(
            parentPrefix,
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

    private void UpdateCount()
    {
        if (CountText is null || _photosView is null)
            return;

        int visibleCount = _photosView.Cast<object>().Count();

        CountText.Text = $"{visibleCount} of {_photos.Count} images shown";
    }
}
