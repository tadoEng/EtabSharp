using EtabSharp.Core;
using EtabSharp.Mcp.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace EtabSharp.Mcp.Tools;

/// <summary>
/// MCP tools for retrieving building information from ETABS model.
/// </summary>
[McpServerToolType]
public static class BuildingInformationTools
{
    [McpServerTool(UseStructuredContent = true)]
    [Description(
        "Get comprehensive building information including story count, heights, materials, " +
        "frame sections, area sections, load patterns, load cases, load combinations, and groups " +
        "from the active ETABS model.")]
    public static BuildingInformationResult GetBuildingInformation()
    {
        try
        {
            var etabs = ETABSWrapper.Connect();
            if (etabs == null)
            {
                return new BuildingInformationResult(
                    Success: false,
                    Error: "No active ETABS instance found. Please open ETABS first.",
                    ModelInfo: null, Stories: null, Materials: null,
                    FrameSections: null, AreaSections: null, LoadPatterns: null,
                    LoadCases: null, LoadCombinations: null, Groups: null);
            }

            var model = etabs.Model;

            var unitResult = model.Units.GetPresentUnits();
            var units = new UnitsInfo(
                unitResult.Force.ToString(),
                unitResult.Length.ToString(),
                unitResult.Temperature.ToString());

            return new BuildingInformationResult(
                Success: true,
                Error: null,
                ModelInfo: BuildModelInfo(model, units),
                Stories: BuildStoryInfo(model),
                Materials: BuildCategorySummary(model),
                FrameSections: BuildFrameSectionsSummary(model),
                AreaSections: BuildAreaSectionsSummary(model),
                LoadPatterns: BuildLoadPatternsSummary(model),
                LoadCases: BuildLoadCasesSummary(model),
                LoadCombinations: BuildLoadCombinationsSummary(model),
                Groups: BuildGroupsSummary(model)
            );
        }
        catch (Exception ex)
        {
            return new BuildingInformationResult(
                Success: false,
                Error: ex.Message,
                ModelInfo: null, Stories: null, Materials: null,
                FrameSections: null, AreaSections: null, LoadPatterns: null,
                LoadCases: null, LoadCombinations: null, Groups: null);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static ModelInfoResult BuildModelInfo(ETABSModel model, UnitsInfo units) =>
        new(
            Filename: model.ModelInfo.GetModelFilepath(),
            Version: model.ModelInfo.GetVersion(),
            Units: units
        );

    private static StoryInfoResult BuildStoryInfo(ETABSModel model)
    {
        try
        {
            var data = model.Story.GetStories();
            var totalHeight = data.StoryElevations.Length > 0
                ? data.StoryElevations.Max() - data.StoryElevations.Min()
                : 0;

            var stories = data.StoryNames.Select((name, i) => new StoryItem(
                Name: name,
                Height: data.StoryHeights[i],
                Elevation: data.StoryElevations[i],
                IsMasterStory: data.IsMasterStory[i],
                SimilarToStory: data.SimilarToStory[i]
            )).ToList();

            return new StoryInfoResult(data.StoryNames.Length, totalHeight, stories);
        }
        catch (Exception ex)
        {
            return new StoryInfoResult(0, 0, new List<StoryItem>());
        }
    }

    private static CategorySummary BuildCategorySummary(ETABSModel model)
    {
        try
        {
            var allMaterials = model.Materials.GetNameList();
            var grouped = allMaterials
                .GroupBy(name =>
                {
                    var (matType, _, _, _) = model.Materials.GetMaterial(name);
                    return matType.ToString();
                })
                .Select(g => new CategoryGroup(g.Key, g.Count(), g.ToList()))
                .ToList();

            return new CategorySummary(allMaterials.Length, grouped);
        }
        catch
        {
            return new CategorySummary(0, new List<CategoryGroup>());
        }
    }

    private static CategorySummary BuildFrameSectionsSummary(ETABSModel model)
    {
        try
        {
            var sections = model.PropFrame.GetNameList();
            var grouped = sections
                .GroupBy(s => model.PropFrame.GetSectionType(s).ToString())
                .Select(g => new CategoryGroup(g.Key, g.Count(), g.ToList()))
                .ToList();

            return new CategorySummary(sections.Length, grouped);
        }
        catch
        {
            return new CategorySummary(0, new List<CategoryGroup>());
        }
    }

    private static CategorySummary BuildAreaSectionsSummary(ETABSModel model)
    {
        try
        {
            var sections = model.PropArea.GetNameList();
            var grouped = sections
                .GroupBy(s => model.PropArea.GetPropertyType(s).ToString())
                .Select(g => new CategoryGroup(g.Key, g.Count(), g.ToList()))
                .ToList();

            return new CategorySummary(sections.Length, grouped);
        }
        catch
        {
            return new CategorySummary(0, new List<CategoryGroup>());
        }
    }

    private static CategorySummary BuildLoadPatternsSummary(ETABSModel model)
    {
        try
        {
            var patterns = model.LoadPatterns.GetNameList();
            var grouped = patterns
                .GroupBy(p => model.LoadPatterns.GetLoadType(p).ToString())
                .Select(g => new CategoryGroup(g.Key, g.Count(), g.ToList()))
                .ToList();

            return new CategorySummary(patterns.Length, grouped);
        }
        catch
        {
            return new CategorySummary(0, new List<CategoryGroup>());
        }
    }

    private static CategorySummary BuildLoadCasesSummary(ETABSModel model)
    {
        try
        {
            var cases = model.LoadCases.GetNameList();
            var grouped = cases
                .GroupBy(c =>
                {
                    var (caseType, _) = model.LoadCases.GetTypeOAPI(c);
                    return caseType.ToString();
                })
                .Select(g => new CategoryGroup(g.Key, g.Count(), g.ToList()))
                .ToList();

            return new CategorySummary(cases.Length, grouped);
        }
        catch
        {
            return new CategorySummary(0, new List<CategoryGroup>());
        }
    }

    private static LoadCombinationsResult BuildLoadCombinationsSummary(ETABSModel model)
    {
        try
        {
            var combos = model.LoadCombinations.GetNameList();
            return new LoadCombinationsResult(combos.Length, combos.ToList());
        }
        catch
        {
            return new LoadCombinationsResult(0, new List<string>());
        }
    }

    private static GroupsResult BuildGroupsSummary(ETABSModel model)
    {
        try
        {
            var groups = model.Groups.GetNameList();
            var items = groups
                .Select(name => new GroupItem(name, model.Groups.GetAssignmentCount(name)))
                .ToList();

            return new GroupsResult(groups.Length, items);
        }
        catch
        {
            return new GroupsResult(0, new List<GroupItem>());
        }
    }
}