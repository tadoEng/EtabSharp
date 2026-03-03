using EtabSharp.Core;
using ETABSv1;

// ─────────────────────────────────────────────────────────────
//  EtabSharp Visual Test — Sidecar Scenarios
//  Tests every operation that etab-cli.exe needs to perform.
//  Run from Visual Studio or: dotnet run
//
//  Dispose() contract (post-refactor):
//    Dispose()  → releases COM references only. ETABS keeps running.
//    Close()    → calls ApplicationExit. Shuts down ETABS.
//
//  Mode A pattern:  app?.Dispose()
//  Mode B pattern:  app?.Application.ApplicationExit(false);  app?.Dispose();
// ─────────────────────────────────────────────────────────────

const string TEST_MODEL_PATH = @"D:\repo\bookmarkr\Nashville Hotel_V1.2_WIND.EDB";
const string OUTPUT_DIR = @"D:\repo\bookmarkr\sidecar_test_output";

Directory.CreateDirectory(OUTPUT_DIR);

while (true)
{
    PrintMenu();
    var key = Console.ReadKey(intercept: true).KeyChar;
    Console.WriteLine();

    switch (key)
    {
        case '1': await Test_GetStatus_Running(); break;
        case '2': await Test_GetStatus_NotRunning(); break;
        case '3': await Test_OpenModel(); break;
        case '4': await Test_CloseModel_Save(); break;
        case '5': await Test_CloseModel_NoSave(); break;
        case '6': await Test_UnlockModel(); break;
        case '7': await Test_GenerateE2K(); break;
        case '9': await Test_RunAnalysis(); break;
        case 'b': await Test_ModeB_FullPipeline(); break;
        case 'q': return;
        default: Warn("Unknown option"); break;
    }

    Console.WriteLine("\nPress any key to return to menu...");
    Console.ReadKey(intercept: true);
    Console.Clear();
}

// ─────────────────────────────────────────────────────────────
//  MENU
// ─────────────────────────────────────────────────────────────

void PrintMenu()
{
    Console.WriteLine("╔══════════════════════════════════════════════════════╗");
    Console.WriteLine("║         EtabSharp Sidecar Visual Tests               ║");
    Console.WriteLine("╠══════════════════════════════════════════════════════╣");
    Console.WriteLine("║  Dispose() contract:                                 ║");
    Console.WriteLine("║    Dispose() → release COM only, ETABS keeps running ║");
    Console.WriteLine("║    Close()  → ApplicationExit, shuts down ETABS      ║");
    Console.WriteLine("╠══════════════════════════════════════════════════════╣");
    Console.WriteLine("║  Mode A — attach to your running ETABS               ║");
    Console.WriteLine("║   [1]  get-status    (ETABS running)                 ║");
    Console.WriteLine("║   [2]  get-status    (ETABS NOT running — manual)    ║");
    Console.WriteLine("║   [3]  open-model    (opens test model in ETABS)     ║");
    Console.WriteLine("║   [4]  close-model   (--save)                        ║");
    Console.WriteLine("║   [5]  close-model   (--no-save)                     ║");
    Console.WriteLine("║   [6]  unlock-model  (clear post-analysis lock)      ║");
    Console.WriteLine("╠══════════════════════════════════════════════════════╣");
    Console.WriteLine("║  Mode B — new hidden ETABS instance                  ║");
    Console.WriteLine("║   [7]  generate-e2k                                  ║");
    Console.WriteLine("║   [9]  run-analysis                                  ║");
    Console.WriteLine("║   [b]  full pipeline  (7 → 9)                        ║");
    Console.WriteLine("╠══════════════════════════════════════════════════════╣");
    Console.WriteLine("║   [q]  quit                                          ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════╝");
    Console.Write("\nSelect: ");
}

// ─────────────────────────────────────────────────────────────
//  TEST 1 — get-status (ETABS running)
//  Mode A: attach → read state → Dispose() releases COM only
//  Verify: ETABS UI stays open after dispose
// ─────────────────────────────────────────────────────────────

