namespace EtabSharp.Mcp.Models;

// ─── Project Summary ─────────────────────────────────────────────────────────

public record ProjectSummary(
    string FileName,
    string Version,
    UnitsInfo Units,
    int StoryCount,
    double TotalHeight,
    int MaterialCount,
    int FrameCount,
    int AreaCount,
    int LoadPatternCount,
    int LoadCaseCount,
    bool AnalysisComplete,
    BaseReactionSummary? BaseReactions
)
{
    public static ProjectSummary NotConnected() => new(
        FileName: "",
        Version: "",
        Units: new UnitsInfo("", "", ""),
        StoryCount: 0,
        TotalHeight: 0,
        MaterialCount: 0,
        FrameCount: 0,
        AreaCount: 0,
        LoadPatternCount: 0,
        LoadCaseCount: 0,
        AnalysisComplete: false,
        BaseReactions: null
    );
}

public record UnitsInfo(string Force, string Length, string Temperature);

// ─── Base Reactions ───────────────────────────────────────────────────────────

public record BaseReactionSummary(
    int Count,
    List<ReactionItem> Reactions
);

public record ReactionItem(
    string LoadCase,
    string StepType,
    double StepNumber,
    double Fx,
    double Fy,
    double Fz,
    double Mx,
    double My,
    double Mz
);

// ─── Building Information ─────────────────────────────────────────────────────

public record BuildingInformationResult(
    bool Success,
    string? Error,
    ModelInfoResult? ModelInfo,
    StoryInfoResult? Stories,
    CategorySummary? Materials,
    CategorySummary? FrameSections,
    CategorySummary? AreaSections,
    CategorySummary? LoadPatterns,
    CategorySummary? LoadCases,
    LoadCombinationsResult? LoadCombinations,
    GroupsResult? Groups
);

public record ModelInfoResult(string Filename, string Version, UnitsInfo Units);

public record StoryInfoResult(
    int Count,
    double TotalHeight,
    List<StoryItem> Stories
);

public record StoryItem(
    string Name,
    double Height,
    double Elevation,
    bool IsMasterStory,
    string SimilarToStory
);

public record CategorySummary(
    int TotalCount,
    List<CategoryGroup> ByType
);

public record CategoryGroup(string Type, int Count, List<string> Items);

public record LoadCombinationsResult(int TotalCount, List<string> Combinations);

public record GroupsResult(int TotalCount, List<GroupItem> Groups);

public record GroupItem(string Name, int ObjectCount);

// ─── Analysis Results ─────────────────────────────────────────────────────────

public record FrameForcesResult(
    bool Success,
    string? Error,
    int TotalResults,
    int ResultsShown,
    string? Note,
    List<FrameForceItem>? Forces
);

public record FrameForceItem(
    string Frame,
    string Element,
    string LoadCase,
    string StepType,
    double ObjectStation,
    double ElementStation,
    ForceComponents Forces
);

public record ForceComponents(
    double Axial,
    double Shear2,
    double Shear3,
    double Torsion,
    double Moment2,
    double Moment3
);

public record JointDisplacementsResult(
    bool Success,
    string? Error,
    int TotalResults,
    int ResultsShown,
    string? Note,
    List<JointDisplacementItem>? Displacements
);

public record JointDisplacementItem(
    string Point,
    string Element,
    string LoadCase,
    string StepType,
    double StepNumber,
    DisplacementComponents Displacements
);

public record DisplacementComponents(
    double Ux, double Uy, double Uz,
    double Rx, double Ry, double Rz
);

public record StoryDriftsResult(
    bool Success,
    string? Error,
    int Count,
    List<StoryDriftItem>? Drifts
);

public record StoryDriftItem(
    string Story,
    string LoadCase,
    string StepType,
    string Direction,
    double Drift,
    string Label,
    LocationXYZ Location
);

public record LocationXYZ(double X, double Y, double Z);

public record ModalResultsData(
    bool Success,
    string? Error,
    int TotalModes,
    List<ModeItem>? Modes,
    List<ParticipationFactorItem>? ParticipationFactors,
    List<MassRatioItem>? MassRatios
);

public record ModeItem(
    string LoadCase,
    double Mode,
    double Period,
    double Frequency,
    double CircularFrequency,
    double Eigenvalue
);

public record ParticipationFactorItem(
    string LoadCase,
    double Mode,
    double Period,
    double Ux, double Uy, double Uz,
    double Rx, double Ry, double Rz
);

public record MassRatioItem(
    string LoadCase,
    double Mode,
    double Period,
    double Ux, double Uy, double Uz,
    double SumUx, double SumUy, double SumUz
);