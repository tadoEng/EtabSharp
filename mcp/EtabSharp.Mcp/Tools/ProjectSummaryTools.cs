using EtabSharp.Core;
using EtabSharp.Mcp.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace EtabSharp.Mcp.Tools;

/// <summary>
/// High-level MCP tools for a quick project overview — ideal for demo and onboarding workflows.
/// Combines building geometry + analysis results into a single structured response.
/// </summary>
[McpServerToolType]
public static class ProjectSummaryTools
{
    [McpServerTool(UseStructuredContent = true)]
    [Description(
        "Get a complete project summary including model filename, story count, total height, " +
        "material/section/element counts, and base reactions if analysis has already been run. " +
        "Use this as the first tool to call to understand a model before diving into details.")]
    public static ProjectSummary GetProjectSummary()
    {
        var etabs = ETABSWrapper.Connect();
        if (etabs == null)
            return ProjectSummary.NotConnected();

        var model = etabs.Model;

        // ── Model meta ──────────────────────────────────────────────────────
        var filename = model.ModelInfo.GetModelFilepath();
        var version = model.ModelInfo.GetVersion();
        var unitResult = model.Units.GetPresentUnits();
        var units = new UnitsInfo(
            unitResult.Force.ToString(),
            unitResult.Length.ToString(),
            unitResult.Temperature.ToString());

        // ── Geometry counts ─────────────────────────────────────────────────
        var storyData = model.Story.GetStories();
        var storyCount = storyData.StoryNames.Length;
        var totalHeight = storyData.StoryElevations.Length > 0
            ? storyData.StoryElevations.Max() - storyData.StoryElevations.Min()
            : 0;

        var materialCount = model.Materials.GetNameList().Length;
        var frameCount = model.Frames.GetNameList().Length;
        var areaCount = model.Areas.GetNameList().Length;
        var loadPatternCount = model.LoadPatterns.GetNameList().Length;
        var loadCaseCount = model.LoadCases.GetNameList().Length;

        // ── Base reactions (only if analysis has been run) ──────────────────
        BaseReactionSummary? reactions = null;
        try
        {
            model.AnalysisResultsSetup.SetCaseSelectedForOutput("all", true);
            model.AnalysisResultsSetup.SetComboSelectedForOutput("all", true);

            var react = model.AnalysisResults.GetBaseReact();
            if (react.Results.Count > 0)
            {
                reactions = new BaseReactionSummary(
                    Count: react.Results.Count,
                    Reactions: react.Results.Select(r => new ReactionItem(
                        LoadCase: r.LoadCase,
                        StepType: r.StepType,
                        StepNumber: r.StepNum,
                        Fx: r.FX, Fy: r.FY, Fz: r.FZ,
                        Mx: r.MX, My: r.MY, Mz: r.MZ
                    )).ToList()
                );
            }
        }
        catch
        {
            // Analysis not run yet — that's fine, reactions stays null
        }

        return new ProjectSummary(
            FileName: System.IO.Path.GetFileName(filename),
            Version: version,
            Units: units,
            StoryCount: storyCount,
            TotalHeight: totalHeight,
            MaterialCount: materialCount,
            FrameCount: frameCount,
            AreaCount: areaCount,
            LoadPatternCount: loadPatternCount,
            LoadCaseCount: loadCaseCount,
            AnalysisComplete: reactions != null,
            BaseReactions: reactions
        );
    }
}