# EtabSharp

A modern, strongly-typed .NET wrapper for ETABS API (v22 and later).

## Features

- 🎯 **Strongly-typed API** with full IntelliSense support
- 🔄 **Automatic version detection** and compatibility checking
- 📊 **Comprehensive coverage** of ETABS functionality
- 🛡️ **Type-safe** property management and operations
- 📝 **Extensive documentation** with XML comments
- ⚡ **Performance optimized** with lazy loading

## Requirements

- **ETABS v22 or later** must be installed on your machine
- **.NET 10.0** or later
- **Windows OS** (ETABS is Windows-only)

## Installation

```bash
dotnet add package EtabSharp
```

## Quick Start

```csharp
using EtabSharp.Core;

// Connect to running ETABS instance
using var etabs = ETABSWrapper.Connect();

if (etabs == null)
{
    Console.WriteLine("No ETABS instance found. Please start ETABS first.");
    return;
}

// Access model components
var model = etabs.Model;

// Create a concrete material
var concrete = model.Materials.AddConcreteMaterial("C30", fc: 30, ec: 25000);

// Create a rectangular column
var column = model.PropFrame.AddRectangularSection("COL-400x400", "C30", 400, 400);

// Add a frame between two points
var frame = model.Frames.AddFrame("1", "2", "COL-400x400");

// Run analysis
model.Analyze.CreateAnalysisModel();
model.Analyze.RunAnalysis();

// Get results
var displacements = model.AnalysisResults.GetJointDispl("", eItemTypeElm.Objects);
```

## Documentation

Full documentation available at [![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/tadodev/EtabSharp)

## Important Notes

### ETABSv1.dll Reference
This package does **NOT** include `ETABSv1.dll`. You must have ETABS installed on your machine. The wrapper will automatically locate the DLL from your ETABS installation.

### Supported ETABS Versions
- ETABS v22.x ✅
- ETABS v23.x ✅
- Earlier versions ❌ (not supported)

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Contributing

Contributions welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) first.

## Support