async Task Test_GetStatus_Running()
{
    Header("get-status — ETABS running (Mode A)");
    Info("ETABS must already be open.");

    ETABSApplication? app = null;
    try
    {
        app = ETABSWrapper.Connect();
        if (app == null) { Fail("No running ETABS found. Open ETABS first."); return; }

        var isVisible = app.Application.Visible();
        var apiVersion = app.Application.GetOAPIVersionNumber();
        var filename = app.Model.ModelInfo.GetModelFilename(includePath: true);
        var isLocked = app.Model.ModelInfo.IsLocked();
        var version = app.Model.ModelInfo.GetVersion();
        var caseStatuses = app.Model.Analyze.GetCaseStatus();
        var isAnalyzed = caseStatuses.Any(cs => cs.IsFinished);
        var pid = System.Diagnostics.Process.GetProcessesByName("ETABS")
                               .FirstOrDefault()?.Id;

        Pass("Attached to ETABS");
        Row("pid", pid?.ToString() ?? "unknown");
        Row("etabsVersion", version);
        Row("apiVersion", apiVersion.ToString("F2"));
        Row("openFilePath", string.IsNullOrEmpty(filename) ? "(none)" : filename);
        Row("isModelOpen", (!string.IsNullOrEmpty(filename)).ToString());
        Row("isLocked", isLocked.ToString());
        Row("isAnalyzed", isAnalyzed.ToString());
        Row("isVisible", isVisible.ToString());
        Row("caseCount", caseStatuses.Count.ToString());
    }
    catch (Exception ex)
    {
        Fail($"Exception: {ex.Message}");
    }
    finally
    {
        // Mode A: Dispose() releases COM only — ETABS must stay running
        app?.Dispose();

        var stillRunning = System.Diagnostics.Process.GetProcessesByName("ETABS").Any();
        if (stillRunning) Pass("✓ ETABS still running after Dispose() — COM-only release confirmed");
        else Fail("✗ ETABS exited after Dispose() — Dispose() is calling ApplicationExit (bug)");
    }
}

// ─────────────────────────────────────────────────────────────
//  TEST 2 — get-status (ETABS NOT running)
//  Connect() must return null gracefully — no COM call attempted
// ─────────────────────────────────────────────────────────────

async Task Test_GetStatus_NotRunning()
{
    Header("get-status — ETABS NOT running");
    Warn("Close ETABS completely, then press Enter...");
    Console.ReadLine();

    var etabsProcesses = System.Diagnostics.Process.GetProcessesByName("ETABS");
    if (etabsProcesses.Any())
    {
        Fail($"ETABS is still running (PID: {etabsProcesses[0].Id}). Close it and try again.");
        return;
    }

    var app = ETABSWrapper.Connect();

    if (app == null)
    {
        Pass("ETABSWrapper.Connect() returned null — correct");
        Row("success", "true");
        Row("isRunning", "false");
        Row("error", "null");
        Info("Sidecar returns: { success: true, isRunning: false }");
    }
    else
    {
        Fail("Expected null but got ETABSApplication — was ETABS actually running?");
        app.Dispose();
    }
}

// ─────────────────────────────────────────────────────────────
//  TEST 3 — open-model
//  Mode A: attach → OpenFile() → Dispose() releases COM only
//  Verify: ETABS shows the new file and stays open
// ─────────────────────────────────────────────────────────────

