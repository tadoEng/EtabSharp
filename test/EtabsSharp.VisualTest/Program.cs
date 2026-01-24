using EtabSharp.Core;
using EtabSharp.DatabaseTables.Models;
using ETABSv1;

Console.WriteLine("=================================================");
Console.WriteLine("  EtabSharp Example - Frame Analysis with Rebar");
Console.WriteLine("=================================================\n");

// Configuration
bool attachToInstance = true;
string apiPath = @"C:\CSi_ETABS_API_Example";
string modelPath = Path.Combine(apiPath, "EtabSharp_Example.edb");

// Ensure directory exists
if (!Directory.Exists(apiPath))
{
    Directory.CreateDirectory(apiPath);
    Console.WriteLine($"✓ Created directory: {apiPath}");
}

// Connect to or create ETABS instance
ETABSApplication? etabs;

if (attachToInstance)
{
    Console.WriteLine("Attempting to connect to running ETABS instance...");
    etabs = ETABSWrapper.Connect();
    if (etabs == null)
    {
        Console.WriteLine("❌ No running instance found. Please open ETABS first.");
        return;
    }
}
else
{
    Console.WriteLine("Creating new ETABS instance...");
    etabs = ETABSWrapper.CreateNew(startApplication: true);
    if (etabs == null)
    {
        Console.WriteLine("❌ Cannot start ETABS.");
        return;
    }
}

