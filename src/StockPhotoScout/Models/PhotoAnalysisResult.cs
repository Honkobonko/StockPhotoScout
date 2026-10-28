namespace StockPhotoScout.Models;

public sealed class PhotoAnalysisResult
{
    /// <summary>
    /// Geschätztes Potenzial des Bildes für Stock-Plattformen (0–10).
    /// </summary>
    public int StockPotential { get; set; }

    /// <summary>
    /// Einschätzung, ob identifizierbare Personen zu sehen sind.
    /// Zulässige Werte: None Detected, People Detected, Identifiability Unclear.
    /// </summary>
    public string PeopleStatus { get; set; } = "Identifiability Unclear";

    /// <summary>
    /// Vorläufige Einschätzung zur Notwendigkeit und Verfügbarkeit von Releases.
    /// Zulässige Werte: Not Required (Provisional), Release Needs Review,
    /// Release Available, Unclear.
    /// </summary>
    public string ReleaseStatus { get; set; } = "Unclear";

    /// <summary>
    /// Empfehlung für die weitere Bearbeitung.
    /// Zulässige Werte: Candidate, Manual Review, Likely Unsuitable.
    /// </summary>
    public string Recommendation { get; set; } = "Manual Review";

    /// <summary>
    /// Kurze Begründung für die Einschätzung.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
}