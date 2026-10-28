using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace StockPhotoScout.Models;

public class PhotoItem : INotifyPropertyChanged
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public BitmapImage? Thumbnail { get; set; }

    private int _stockPotential;
    public int StockPotential
    {
        get => _stockPotential;
        set
        {
            if (_stockPotential == value)
                return;

            _stockPotential = value;
            OnPropertyChanged();
        }
    }

    private string _peopleStatus = "Noch nicht analysiert";
    public string PeopleStatus
    {
        get => _peopleStatus;
        set
        {
            if (_peopleStatus == value)
                return;

            _peopleStatus = value;
            OnPropertyChanged();
        }
    }

    private string _releaseStatus = "Unklar";
    public string ReleaseStatus
    {
        get => _releaseStatus;
        set
        {
            if (_releaseStatus == value)
                return;

            _releaseStatus = value;
            OnPropertyChanged();
        }
    }

    private string _recommendation = "Noch nicht analysiert";
    public string Recommendation
    {
        get => _recommendation;
        set
        {
            if (_recommendation == value)
                return;

            _recommendation = value;
            OnPropertyChanged();
        }
    }

    private string _explanation = "";
    public string Explanation
    {
        get => _explanation;
        set
        {
            if (_explanation == value)
                return;

            _explanation = value;
            OnPropertyChanged();
        }
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
            OnPropertyChanged();
        }
    }

    private bool _isAnalyzed;
    public bool IsAnalyzed
    {
        get => _isAnalyzed;
        set
        {
            if (_isAnalyzed == value)
                return;

            _isAnalyzed = value;
            OnPropertyChanged();
        }
    }

    public bool HasPeople { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this, new PropertyChangedEventArgs(propertyName));
    }
}