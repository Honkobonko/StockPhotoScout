namespace StockPhotoScout.Services;

public sealed class PhotoFilterService
{
    public bool Matches(PhotoItem photo, PhotoFilterOptions options)
    {
        if (!MatchesRecommendation(photo, options))
            return false;

        if (!MatchesPeopleStatus(photo, options))
            return false;

        if (!MatchesReleaseStatus(photo, options))
            return false;

        if (photo.TechnicalQuality < ReadScore(options.MinimumTechnicalQuality))
            return false;

        if (photo.StockPotential < ReadScore(options.MinimumStockPotential))
            return false;

        if (options.EditorialOnly && !photo.EditorialPotential)
            return false;

        return true;
    }

    private static bool MatchesRecommendation(
        PhotoItem photo,
        PhotoFilterOptions options)
    {
        bool anySelected =
            options.ShowCandidates ||
            options.ShowManualReview ||
            options.ShowUnsuitable ||
            options.ShowNotAnalyzed;

        if (!anySelected)
            return false;

        return
            (options.ShowCandidates &&
             photo.Recommendation == "Candidate") ||

            (options.ShowManualReview &&
             photo.Recommendation == "Manual Review") ||

            (options.ShowUnsuitable &&
             photo.Recommendation == "Likely Unsuitable") ||

            (options.ShowNotAnalyzed &&
             !photo.IsAnalyzed);
    }

    private static bool MatchesPeopleStatus(
        PhotoItem photo,
        PhotoFilterOptions options)
    {
        bool anySelected =
            options.ShowNoPeople ||
            options.ShowPeopleDetected ||
            options.ShowPeopleUnclear;

        if (!anySelected)
            return false;

        return
            (options.ShowNoPeople &&
             photo.PeopleStatus == "None Detected") ||

            (options.ShowPeopleDetected &&
             photo.PeopleStatus == "People Detected") ||

            (options.ShowPeopleUnclear &&
             photo.PeopleStatus == "Identifiability Unclear");
    }

    private static bool MatchesReleaseStatus(
        PhotoItem photo,
        PhotoFilterOptions options)
    {
        bool anySelected =
            options.ShowReleaseNotRequired ||
            options.ShowReleaseNeedsReview ||
            options.ShowReleaseAvailable ||
            options.ShowReleaseUnclear;

        if (!anySelected)
            return false;

        return
            (options.ShowReleaseNotRequired &&
             photo.ReleaseStatus == "Not Required (Provisional)") ||

            (options.ShowReleaseNeedsReview &&
             photo.ReleaseStatus == "Release Needs Review") ||

            (options.ShowReleaseAvailable &&
             photo.ReleaseStatus == "Release Available") ||

            (options.ShowReleaseUnclear &&
             photo.ReleaseStatus == "Unclear");
    }

    private static int ReadScore(string? text)
    {
        if (!int.TryParse(text, out int score))
            return 0;

        return Math.Clamp(score, 0, 100);
    }
}

public sealed class PhotoFilterOptions
{
    public bool ShowCandidates { get; init; }
    public bool ShowManualReview { get; init; }
    public bool ShowUnsuitable { get; init; }
    public bool ShowNotAnalyzed { get; init; }

    public bool ShowNoPeople { get; init; }
    public bool ShowPeopleDetected { get; init; }
    public bool ShowPeopleUnclear { get; init; }

    public bool ShowReleaseNotRequired { get; init; }
    public bool ShowReleaseNeedsReview { get; init; }
    public bool ShowReleaseAvailable { get; init; }
    public bool ShowReleaseUnclear { get; init; }

    public string? MinimumTechnicalQuality { get; init; }
    public string? MinimumStockPotential { get; init; }

    public bool EditorialOnly { get; init; }
}
