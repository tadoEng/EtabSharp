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