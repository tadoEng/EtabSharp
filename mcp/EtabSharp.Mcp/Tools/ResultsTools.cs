using EtabSharp.Core;
using EtabSharp.Loads.LoadCases.Models;
using EtabSharp.Mcp.Models;
using ETABSv1;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace EtabSharp.Mcp.Tools;

/// <summary>
/// MCP tools for retrieving analysis results from ETABS.
/// </summary>
[McpServerToolType]
public static class ResultsTools
{
    // ── Output setup helper ───────────────────────────────────────────────────

    private static void EnsureOutputSetup(ETABSApplication etabs, string[]? caseNames, string[]? comboNames)
    {
        var setup = etabs.Model.AnalysisResultsSetup;

        if (caseNames is { Length: > 0 })
            foreach (var c in caseNames)
                setup.SetCaseSelectedForOutput(c, true);
        else
            setup.SetCaseSelectedForOutput("all", true);

        if (comboNames is { Length: > 0 })
            foreach (var c in comboNames)
                setup.SetComboSelectedForOutput(c, true);
        else
            setup.SetComboSelectedForOutput("all", true);
    }

    private static (string[]? caseNames, string[]? comboNames) ParseFilters(string cases, string combos)
    {
        string[]? caseNames = cases.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : cases.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToArray();

        string[]? comboNames = combos.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : combos.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToArray();

        return (caseNames, comboNames);
    }

    // ── Tools ─────────────────────────────────────────────────────────────────

    [McpServerTool(UseStructuredContent = true)]
    [Description(
        "Get base reactions for all load cases and combinations including forces and moments " +
        "at the base of the structure. Requires analysis to have been run.")]
    public static BaseReactionSummary GetBaseReactions(
        [Description("Comma-separated load case names, or 'all'")] string cases = "all",
        [Description("Comma-separated combo names, or 'all'")] string combos = "all")
    {
        var etabs = ETABSWrapper.Connect();
        if (etabs == null)
            return new BaseReactionSummary(0, new List<ReactionItem>());

        var (caseNames, comboNames) = ParseFilters(cases, combos);
        EnsureOutputSetup(etabs, caseNames, comboNames);

        var results = etabs.Model.AnalysisResults.GetBaseReact();

        var reactions = results.Results.Select(r => new ReactionItem(
            LoadCase: r.LoadCase,
            StepType: r.StepType,
            StepNumber: r.StepNum,
            Fx: r.FX, Fy: r.FY, Fz: r.FZ,
            Mx: r.MX, My: r.MY, Mz: r.MZ
        )).ToList();

        return new BaseReactionSummary(reactions.Count, reactions);
    }

    [McpServerTool(UseStructuredContent = true)]
    [Description(
        "Get joint displacements for specified points or all points. " +
        "Requires analysis to have been run.")]
    public static JointDisplacementsResult GetJointDisplacements(
        [Description("Joint/point name, or 'all'")] string pointName = "all",
        [Description("Comma-separated load case names, or 'all'")] string cases = "all",
        [Description("Comma-separated combo names, or 'all'")] string combos = "all")
    {
        try
        {
            var etabs = ETABSWrapper.Connect();
            if (etabs == null)
                return new JointDisplacementsResult(false, "No active ETABS instance found.", 0, 0, null, null);

            var (caseNames, comboNames) = ParseFilters(cases, combos);
            EnsureOutputSetup(etabs, caseNames, comboNames);

            var name = pointName.Equals("all", StringComparison.OrdinalIgnoreCase) ? "" : pointName;
            var results = etabs.Model.AnalysisResults.GetJointDispl(name, eItemTypeElm.ObjectElm);
            const int limit = 200;

            var displacements = results.Results.Take(limit).Select(r => new JointDisplacementItem(
                Point: r.ObjectName,
                Element: r.ElementName,
                LoadCase: r.LoadCase,
                StepType: r.StepType,
                StepNumber: r.StepNum,
                Displacements: new DisplacementComponents(r.U1, r.U2, r.U3, r.R1, r.R2, r.R3)
            )).ToList();

            return new JointDisplacementsResult(
                Success: true,
                Error: null,
                TotalResults: results.Results.Count,
                ResultsShown: displacements.Count,
                Note: results.Results.Count > limit ? $"Showing first {limit} results only" : null,
                Displacements: displacements
            );
        }
        catch (Exception ex)
        {
            return new JointDisplacementsResult(false, ex.Message, 0, 0, null, null);
        }
    }

    [McpServerTool(UseStructuredContent = true)]
    [Description(
        "Get frame forces (axial, shear, moment) for specified frames or all frames. " +
        "Requires analysis to have been run.")]
    public static FrameForcesResult GetFrameForces(
        [Description("Frame element name, or 'all'")] string frameName = "all",
        [Description("Comma-separated load case names, or 'all'")] string cases = "all",
        [Description("Comma-separated combo names, or 'all'")] string combos = "all")
    {
        try
        {
            var etabs = ETABSWrapper.Connect();
            if (etabs == null)
                return new FrameForcesResult(false, "No active ETABS instance found.", 0, 0, null, null);

            var (caseNames, comboNames) = ParseFilters(cases, combos);
            EnsureOutputSetup(etabs, caseNames, comboNames);

            var name = frameName.Equals("all", StringComparison.OrdinalIgnoreCase) ? "" : frameName;
            var results = etabs.Model.AnalysisResults.GetFrameForce(name, eItemTypeElm.ObjectElm);
            const int limit = 200;

            var forces = results.Results.Take(limit).Select(r => new FrameForceItem(
                Frame: r.ObjectName,
                Element: r.ElementName,
                LoadCase: r.LoadCase,
                StepType: r.StepType,
                ObjectStation: r.ObjectStation,
                ElementStation: r.ElementStation,
                Forces: new ForceComponents(r.P, r.V2, r.V3, r.T, r.M2, r.M3)
            )).ToList();

            return new FrameForcesResult(
                Success: true,
                Error: null,
                TotalResults: results.Results.Count,
                ResultsShown: forces.Count,
                Note: results.Results.Count > limit ? $"Showing first {limit} results only" : null,
                Forces: forces
            );
        }
        catch (Exception ex)
        {
            return new FrameForcesResult(false, ex.Message, 0, 0, null, null);
        }
    }