Console.WriteLine($"✓ Connected to ETABS v{etabs.FullVersion}\n");
try
{
    var model = etabs.Model;

    // Get the available database tables

    var tables = model.DatabaseTables.GetAvailableTables();

    foreach (var table in tables.Tables)
    {
        Console.WriteLine(table.TableName);
    }

    Console.WriteLine("---------------------------------");
    var name = model.DatabaseTables.GetAllFieldsInTable("Load Combination Definitions");

    foreach (TableFieldInfo tableFieldInfo in name.Fields)
    {
        Console.WriteLine(tableFieldInfo);
    }
    // Run the model
    if (model.ModelInfo.IsLocked())
    {
        model.ModelInfo.SetLocked(false);
    }

    model.Analyze.SetAllCasesToRun();
    model.Analyze.RunCompleteAnalysis();
    Console.WriteLine("finish run analysis");

    // Setup the results for all load combinations
    model.AnalysisResultsSetup.DeselectAllCasesAndCombosForOutput();
    model.AnalysisResultsSetup.SetCaseSelectedForOutput("Dead");
    Console.WriteLine("finish setup output cases");

    // Get Joint Displacements for joint "14"
    var displacements = model.AnalysisResults.GetJointDispl("4", eItemTypeElm.Element);

    // Print joint displacement results
    if (displacements is { IsSuccess: true, NumberResults: > 0 })
    {
        Console.WriteLine("\n=================================================");
        Console.WriteLine("  Joint Displacement Results");
        Console.WriteLine("=================================================\n");

        foreach (var result in displacements.Results)
        {
            Console.WriteLine($"Joint: {result.ObjectName}");
            Console.WriteLine($"  Element: {result.ElementName}");
            Console.WriteLine($"  Load Case: {result.LoadCase}");
            Console.WriteLine($"  Step Type: {result.StepType}");
            Console.WriteLine($"  Step Number: {result.StepNum:F0}");
            Console.WriteLine($"\n  Translations (Global):");
            Console.WriteLine($"    U1 (X): {result.U1:F6} in");
            Console.WriteLine($"    U2 (Y): {result.U2:F6} in");
            Console.WriteLine($"    U3 (Z): {result.U3:F6} in");
            Console.WriteLine($"\n  Rotations (Global):");
            Console.WriteLine($"    R1 (RX): {result.R1:F8} rad");
            Console.WriteLine($"    R2 (RY): {result.R2:F8} rad");
            Console.WriteLine($"    R3 (RZ): {result.R3:F8} rad");
            Console.WriteLine();
        }

        Console.WriteLine($"Total Results: {displacements.NumberResults}");

        //setup all load case for base reaction retrieval
        model.AnalysisResultsSetup.SetAllCasesAndCombosForOutput();
        var baseReactionResults = model.AnalysisResults.GetBaseReact();

        var reactions = baseReactionResults.Results.Select(r => new
        {
            loadCase = r.LoadCase,
            stepType = r.StepType,
            stepNumber = r.StepNum,
            forces = new { fx = r.FX, fy = r.FY, fz = r.FZ },
            moments = new { mx = r.MX, my = r.MY, mz = r.MZ }
        }).ToList();

        if (baseReactionResults is { IsSuccess: true, NumberResults: > 0 })
        {
            Console.WriteLine($"Base Reaction Location:");
            Console.WriteLine($"  Global X: {baseReactionResults.GlobalX:F4}");
            Console.WriteLine($"  Global Y: {baseReactionResults.GlobalY:F4}");
            Console.WriteLine($"  Global Z: {baseReactionResults.GlobalZ:F4}\n");

            foreach (var reaction in baseReactionResults.Results)
            {
                Console.WriteLine($"Load Case: {reaction.LoadCase}");
                Console.WriteLine($"  Step Type: {reaction.StepType}");
                Console.WriteLine($"  Step Number: {reaction.StepNum:F0}");
                Console.WriteLine($"\n  Forces:");
                Console.WriteLine($"    FX: {reaction.FX:F4} kip");
                Console.WriteLine($"    FY: {reaction.FY:F4} kip");
                Console.WriteLine($"    FZ: {reaction.FZ:F4} kip");
                Console.WriteLine($"\n  Moments:");
                Console.WriteLine($"    MX: {reaction.MX:F4} kip-in");
                Console.WriteLine($"    MY: {reaction.MY:F4} kip-in");
                Console.WriteLine($"    MZ: {reaction.MZ:F4} kip-in");
                Console.WriteLine();
            }

            Console.WriteLine($"Total Reactions: {baseReactionResults.NumberResults}");

        }
        else if (!displacements.IsSuccess)
        {
            Console.WriteLine($"❌ Error retrieving displacement results: {displacements.ErrorMessage}");
        }
        else
        {
            Console.WriteLine("⚠️ No displacement results found for joint P4");
        }
    }


    //START THE DESIGN PART
    model.SteelDesign.StartDesign();
    var results = model.SteelDesign.GetSummaryResults_3("All", eItemType.Group);

    foreach (var steelDesignSummaryResult in results.Results)
    {
        Console.WriteLine($"Frame:{steelDesignSummaryResult.FrameName} with DCR:{steelDesignSummaryResult.ControllingRatio}");
    }

    //model.ConcreteDesign.StartDesign();

    //var results = model.ConcreteDesign.GetSummaryResultsColumn("All", eItemType.Group);

    //foreach (var concreteColumnDesignResult in results.Results)
    //{
    //    Console.WriteLine($"Frame: {concreteColumnDesignResult.FrameName} with DCR: {concreteColumnDesignResult.PMMRatio}");
    //}

    //var totals = model.ConcreteDesign.GetRebarPrefsColumn(52);


}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}