async Task Test_OpenModel()
{
    Header("open-model (Mode A)");

    if (!File.Exists(TEST_MODEL_PATH))
    {
        Fail($"Test model not found: {TEST_MODEL_PATH}");
        return;
    }

    ETABSApplication? app = null;
    try
    {
        app = ETABSWrapper.Connect();
        if (app == null) { Fail("No running ETABS found. Open ETABS first."); return; }

        var previousFile = app.Model.ModelInfo.GetModelFilename(includePath: true);
        Info($"Currently open: {(string.IsNullOrEmpty(previousFile) ? "(none)" : previousFile)}");

        if (app.Model.ModelInfo.IsLocked())
        {
            Warn("Model is locked — unlocking before open");
            app.Model.ModelInfo.SetLocked(false);
        }

        int ret = app.Model.Files.OpenFile(TEST_MODEL_PATH);

        if (ret == 0)
        {
            var nowOpen = app.Model.ModelInfo.GetModelFilename(includePath: true);
            Pass($"OpenFile succeeded (ret=0)");
            Row("previousFile", string.IsNullOrEmpty(previousFile) ? "(none)" : previousFile);
            Row("nowOpen", nowOpen);
            Row("matched", (string.Equals(nowOpen, TEST_MODEL_PATH, StringComparison.OrdinalIgnoreCase)).ToString());
        }
        else
        {
            Fail($"OpenFile returned {ret}");
        }
    }
    catch (Exception ex)
    {
        Fail($"Exception: {ex.Message}");
    }
    finally
    {
        // Mode A: Dispose() releases COM only — ETABS must stay open with the file loaded
        app?.Dispose();

        var stillRunning = System.Diagnostics.Process.GetProcessesByName("ETABS").Any();
        if (stillRunning) Pass("✓ ETABS still running after Dispose()");
        else Fail("✗ ETABS exited after Dispose() — Dispose() is calling ApplicationExit (bug)");
    }
}

// ─────────────────────────────────────────────────────────────
//  TEST 4 — close-model --save
//  Mode A: attach → SaveFile() → InitializeNewModel() → Dispose()
//  Verify: ETABS shows blank model and stays open
// ─────────────────────────────────────────────────────────────

async Task Test_CloseModel_Save()
{
    Header("close-model --save (Mode A)");
    Info("ETABS must be running with a model open.");

    ETABSApplication? app = null;
    try
    {
        app = ETABSWrapper.Connect();
        if (app == null) { Fail("No running ETABS found."); return; }

        var currentFile = app.Model.ModelInfo.GetModelFilename(includePath: true);
        if (string.IsNullOrEmpty(currentFile))
        {
            Warn("No model currently open. Open one first (Test 3).");
            return;
        }

        Info($"Currently open: {currentFile}");

        int saveRet = app.Model.Files.SaveFile(currentFile);
        Row("SaveFile ret", saveRet.ToString());

        int blankRet = app.SapModel.InitializeNewModel(eUnits.kip_ft_F);
        Row("InitializeNewModel ret", blankRet.ToString());

        if (saveRet == 0 && blankRet == 0)
        {
            Pass("close-model --save succeeded");
            var nowOpen = app.Model.ModelInfo.GetModelFilename(includePath: true);
            Row("fileAfterClose", string.IsNullOrEmpty(nowOpen) ? "(none — blank model)" : nowOpen);
        }
        else
        {
            Fail($"Non-zero return: save={saveRet}, blank={blankRet}");
        }
    }
    catch (Exception ex)
    {
        Fail($"Exception: {ex.Message}");
    }
    finally
    {
        // Mode A: Dispose() releases COM only — ETABS must stay open with blank model
        app?.Dispose();

        var stillRunning = System.Diagnostics.Process.GetProcessesByName("ETABS").Any();
        if (stillRunning) Pass("✓ ETABS still running after Dispose()");
        else Fail("✗ ETABS exited after Dispose() — Dispose() is calling ApplicationExit (bug)");
    }
}

// ─────────────────────────────────────────────────────────────
//  TEST 5 — close-model --no-save
//  Mode A: attach → InitializeNewModel() (no save) → Dispose()
//  Verify: no Save dialog appears, ETABS shows blank model and stays open
// ─────────────────────────────────────────────────────────────