- 📖 [Documentation](https://github.com/tadodev/EtabSharp/wiki)
- 🐛 [Issue Tracker](https://github.com/tadodev/EtabSharp/issues)
- 💬 [Discussions](https://github.com/tadodev/EtabSharp/discussions)


# EtabSharp
```csharp
EtabSharp/
├── Core/                           # Application & Model wrappers
│   ├── ETABSApplication.cs
│   ├── ETABSModel.cs
│   ├── ETABSWrapper.cs
│   └── Models/
│
├── Properties/                     # Define → Section Properties
│   ├── Materials/                  # Define → Material
│   │   ├── MaterialManager.cs     (implements IPropMaterial)
│   │   ├── Constants/
│   │   └── Models/
│   ├── Frames/                     # Define → Frame Sections
│   │   ├── FramePropertyManager.cs (implements IPropFrame)
│   │   └── Models/
│   ├── Areas/                      # Define → Slab & Wall
│   │   ├── AreaPropertyManager.cs  (implements IPropArea)
│   │   └── Models/
│   ├── Links/                      # Define → Link/Support
│   │   └── (future)
│   └── Cables/                     # Define → Cable
│       └── (future)
│
├── Elements/                       # Draw → Objects
│   ├── Stories/                    # Edit → Story
│   │   ├── StoryManager.cs        (implements IStory)
│   │   └── Models/
│   ├── Points/                     # Draw → Point
│   │   ├── PointObjectManager.cs  (implements IPointObject)
│   │   └── Models/
│   ├── Frames/                     # Draw → Frame
│   │   ├── FrameObjectManager.cs  (implements IFrameObject)
│   │   └── Models/
│   ├── Areas/                      # Draw → Slab/Wall
│   │   ├── AreaObjectManager.cs   (implements IAreaObject)
│   │   └── Models/
│   └── Selection/                  # Select menu
│       ├── SelectionManager.cs    (implements ISelection)
│       └── Models/
│
├── Labels/                         # Define → Pier/Spandrel Labels
│   ├── Piers/
│   │   ├── PierLabelManager.cs    (implements IPierLabel)
│   │   └── Models/
│   └── Spandrels/
│       ├── SpandrelLabelManager.cs (implements ISpandrelLabel)
│       └── Models/
│
├── Groups/                         # Define → Groups
│   ├── GroupManager.cs            (implements IGroup)
│   └── Models/
│
├── Loads/                          # Define → Load Patterns/Cases/Combos
│   ├── Patterns/
│   │   ├── LoadPatternManager.cs  (implements ILoadPattern)
│   │   └── Models/
│   ├── Cases/
│   │   ├── LoadCaseManager.cs     (implements ILoadCase)
│   │   └── Models/
│   ├── Combos/
│   │   ├── LoadComboManager.cs    (implements ILoadCombo)
│   │   └── Models/
│   └── Assignment/                 # Assign → Loads
│       ├── LoadAssignmentManager.cs
│       └── Models/
│
├── Analysis/                       # Analyze menu
│   ├── AnalysisManager.cs         (implements IAnalysis)
│   ├── ResultSetup/
│   │   ├── ResultSetupManager.cs  (implements IResultSetup)
│   │   └── Models/
│   ├── Results/
│   │   ├── ResultsManager.cs      (implements IResults)
│   │   └── Models/
│   └── Models/
│
├── Design/                         # Design menu
│   ├── Concrete/
│   │   ├── ConcreteDesignManager.cs (implements IConcreteDesign)
│   │   └── Models/
│   ├── Steel/
│   │   ├── SteelDesignManager.cs   (implements ISteelDesign)
│   │   └── Models/
│   ├── Shearwall/
│   │   ├── ShearwallDesignManager.cs (implements IShearwallDesign)
│   │   └── Models/
│   ├── Composite/
│   │   └── (future)
│   └── Forces/                      # Design → Steel/Concrete Frame Design Forces
│       ├── DesignForceManager.cs
│       └── Models/
│
├── Tables/                          # Display → Show Tables (Ctrl+T)
│   ├── DatabaseTableManager.cs     (implements IDatabaseTable)
│   └── Models/
│
├── System/                          # File, Units, Model Info
│   ├── FileManager.cs              (implements IFiles)
│   ├── UnitManager.cs              (implements IUnitSystem)
│   ├── ModelInfoManager.cs         (implements ISapModelInfor)
│   └── Models/
│
├── Interfaces/
│   ├── Properties/
│   ├── Elements/
│   ├── Labels/
│   ├── Groups/
│   ├── Loads/
│   ├── Analysis/
│   ├── Design/
│   ├── Tables/
│   └── System/
│
└── Exceptions/


etabvcs/                                    # Root project
├── Cargo.toml                             # Workspace configuration
├── package.json                           # Frontend dependencies
├── vite.config.ts
├── tsconfig.json
├── tailwind.config.js
├── README.md
├── .gitignore
│
├── src/                                   # Frontend (React + TypeScript)
│   ├── main.tsx
│   ├── App.tsx
│   ├── components/
│   │   ├── ModelViewer/
│   │   │   ├── ModelViewer.tsx           # 3D model visualization
│   │   │   ├── StoryView.tsx             # Story-by-story view
│   │   │   ├── MemberList.tsx            # Columns/Beams/Walls list
│   │   │   └── GridOverlay.tsx           # Grid display
│   │   ├── VersionControl/
│   │   │   ├── CommitHistory.tsx         # Git commit history
│   │   │   ├── CommitDialog.tsx          # Commit changes dialog
│   │   │   ├── DiffViewer.tsx            # Show model differences
│   │   │   └── BranchSelector.tsx        # Branch management
│   │   ├── DataTables/
│   │   │   ├── StoriesTable.tsx          # Stories data
│   │   │   ├── PointsTable.tsx           # Points coordinates
│   │   │   ├── MembersTable.tsx          # Structural members
│   │   │   └── LoadsTable.tsx            # Load patterns/cases
│   │   ├── Analysis/
│   │   │   ├── StatisticsPanel.tsx       # Model statistics
│   │   │   ├── ValidationResults.tsx     # Validation errors
│   │   │   └── ComparisonView.tsx        # Compare two models
│   │   └── Settings/
│   │       ├── CliSettings.tsx           # EtabSharp CLI path config
│   │       └── GitConfig.tsx             # Git user settings
│   ├── store/
│   │   ├── index.ts
│   │   ├── modelSlice.ts                 # Current model state
│   │   ├── vcsSlice.ts                   # Version control state
│   │   └── uiSlice.ts                    # UI state
│   ├── hooks/
│   │   ├── useModel.ts                   # Model operations hook
│   │   ├── useVcs.ts                     # VCS operations hook
│   │   └── useCli.ts                     # CLI interaction hook
│   └── types/
│       ├── model.ts                      # TypeScript model types
│       ├── vcs.ts                        # VCS types
│       └── api.ts                        # API response types
│
├── src-tauri/                             # Tauri backend (Rust)
│   ├── Cargo.toml                        # Tauri app dependencies
│   ├── tauri.conf.json
│   ├── build.rs
│   ├── icons/
│   └── src/
│       ├── main.rs                       # Tauri app entry point
│       │
│       ├── commands/                     # Tauri commands (exposed to frontend)
│       │   ├── mod.rs
│       │   ├── model_commands.rs         # Model operations
│       │   │   # - import_model(path) -> Model
│       │   │   # - parse_e2k(path) -> E2KModel
│       │   │   # - get_model_stats(id) -> Statistics
│       │   │   # - export_model(id, format) -> Result
│       │   ├── vcs_commands.rs           # Version control
│       │   │   # - init_repository(path) -> Result
│       │   │   # - commit_model(message, model) -> Commit
│       │   │   # - get_history() -> Vec<Commit>
│       │   │   # - checkout(commit_id) -> Model
│       │   │   # - diff(commit1, commit2) -> Diff
│       │   │   # - create_branch(name) -> Result
│       │   ├── cli_commands.rs           # EtabSharp CLI bridge
│       │   │   # - execute_cli(args) -> CliOutput
│       │   │   # - get_cli_version() -> String
│       │   │   # - convert_to_json(e2k_path) -> JsonPath
│       │   └── validation_commands.rs    # Model validation
│       │       # - validate_model(model) -> ValidationReport
│       │       # - check_references() -> Vec<Error>
│       │
│       ├── services/                     # Business logic layer
│       │   ├── mod.rs
│       │   ├── model_service.rs          # Model management service
│       │   │   # - load_model(path) -> Result<Model>
│       │   │   # - save_model(model) -> Result<()>
│       │   │   # - calculate_statistics(model) -> Stats
│       │   ├── cli_service.rs            # CLI spawning/communication
│       │   │   # - spawn_cli(command) -> ChildProcess
│       │   │   # - parse_cli_output(output) -> Result
│       │   │   # - check_cli_available() -> bool
│       │   ├── git_service.rs            # Git operations (using git2)
│       │   │   # - init_repo(path) -> Repository
│       │   │   # - create_commit(repo, msg) -> Oid
│       │   │   # - get_commits(repo) -> Vec<Commit>
│       │   │   # - checkout_commit(repo, id) -> Result
│       │   │   # - create_diff(old, new) -> Diff
│       │   ├── storage_service.rs        # Database operations
│       │   │   # - save_snapshot(model) -> Result<i64>
│       │   │   # - get_snapshot(id) -> Snapshot
│       │   │   # - list_snapshots() -> Vec<Snapshot>
│       │   └── diff_service.rs           # Model comparison
│       │       # - compare_models(m1, m2) -> ModelDiff
│       │       # - detect_changes(old, new) -> ChangeSet
│       │
│       ├── models/                       # Data structures
│       │   ├── mod.rs
│       │   ├── snapshot.rs               # ModelSnapshot struct
│       │   ├── commit.rs                 # Commit metadata
│       │   ├── diff.rs                   # Diff structures
│       │   └── stats.rs                  # Statistics structures
│       │
│       ├── db/                           # Database layer
│       │   ├── mod.rs
│       │   ├── schema.rs                 # SQLx schema definitions
│       │   ├── connection.rs             # DB connection pool
│       │   └── migrations/               # SQL migration files
│       │       ├── 001_init.sql
│       │       ├── 002_add_snapshots.sql
│       │       └── 003_add_commits.sql
│       │
│       ├── utils/                        # Utility functions
│       │   ├── mod.rs
│       │   ├── file_utils.rs            # File operations
│       │   └── error_utils.rs           # Error handling
│       │
│       └── error.rs                      # Application error types
│
├── e2k-parser/                            # ⭐ SEPARATE LIBRARY CRATE
│   ├── Cargo.toml
│   ├── README.md
│   ├── LICENSE
│   ├── src/
│   │   ├── lib.rs                        # Library entry point
│   │   │
│   │   ├── parser/                       # Parser implementation
│   │   │   ├── mod.rs                    # Main parser orchestrator
│   │   │   ├── primitives.rs            # Basic parsers (number, string, etc.)
│   │   │   ├── sections/                # Section-specific parsers
│   │   │   │   ├── mod.rs
│   │   │   │   ├── file_info.rs         # Parse file metadata
│   │   │   │   ├── program_info.rs      # Parse program info
│   │   │   │   ├── controls.rs          # Parse CONTROLS section
│   │   │   │   ├── stories.rs           # Parse STORIES section
│   │   │   │   ├── grids.rs             # Parse GRIDS section
│   │   │   │   ├── materials.rs         # Parse MATERIAL PROPERTIES
│   │   │   │   ├── rebar_defs.rs        # Parse REBAR DEFINITIONS
│   │   │   │   ├── frame_sections.rs    # Parse FRAME SECTIONS
│   │   │   │   ├── concrete_sections.rs # Parse CONCRETE SECTIONS
│   │   │   │   ├── shell_props.rs       # Parse SLAB/WALL PROPERTIES
│   │   │   │   ├── points.rs            # Parse POINT COORDINATES
│   │   │   │   ├── lines.rs             # Parse LINE CONNECTIVITIES
│   │   │   │   ├── areas.rs             # Parse AREA CONNECTIVITIES
│   │   │   │   ├── assignments.rs       # Parse ASSIGNS sections
│   │   │   │   ├── loads.rs             # Parse LOAD PATTERNS
│   │   │   │   ├── load_cases.rs        # Parse LOAD CASES
│   │   │   │   ├── combinations.rs      # Parse LOAD COMBINATIONS
│   │   │   │   └── preferences.rs       # Parse DESIGN PREFERENCES
│   │   │   └── utils.rs                 # Parser utilities
│   │   │
│   │   ├── model/                        # Domain model
│   │   │   ├── mod.rs                    # E2KModel struct
│   │   │   ├── core.rs                   # Core model structures
│   │   │   ├── geometry/                 # Geometry structures
│   │   │   │   ├── mod.rs
│   │   │   │   ├── point.rs             # Point struct + methods
│   │   │   │   ├── line.rs              # Line struct + methods
│   │   │   │   ├── area.rs              # Area struct + methods
│   │   │   │   └── grid.rs              # Grid struct
│   │   │   ├── structural/               # Structural elements
│   │   │   │   ├── mod.rs
│   │   │   │   ├── story.rs             # Story struct
│   │   │   │   ├── material.rs          # Material struct
│   │   │   │   ├── section.rs           # Section structs
│   │   │   │   └── property.rs          # Properties
│   │   │   ├── loading/                  # Load structures
│   │   │   │   ├── mod.rs
│   │   │   │   ├── pattern.rs           # LoadPattern
│   │   │   │   ├── case.rs              # LoadCase
│   │   │   │   └── combination.rs       # LoadCombination
│   │   │   └── design/                   # Design data
│   │   │       ├── mod.rs
│   │   │       └── preferences.rs       # Design preferences
│   │   │
│   │   ├── validation/                   # Validation system
│   │   │   ├── mod.rs                    # Validator struct
│   │   │   ├── rules/                    # Validation rules
│   │   │   │   ├── mod.rs
│   │   │   │   ├── required_sections.rs # Check required sections
│   │   │   │   ├── reference_integrity.rs # Check point/line refs
│   │   │   │   └── geometry_validity.rs # Check geometric validity
│   │   │   └── report.rs                 # ValidationReport
│   │   │
│   │   ├── query/                        # Query interface
│   │   │   ├── mod.rs                    # Query builder
│   │   │   ├── builder.rs                # Fluent query API
│   │   │   └── filters.rs                # Filter functions
│   │   │
│   │   ├── export/                       # Export functionality
│   │   │   ├── mod.rs
│   │   │   ├── json.rs                   # JSON export
│   │   │   └── csv.rs                    # CSV export
│   │   │
│   │   ├── error.rs                      # Error types
│   │   └── prelude.rs                    # Common imports
│   │
│   ├── tests/
│   │   ├── integration_tests.rs
│   │   ├── parser_tests.rs
│   │   ├── validation_tests.rs
│   │   └── fixtures/
│   │       ├── sample_simple.e2k
│   │       ├── sample_hotel.e2k
│   │       └── invalid.e2k
│   │
│   ├── benches/
│   │   └── parse_benchmark.rs            # Performance benchmarks
│   │
│   └── examples/
│       ├── basic_parse.rs
│       ├── validate_model.rs
│       └── export_json.rs
│
├── EtabExtension.CLI/                     # C# CLI (already exists)
│   └── ... (your existing C# project)
│
└── docs/
    ├── architecture.md
    ├── api.md
    └── development.md
```