//
// try
// {
//     var model = etabs.Model;
//
//     // =====================================
//     // 1. INITIALIZE MODEL
//     // =====================================
//     Console.WriteLine("STEP 1: Initializing Model");
//     Console.WriteLine("----------------------------");
//
//     model.ModelInfo.InitializeNewModel(eUnits.kip_in_F);
//     model.Files.NewBlankModel();
//     Console.WriteLine("✓ New blank model created\n");
//
//     // =====================================
//     // 2. DEFINE MATERIALS
//     // =====================================
//     Console.WriteLine("STEP 2: Defining Materials");
//     Console.WriteLine("----------------------------");
//
//     // Concrete material
//     var concrete = model.Materials.AddConcreteMaterial(
//         name: "C5ksi",
//         fpc: 5000,  // 5 ksi compressive strength
//         Ec: 47 * Math.Sqrt(5000) * 1000  // 47*sqrt(fc) * 1000 psi -> ksi
//     );
//     Console.WriteLine($"✓ Concrete: {concrete.Name}");
//     Console.WriteLine($"  f'c = {concrete.fpc / 1000:F1} ksi");
//     Console.WriteLine($"  Ec = {concrete.Ec / 1000:F0} ksi");
//
//     // Rebar material
//     var rebar = model.Materials.AddRebarMaterial(
//         name: "A615Gr60",
//         fy: 60000,   // 60 ksi yield strength
//         fu: 90000    // 90 ksi ultimate strength
//     );
//     Console.WriteLine($"✓ Rebar: {rebar.Name}");
//     Console.WriteLine($"  fy = {rebar.Fy / 1000:F0} ksi");
//     Console.WriteLine($"  fu = {rebar.u / 1000:F0} ksi\n");
//
//     // =====================================
//     // 3. DEFINE FRAME SECTIONS
//     // =====================================
//     Console.WriteLine("STEP 3: Defining Frame Sections");
//     Console.WriteLine("--------------------------------");
//
//     // Column section (12" x 12")
//     var colSection = model.PropFrame.AddRectangularSection(
//         name: "COL_12x12",
//         materialName: concrete.Name,
//         depth: 12,
//         width: 12
//     );
//     Console.WriteLine($"✓ Column Section: {colSection.Name}");
//     Console.WriteLine($"  Dimensions: {colSection.Depth}\" x {colSection.Width}\"");
//
//     // Assign column reinforcement
//     var columnRebar = PropColumnRebarRect.Create(
//         longitudinalRebar: rebar.Name,
//         confinementRebar: rebar.Name,
//         barsIn3Direction: 4,    // 4 bars in width direction
//         barsIn2Direction: 4,    // 4 bars in depth direction
//         cover: 1.5,        // 1.5" cover
//         barSize: "#8",
//         tieSize: "#4"
//     );
//     model.PropFrame.SetColumnRebarRectangular(colSection.Name, columnRebar);
//     Console.WriteLine($"  Rebar: {columnRebar.NumberOfBars2Dir} x {columnRebar.NumberOfBars3Dir} bars");
//     Console.WriteLine($"  Cover: {columnRebar.Cover}\"");
//
//     // Beam section (12" x 24")
//     var beamSection = model.PropFrame.AddRectangularSection(
//         name: "BEAM_12x24",
//         materialName: concrete.Name,
//         depth: 24,
//         width: 12
//     );
//     Console.WriteLine($"✓ Beam Section: {beamSection.Name}");
//     Console.WriteLine($"  Dimensions: {beamSection.Depth}\" x {beamSection.Width}\"");
//
//     // Assign beam reinforcement
//     var beamRebar = PropBeamRebar.Create(
//         rebarMatProp: rebar.Name,
//         confinementMatProp: rebar.Name,
//         topCover: 1.5,
//         botCover: 1.5,
//         topLeftArea: 0,
//         topRightArea: 0,
//         botLeftArea: 4.0,   // 4 in² bottom left
//         botRightArea: 4.0   // 4 in² bottom right
//     );
//     model.PropFrame.SetBeamRebar(beamSection.Name, beamRebar);
//     Console.WriteLine($"  Top Rebar: {beamRebar.TopLeftArea} in² / {beamRebar.TopRightArea} in²");
//     Console.WriteLine($"  Bot Rebar: {beamRebar.BotLeftArea} in² / {beamRebar.BotRightArea} in²");
//
//     // Apply cracked section modifiers to both sections
//     var crackedModifiers = PropFrameModifiers.Cracked();
//     model.PropFrame.SetModifiers(colSection.Name, crackedModifiers);
//     model.PropFrame.SetModifiers(beamSection.Name, crackedModifiers);
//     Console.WriteLine("✓ Applied cracked section modifiers\n");
//
//     // =====================================
//     // 4. SWITCH UNITS AND BUILD GEOMETRY
//     // =====================================
//     Console.WriteLine("STEP 4: Building Frame Geometry");
//     Console.WriteLine("--------------------------------");
//
//     model.Units.SetPresentUnits(Units.US_Kip_Ft);
//     Console.WriteLine("✓ Units set to kip-ft");
//
//     // Add frame objects by coordinates
//     var frame1 = model.Frames.AddFrameByCoordinates(0, 0, 0, 0, 0, 10, colSection.Name, "COL1");
//     var frame2 = model.Frames.AddFrameByCoordinates(0, 0, 10, 8, 0, 16, beamSection.Name, "BEAM1");
//     var frame3 = model.Frames.AddFrameByCoordinates(-4, 0, 10, 0, 0, 10, beamSection.Name, "BEAM2");
//
//     Console.WriteLine($"✓ Frame 1 (Column): {frame1}");
//     Console.WriteLine($"✓ Frame 2 (Beam):   {frame2}");
//     Console.WriteLine($"✓ Frame 3 (Beam):   {frame3}\n");
//
//     // =====================================
//     // 5. ASSIGN RESTRAINTS
//     // =====================================
//     Console.WriteLine("STEP 5: Assigning Restraints");
//     Console.WriteLine("-----------------------------");
//
//     // Get points from frame 1 (column)
//     string pointBase = "";
//     string pointTop = "";
//     model.SapModel.FrameObj.GetPoints(frame1, ref pointBase, ref pointTop);
//
//     // Fixed support at base
//     model.Points.SetRestraint(pointBase, PointRestraint.Fixed());
//     Console.WriteLine($"✓ Point {pointBase}: Fixed support");
//
//     // Get points from frame 2 (beam)
//     string pointBeamI = "";
//     string pointBeamJ = "";
//     model.SapModel.FrameObj.GetPoints(frame2, ref pointBeamI, ref pointBeamJ);
//
//     // Roller support at beam end
//     model.Points.SetRestraint(pointBeamJ, PointRestraint.Pinned());
//     Console.WriteLine($"✓ Point {pointBeamJ}: Pinned support\n");
//
//     // =====================================
//     // 6. DEFINE LOAD PATTERNS
//     // =====================================
//     Console.WriteLine("STEP 6: Defining Load Patterns");
//     Console.WriteLine("-------------------------------");
//
//     model.LoadPatterns.AddDeadLoad("DEAD", 1.0);
//     model.LoadPatterns.AddSuperDeadLoad("SDL");
//     model.LoadPatterns.AddLiveLoad("LIVE");
//
//     Console.WriteLine("✓ Load Pattern: DEAD (Self-weight = 1.0)");
//     Console.WriteLine("✓ Load Pattern: SDL (Super Dead Load)");
//     Console.WriteLine("✓ Load Pattern: LIVE (Live Load)\n");
//
//     // =====================================
//     // 7. APPLY LOADS
//     // =====================================
//     Console.WriteLine("STEP 7: Applying Loads");
//     Console.WriteLine("-----------------------");
//
//     // Uniform load on beam (frame2) - SDL
//     var uniformLoad = FrameDistributedLoad.CreateGravityLoad(frame2, "SDL", 1.5); // 1.5 kip/ft
//     model.Frames.SetLoadDistributed(frame2, uniformLoad);
//     Console.WriteLine($"✓ Frame {frame2}: Uniform SDL = 1.5 kip/ft");
//
//     // Point load at midspan of frame3
//     model.Frames.SetMidspanLoad(frame3, "LIVE", 10.0); // 10 kip at midspan
//     Console.WriteLine($"✓ Frame {frame3}: Point LIVE = 10 kip at midspan\n");
//
//     // =====================================
//     // 8. CREATE LOAD COMBINATIONS
//     // =====================================
//     Console.WriteLine("STEP 8: Creating Load Combinations");
//     Console.WriteLine("-----------------------------------");
//
//     model.LoadCombinations.CreateLinearCombo("COMB1", ("DEAD", 1.4));
//     Console.WriteLine("✓ COMB1: 1.4D");
//
//     model.LoadCombinations.CreateLinearCombo("COMB2", ("DEAD", 1.2), ("LIVE", 1.6));
//     Console.WriteLine("✓ COMB2: 1.2D + 1.6L");
//
//     model.LoadCombinations.CreateLinearCombo("COMB3", ("DEAD", 1.2), ("SDL", 1.2), ("LIVE", 1.6));
//     Console.WriteLine("✓ COMB3: 1.2D + 1.2SDL + 1.6L\n");
//
//     // =====================================
//     // 9. SWITCH UNITS AND SAVE
//     // =====================================
//     Console.WriteLine("STEP 9: Saving Model");
//     Console.WriteLine("--------------------");
//
//     model.Units.SetPresentUnits(Units.US_Kip_In);
//     Console.WriteLine("✓ Units switched to kip-in");
//
//     model.Files.SaveFile(modelPath);
//     Console.WriteLine($"✓ Model saved: {modelPath}\n");
//
//     // =====================================
//     // 10. RUN ANALYSIS
//     // =====================================
//     Console.WriteLine("STEP 10: Running Analysis");
//     Console.WriteLine("-------------------------");
//
//     model.Analyze.RunCompleteAnalysis();
//     Console.WriteLine("✓ Analysis completed successfully\n");
//
//     // =====================================
//     // 11. GET RESULTS
//     // =====================================
//     Console.WriteLine("STEP 11: Extracting Results");
//     Console.WriteLine("----------------------------");
//
//     // Setup results for COMB2
//     model.AnalysisResultsSetup.DeselectAllCasesAndCombosForOutput();
//     model.AnalysisResultsSetup.SetComboSelectedForOutput("COMB2");
//
//     // Get joint displacements at top of column
//     var displacements = model.AnalysisResults.GetJointDispl(pointTop, eItemTypeElm.ObjectElm);
//
//     if (displacements.NumberResults > 0)
//     {
//         Console.WriteLine($"Joint {pointTop} Displacements (COMB2):");
//         Console.WriteLine($"  UX = {displacements.U1[0]:F4} in");
//         Console.WriteLine($"  UY = {displacements.U2[0]:F4} in");
//         Console.WriteLine($"  UZ = {displacements.U3[0]:F4} in");
//         Console.WriteLine($"  RX = {displacements.R1[0]:F6} rad");
//         Console.WriteLine($"  RY = {displacements.R2[0]:F6} rad");
//         Console.WriteLine($"  RZ = {displacements.R3[0]:F6} rad");
//     }
//
//     // Get base reactions
//     var baseReactions = model.AnalysisResults.GetBaseReact();
//     if (baseReactions.NumberResults > 0)
//     {
//         Console.WriteLine($"\nBase Reactions (COMB2):");
//         for (int i = 0; i < baseReactions.NumberResults; i++)
//         {
//             if (baseReactions.LoadCase[i] == "COMB2")
//             {
//                 Console.WriteLine($"  FX = {baseReactions.FX[i]:F2} kip");
//                 Console.WriteLine($"  FY = {baseReactions.FY[i]:F2} kip");
//                 Console.WriteLine($"  FZ = {baseReactions.FZ[i]:F2} kip");
//                 Console.WriteLine($"  MX = {baseReactions.MX[i]:F2} kip-in");
//                 Console.WriteLine($"  MY = {baseReactions.MY[i]:F2} kip-in");
//                 Console.WriteLine($"  MZ = {baseReactions.MZ[i]:F2} kip-in");
//                 break;
//             }
//         }
//     }
//
//     // Get frame forces
//     var frameForces = model.AnalysisResults.GetFrameForce(frame2, eItemTypeElm.ObjectElm);
//     if (frameForces.NumberResults > 0)
//     {
//         Console.WriteLine($"\nFrame {frame2} Forces at i-end (COMB2):");
//         for (int i = 0; i < frameForces.NumberResults; i++)
//         {
//             if (frameForces.LoadCase[i] == "COMB2" && frameForces.ObjSta[i] < 0.01)
//             {
//                 Console.WriteLine($"  P (Axial)  = {frameForces.P[i]:F2} kip");
//                 Console.WriteLine($"  V2 (Shear) = {frameForces.V2[i]:F2} kip");
//                 Console.WriteLine($"  V3 (Shear) = {frameForces.V3[i]:F2} kip");
//                 Console.WriteLine($"  M2 (Moment)= {frameForces.M2[i]:F2} kip-in");
//                 Console.WriteLine($"  M3 (Moment)= {frameForces.M3[i]:F2} kip-in");
//                 break;
//             }
//         }
//     }
//
//     Console.WriteLine("\n=================================================");
//     Console.WriteLine("  ✓ EtabSharp Example Completed Successfully!");
//     Console.WriteLine("=================================================");
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"\n❌ ERROR: {ex.Message}");
//     Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
// }
// finally
// {
//     // Close ETABS
//     Console.WriteLine("\nClosing ETABS...");
//     etabs?.Close(false);
//     etabs?.Dispose();
//     Console.WriteLine("✓ ETABS closed");
// }
//
// Console.WriteLine("\nPress any key to exit...");
// Console.ReadKey();