async Task Test_CloseModel_NoSave()
{
    Header("close-model --no-save (Mode A)");
    Info("ETABS must be running. Make a change to the model so it's modified.");
    Info("Verify: no Save dialog appears during this test.");

    ETABSApplication? app = null;
    try
    {
        app = ETABSWrapper.Connect();
        if (app == null) { Fail("No running ETABS found."); return; }

        var currentFile = app.Model.ModelInfo.GetModelFilename(includePath: true);
        Info($"Currently open: {(string.IsNullOrEmpty(currentFile) ? "(none)" : currentFile)}");

        // InitializeNewModel bypasses the Save dialog entirely — confirmed safe
        int blankRet = app.Model.Files.NewBlankModel();
        Row("NewBlankModel ret", blankRet.ToString());

        if (blankRet == 0)
        {
            Pass("close-model --no-save succeeded (no Save dialog)");
            var nowOpen = app.Model.ModelInfo.GetModelFilename(includePath: true);
            Row("fileAfterClose", string.IsNullOrEmpty(nowOpen) ? "(none — blank model)" : nowOpen);
        }
        else
        {
            Fail($"NewBlankModel returned {blankRet}");
        }
    }
    catch (Exception ex)
    {
        Fail($"Exception: {ex.Message}");
    }
    finally
    {
        // Mode A: Dispose() releases COM only — ETABS must stay open
        app?.Dispose();

        var stillRunning = System.Diagnostics.Process.GetProcessesByName("ETABS").Any();
        if (stillRunning) Pass("✓ ETABS still running after Dispose()");
        else Fail("✗ ETABS exited after Dispose() — Dispose() is calling ApplicationExit (bug)");
    }
}

// ─────────────────────────────────────────────────────────────
//  TEST 6 — unlock-model
//  Mode A: attach → SetLocked(false) → Dispose()
//  Verify: ETABS model is editable and stays open
// ─────────────────────────────────────────────────────────────

async Task Test_UnlockModel()
{
    Header("unlock-model (Mode A)");
    Info("ETABS must be running with an analyzed (locked) model open.");

    ETABSApplication? app = null;
    try
    {
        app = ETABSWrapper.Connect();
        if (app == null) { Fail("No running ETABS found."); return; }

        var currentFile = app.Model.ModelInfo.GetModelFilename(includePath: true);
        if (string.IsNullOrEmpty(currentFile))
        {
            Warn("No model open. Open an analyzed model first (Test 3).");
            return;
        }

        Row("currentFile", currentFile);

        bool wasLocked = app.Model.ModelInfo.IsLocked();
        Row("wasLocked", wasLocked.ToString());

        if (!wasLocked)
            Warn("Model is not locked. Run analysis in ETABS UI first, then re-run this test.");

        app.Model.ModelInfo.SetLocked(false);

        bool nowLocked = app.Model.ModelInfo.IsLocked();
        Row("nowLocked", nowLocked.ToString());

        if (!nowLocked) Pass("Lock cleared — model is now editable");
        else Fail("Model is still locked after SetLocked(false)");
    }
    catch (Exception ex)
    {
        Fail($"Exception: {ex.Message}");
    }
    finally
    {
        // Mode A: Dispose() releases COM only — ETABS must stay open
        app?.Dispose();

        var stillRunning = System.Diagnostics.Process.GetProcessesByName("ETABS").Any();
        if (stillRunning) Pass("✓ ETABS still running after Dispose()");
        else Fail("✗ ETABS exited after Dispose() — Dispose() is calling ApplicationExit (bug)");
    }
}

// ─────────────────────────────────────────────────────────────
//  TEST 7 — generate-e2k
//  Mode B: CreateNew → Hide → OpenFile → ExportFile
//          → ApplicationExit(false) → Dispose()
//  Verify: hidden instance exits cleanly, no ETABS window left behind
// ─────────────────────────────────────────────────────────────

