using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace StockPhotoScout;

public class PhotoItem : INotifyPropertyChanged
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public BitmapImage? Thumbnail { get; set; }

    private int _technicalQuality;
    public int TechnicalQuality
    {
        get => _technicalQuality;
        set { _technicalQuality = value; OnPropertyChanged(); }
    }

    private int _stockPotential;
    public int StockPotential
    {
        get => _stockPotential;
        set { _stockPotential = value; OnPropertyChanged(); }
    }

    private string _peopleStatus = "Noch nicht analysiert";
    public string PeopleStatus
    {
        get => _peopleStatus;
        set { _peopleStatus = value; OnPropertyChanged(); }
    }

    private string _releaseStatus = "Unklar";
    public string ReleaseStatus
    {
        get => _releaseStatus;
        set { _releaseStatus = value; OnPropertyChanged(); }
    }

    private string _recommendation = "Noch nicht analysiert";
    public string Recommendation
    {
        get => _recommendation;
        set { _recommendation = value; OnPropertyChanged(); }
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public bool IsAnalyzed { get; set; }
    public bool HasPeople { get; set; }
    public bool EditorialPotential { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this, new PropertyChangedEventArgs(propertyName));
    }
}