    [McpServerTool(UseStructuredContent = true)]
    [Description(
        "Get story drifts for all stories including maximum drift ratios. " +
        "Requires analysis to have been run.")]
    public static StoryDriftsResult GetStoryDrifts(
        [Description("Comma-separated load case names, or 'all'")] string cases = "all",
        [Description("Comma-separated combo names, or 'all'")] string combos = "all")
    {
        try
        {
            var etabs = ETABSWrapper.Connect();
            if (etabs == null)
                return new StoryDriftsResult(false, "No active ETABS instance found.", 0, null);

            var (caseNames, comboNames) = ParseFilters(cases, combos);
            EnsureOutputSetup(etabs, caseNames, comboNames);

            var results = etabs.Model.AnalysisResults.GetStoryDrifts();

            var drifts = results.Results.Select(r => new StoryDriftItem(
                Story: r.Story,
                LoadCase: r.LoadCase,
                StepType: r.StepType,
                Direction: r.Direction,
                Drift: r.Drift,
                Label: r.Label,
                Location: new LocationXYZ(r.X, r.Y, r.Z)
            )).ToList();

            return new StoryDriftsResult(true, null, drifts.Count, drifts);
        }
        catch (Exception ex)
        {
            return new StoryDriftsResult(false, ex.Message, 0, null);
        }
    }

    [McpServerTool(UseStructuredContent = true)]
    [Description(
        "Get modal analysis results including periods, frequencies, and modal participation " +
        "mass ratios. Requires a modal load case to have been run.")]
    public static ModalResultsData GetModalResults(
        [Description("Comma-separated modal case names, or 'all'")] string modalCases = "all")
    {
        try
        {
            var etabs = ETABSWrapper.Connect();
            if (etabs == null)
                return new ModalResultsData(false, "No active ETABS instance found.", 0, null, null, null);

            string[]? caseNames = modalCases.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? null
                : modalCases.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToArray();

            EnsureOutputSetup(etabs, caseNames, null);

            var model = etabs.Model;
            var periods = model.AnalysisResults.GetModalPeriod();
            var participation = model.AnalysisResults.GetModalParticipationFactors();
            var massRatios = model.AnalysisResults.GetModalParticipatingMassRatios();

            var modes = periods.Results.Select(r => new ModeItem(
                r.LoadCase, r.StepNum, r.Period, r.Frequency, r.CircularFrequency, r.EigenValue
            )).ToList();

            var factors = participation.Results.Select(r => new ParticipationFactorItem(
                r.LoadCase, r.StepNum, r.Period,
                r.UX, r.UY, r.UZ, r.RX, r.RY, r.RZ
            )).ToList();

            var ratios = massRatios.Results.Select(r => new MassRatioItem(
                r.LoadCase, r.StepNum, r.Period,
                r.UX, r.UY, r.UZ, r.SumUX, r.SumUY, r.SumUZ
            )).ToList();

            return new ModalResultsData(true, null, modes.Count, modes, factors, ratios);
        }
        catch (Exception ex)
        {
            return new ModalResultsData(false, ex.Message, 0, null, null, null);
        }
    }

    [McpServerTool(UseStructuredContent = true)]
    [Description("Set which load cases and combinations are selected for output retrieval.")]
    public static object SetOutputSelection(
        [Description("Comma-separated case names, or 'all'")] string cases = "all",
        [Description("Comma-separated combo names, or 'all'")] string combos = "all",
        [Description("True to select, false to deselect")] bool select = true)
    {
        try
        {
            var etabs = ETABSWrapper.Connect();
            if (etabs == null)
                return new { Success = false, Error = "No active ETABS instance found." };

            var setup = etabs.Model.AnalysisResultsSetup;
            int casesProcessed = 0, combosProcessed = 0;

            if (cases.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                casesProcessed = setup.SetCaseSelectedForOutput("all", select);
            }
            else
            {
                foreach (var c in cases.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                    if (setup.SetCaseSelectedForOutput(c, select) == 0) casesProcessed++;
            }

            if (combos.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                combosProcessed = setup.SetComboSelectedForOutput("all", select);
            }
            else
            {
                foreach (var c in combos.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                    if (setup.SetComboSelectedForOutput(c, select) == 0) combosProcessed++;
            }

            return new
            {
                Success = true,
                Action = select ? "selected" : "deselected",
                CasesProcessed = casesProcessed,
                CombosProcessed = combosProcessed
            };
        }
        catch (Exception ex)
        {
            return new { Success = false, Error = ex.Message };
        }
    }
}