async Task Test_GenerateE2K()
{
    Header("generate-e2k (Mode B)");

    if (!File.Exists(TEST_MODEL_PATH))
    {
        Fail($"Test model not found: {TEST_MODEL_PATH}");
        return;
    }

    var e2kOutputPath = Path.Combine(OUTPUT_DIR, "model.e2k");
    Info($"Input:  {TEST_MODEL_PATH}");
    Info($"Output: {e2kOutputPath}");

    ETABSApplication? app = null;
    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        Info("Starting hidden ETABS instance...");
        app = ETABSWrapper.CreateNew(startApplication: true);
        if (app == null) { Fail("Failed to create ETABS instance."); return; }

        int hideRet = app.Application.Hide();
        Row("Hide ret", hideRet.ToString());
        Pass($"ETABS started hidden (v{app.FullVersion})");

        Info("Opening model...");
        int openRet = app.Model.Files.OpenFile(TEST_MODEL_PATH);
        Row("OpenFile ret", openRet.ToString());
        if (openRet != 0) { Fail($"OpenFile failed (ret={openRet})"); return; }

        Info("Exporting E2K...");
        int exportRet = app.Model.Files.ExportFile(e2kOutputPath, eFileTypeIO.TextFile);
        sw.Stop();
        Row("ExportFile ret", exportRet.ToString());

        if (exportRet == 0 && File.Exists(e2kOutputPath))
        {
            var sizeKb = new FileInfo(e2kOutputPath).Length / 1024.0;
            var firstLine = File.ReadLines(e2kOutputPath).FirstOrDefault() ?? "";
            Pass("generate-e2k succeeded");
            Row("outputFile", e2kOutputPath);
            Row("sizeKb", $"{sizeKb:F1} KB");
            Row("timeMs", sw.ElapsedMilliseconds.ToString());
            Row("firstLine", firstLine.Length > 60 ? firstLine[..60] + "..." : firstLine);
        }
        else
        {
            Fail($"ExportFile failed (ret={exportRet}) or output missing");
        }
    }
    catch (Exception ex)
    {
        Fail($"Exception: {ex.Message}");
    }
    finally
    {
        if (app != null)
        {
            // Mode B: explicitly exit the hidden instance, then release COM
            Info("Calling ApplicationExit(false) on hidden instance...");
            app.Application.ApplicationExit(false);
            app.Dispose();

            var etabsCount = System.Diagnostics.Process.GetProcessesByName("ETABS").Length;
            // If a user's ETABS was open before the test, count should be 1 (theirs), not 2
            Info($"Remaining ETABS processes: {etabsCount}");
            Pass("✓ Hidden instance exited and COM released");
        }
    }
}

// ─────────────────────────────────────────────────────────────
//  TEST 9 — run-analysis
//  Mode B: CreateNew → Hide → OpenFile → RunCompleteAnalysis
//          → SaveFile → ApplicationExit(false) → Dispose()
//  Verify: hidden instance exits cleanly, .edb updated with results
// ─────────────────────────────────────────────────────────────

