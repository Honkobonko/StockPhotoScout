using StockPhotoScout;
using StockPhotoScout.Services;
using Xunit;

namespace StockPhotoScout.Tests;

public sealed class PhotoFilterServiceTests
{
    private readonly PhotoFilterService _service = new();

    [Fact]
    public void Matches_ReturnsTrue_WhenAllSelectedFiltersMatch()
    {
        var photo = CreatePhoto(
            recommendation: "Candidate",
            peopleStatus: "None Detected",
            releaseStatus: "Release Available",
            technicalQuality: 80,
            stockPotential: 75);

        var options = CreateOptions();

        bool result = _service.Matches(photo, options);

        Assert.True(result);
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenRecommendationDoesNotMatch()
    {
        var photo = CreatePhoto(
            recommendation: "Likely Unsuitable");

        var options = CreateOptions();

        bool result = _service.Matches(photo, options);

        Assert.False(result);
    }

    [Fact]
    public void Matches_ReturnsTrue_WhenPhotoIsNotAnalyzedAndFilterIsEnabled()
    {
        var photo = CreatePhoto(
            recommendation: "Other",
            isAnalyzed: false);

        var options = CreateOptions(
            showNotAnalyzed: true);

        bool result = _service.Matches(photo, options);

        Assert.True(result);
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenNoRecommendationFilterIsSelected()
    {
        var photo = CreatePhoto();

        var options = CreateOptions(
            showCandidates: false,
            showManualReview: false,
            showUnsuitable: false,
            showNotAnalyzed: false);

        bool result = _service.Matches(photo, options);

        Assert.False(result);
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenPeopleFilterDoesNotMatch()
    {
        var photo = CreatePhoto(
            peopleStatus: "People Detected");

        var options = CreateOptions();

        bool result = _service.Matches(photo, options);

        Assert.False(result);
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenReleaseFilterDoesNotMatch()
    {
        var photo = CreatePhoto(
            releaseStatus: "Unclear");

        var options = CreateOptions();

        bool result = _service.Matches(photo, options);

        Assert.False(result);
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenTechnicalQualityIsBelowMinimum()
    {
        var photo = CreatePhoto(technicalQuality: 49);

        var options = CreateOptions(
            minimumTechnicalQuality: "50");

        bool result = _service.Matches(photo, options);

        Assert.False(result);
    }

    [Fact]
    public void Matches_ReturnsTrue_WhenScoresEqualMinimumValues()
    {
        var photo = CreatePhoto(
            technicalQuality: 50,
            stockPotential: 60);

        var options = CreateOptions(
            minimumTechnicalQuality: "50",
            minimumStockPotential: "60");

        bool result = _service.Matches(photo, options);

        Assert.True(result);
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenStockPotentialIsBelowMinimum()
    {
        var photo = CreatePhoto(stockPotential: 59);

        var options = CreateOptions(
            minimumStockPotential: "60");

        bool result = _service.Matches(photo, options);

        Assert.False(result);
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenEditorialOnlyIsEnabledForNonEditorialPhoto()
    {
        var photo = CreatePhoto(editorialPotential: false);

        var options = CreateOptions(editorialOnly: true);

        bool result = _service.Matches(photo, options);

        Assert.False(result);
    }

    [Fact]
    public void Matches_ReturnsTrue_WhenEditorialOnlyIsEnabledForEditorialPhoto()
    {
        var photo = CreatePhoto(editorialPotential: true);

        var options = CreateOptions(editorialOnly: true);

        bool result = _service.Matches(photo, options);

        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-10")]
    public void Matches_HandlesInvalidOrLowTechnicalMinimum(string? minimum)
    {
        var photo = CreatePhoto(technicalQuality: 0);

        var options = CreateOptions(
            minimumTechnicalQuality: minimum);

        bool result = _service.Matches(photo, options);

        Assert.True(result);
    }

    [Fact]
    public void Matches_ClampsTechnicalMinimumAbove100To100()
    {
        var photo = CreatePhoto(technicalQuality: 100);

        var options = CreateOptions(
            minimumTechnicalQuality: "150");

        bool result = _service.Matches(photo, options);

        Assert.True(result);
    }

    private static PhotoItem CreatePhoto(
        string recommendation = "Candidate",
        string peopleStatus = "None Detected",
        string releaseStatus = "Release Available",
        int technicalQuality = 80,
        int stockPotential = 75,
        bool isAnalyzed = true,
        bool editorialPotential = true)
    {
        return new PhotoItem
        {
            Recommendation = recommendation,
            PeopleStatus = peopleStatus,
            ReleaseStatus = releaseStatus,
            TechnicalQuality = technicalQuality,
            StockPotential = stockPotential,
            IsAnalyzed = isAnalyzed,
            EditorialPotential = editorialPotential
        };
    }

    private static PhotoFilterOptions CreateOptions(
        bool showCandidates = true,
        bool showManualReview = true,
        bool showUnsuitable = false,
        bool showNotAnalyzed = false,
        bool showNoPeople = true,
        bool showPeopleDetected = false,
        bool showPeopleUnclear = false,
        bool showReleaseNotRequired = false,
        bool showReleaseNeedsReview = false,
        bool showReleaseAvailable = true,
        bool showReleaseUnclear = false,
        string? minimumTechnicalQuality = "0",
        string? minimumStockPotential = "0",
        bool editorialOnly = false)
    {
        return new PhotoFilterOptions
        {
            ShowCandidates = showCandidates,
            ShowManualReview = showManualReview,
            ShowUnsuitable = showUnsuitable,
            ShowNotAnalyzed = showNotAnalyzed,
            ShowNoPeople = showNoPeople,
            ShowPeopleDetected = showPeopleDetected,
            ShowPeopleUnclear = showPeopleUnclear,
            ShowReleaseNotRequired = showReleaseNotRequired,
            ShowReleaseNeedsReview = showReleaseNeedsReview,
            ShowReleaseAvailable = showReleaseAvailable,
            ShowReleaseUnclear = showReleaseUnclear,
            MinimumTechnicalQuality = minimumTechnicalQuality,
            MinimumStockPotential = minimumStockPotential,
            EditorialOnly = editorialOnly
        };
    }
}