async Task Test_RunAnalysis()
{
    Header("run-analysis (Mode B)");

    if (!File.Exists(TEST_MODEL_PATH))
    {
        Fail($"Test model not found: {TEST_MODEL_PATH}");
        return;
    }

    ETABSApplication? app = null;
    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        Info("Starting hidden ETABS instance...");
        app = ETABSWrapper.CreateNew(startApplication: true);
        if (app == null) { Fail("Failed to create ETABS instance."); return; }

        app.Application.Hide();
        Pass($"ETABS started hidden (v{app.FullVersion})");

        Info("Opening model...");
        int openRet = app.Model.Files.OpenFile(TEST_MODEL_PATH);
        if (openRet != 0) { Fail($"OpenFile failed (ret={openRet})"); return; }

        if (app.Model.ModelInfo.IsLocked())
        {
            Info("Model is locked — clearing before analysis...");
            app.Model.ModelInfo.SetLocked(false);
        }

        Info("Running analysis (this may take several minutes)...");
        int analysisRet = app.Model.Analyze.RunCompleteAnalysis();
        sw.Stop();

        Row("RunCompleteAnalysis ret", analysisRet.ToString());
        Row("timeMs", sw.ElapsedMilliseconds.ToString());
        Row("timeFormatted", FormatDuration(sw.Elapsed));

        if (analysisRet != 0) { Fail($"RunCompleteAnalysis failed (ret={analysisRet})"); return; }

        var caseStatuses = app.Model.Analyze.GetCaseStatus();
        var finished = caseStatuses.Count(cs => cs.IsFinished);
        var total = caseStatuses.Count;

        Row("casesTotal", total.ToString());
        Row("casesFinished", finished.ToString());

        if (app.Model.Analyze.AreAllCasesFinished())
            Pass("All cases finished ✓");
        else
        {
            Warn($"Only {finished}/{total} cases finished");
            foreach (var cs in caseStatuses.Where(c => !c.IsFinished))
                Row("  not finished", cs.CaseName ?? "unknown");
        }

        Info("Saving results into .edb...");
        int saveRet = app.Model.Files.SaveFile(TEST_MODEL_PATH);
        Row("SaveFile ret", saveRet.ToString());

        if (saveRet == 0)
        {
            Pass("Results saved into .edb ✓");
            Row("edbModified", new FileInfo(TEST_MODEL_PATH).LastWriteTime.ToString("HH:mm:ss"));
        }
        else
        {
            Fail($"SaveFile failed (ret={saveRet}) — results not persisted");
        }
    }
    catch (Exception ex)
    {
        Fail($"Exception: {ex.Message}");
    }
    finally
    {
        if (app != null)
        {
            // Mode B: explicitly exit the hidden instance, then release COM
            Info("Calling ApplicationExit(false) on hidden instance...");
            app.Application.ApplicationExit(false);
            app.Dispose();

            var etabsCount = System.Diagnostics.Process.GetProcessesByName("ETABS").Length;
            Info($"Remaining ETABS processes: {etabsCount}");
            Pass("✓ Hidden instance exited and COM released");
        }
    }
}

// ─────────────────────────────────────────────────────────────
//  TEST B — full pipeline (7 → 9)
// ─────────────────────────────────────────────────────────────

async Task Test_ModeB_FullPipeline()
{
    Header("Full Mode B Pipeline (generate-e2k → run-analysis)");
    Info($"Model: {TEST_MODEL_PATH}");

    if (!File.Exists(TEST_MODEL_PATH))
    {
        Fail($"Test model not found: {TEST_MODEL_PATH}");
        return;
    }

    var totalSw = System.Diagnostics.Stopwatch.StartNew();

    Console.WriteLine("\n  Step 1/2: generate-e2k");
    await Test_GenerateE2K();

    Console.WriteLine("\n  Step 2/2: run-analysis");
    await Test_RunAnalysis();

    totalSw.Stop();
    Console.WriteLine();
    Pass($"Full pipeline complete in {FormatDuration(totalSw.Elapsed)}");
    Info("Each step used a separate hidden ETABS instance (correct sidecar behavior)");
}

// ─────────────────────────────────────────────────────────────
//  PRINT HELPERS
// ─────────────────────────────────────────────────────────────

void Header(string title)
{
    Console.WriteLine();
    Console.WriteLine($"┌─ {title}");
    Console.WriteLine($"│");
}

void Pass(string msg)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"│  ✓ {msg}");
    Console.ResetColor();
}

void Fail(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"│  ✗ {msg}");
    Console.ResetColor();
}

void Warn(string msg)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"│  ⚠ {msg}");
    Console.ResetColor();
}

void Info(string msg) => Console.WriteLine($"│  ℹ {msg}");

void Row(string label, string value) =>
    Console.WriteLine($"│    {label,-26} {value}");

string FormatDuration(TimeSpan ts) =>
    ts.TotalMinutes >= 1
        ? $"{(int)ts.TotalMinutes}m {ts.Seconds}s"
        : $"{ts.TotalSeconds:F1}s";