# BIM TOOLS SPX Exporter

**A batch export and model-inspection utility for Autodesk Revit 2024 and Bentley SYNCHRO 4D workflows.**

BIM TOOLS SPX Exporter helps BIM and VDC teams prepare Revit models for downstream SYNCHRO workflows. It brings batch processing, export configuration, model inspection, progress reporting, and logging together in a Windows desktop interface.

The application is built with C#, WPF, and the MVVM pattern. It integrates with the installed SYNCHRO Revit export functionality rather than claiming to implement the proprietary SPX format itself.

> **Compatibility:** Revit 2024 · .NET Framework 4.8 · Windows · x64

---

## Overview

Preparing multiple Revit models for coordination and 4D construction workflows can involve repetitive opening, checking, exporting, and troubleshooting tasks.

BIM TOOLS SPX Exporter is designed to organize that workflow through a single interface, with dedicated commands and services for model scanning, preparation, export execution, progress tracking, and reporting.

### Key capabilities

- **Batch export workflow** — organize and process multiple Revit model files through a central interface.
- **Active-model export** — initiate an export workflow for the model currently open in Revit.
- **Model inspection** — inspect model information and relevant element data before downstream use.
- **Element data extraction** — prepare element-related information for reporting and analysis.
- **Export engine architecture** — separate export-engine responsibilities behind a common interface.
- **Model preparation** — services for resolving views and preparing models for export.
- **File and job management** — scan model files, create export jobs, and manage file naming.
- **Progress feedback** — track batch-processing progress through a dedicated progress window.
- **Logging and diagnostics** — maintain logs and provide access to the log folder for troubleshooting.
- **Settings management** — centralize application settings and workflow configuration.
- **Desktop interface** — use a WPF interface organized with view models and reusable commands.

Available options and behavior depend on the current implementation, Revit model conditions, and the installed SYNCHRO export components.

---

## Workflow

```text
Revit model files
       |
       v
Model file scanning
       |
       v
Job creation and validation
       |
       v
Model opening and preparation
       |
       v
Installed SYNCHRO export integration
       |
       v
SPX output and processing reports
```

The diagram represents the intended high-level workflow. Actual results depend on model accessibility, export configuration, and the installed integration.

---

## Application architecture

The solution separates user interaction, business logic, and integration services.

| Component | Responsibility |
|---|---|
| `Commands/` | Revit commands for batch export, active-model export, inspection, element data, and application information |
| `Engines/` | Export-engine contracts, engine selection, SYNCHRO integration, IFC-related processing, and model inspection |
| `Infrastructure/` | Application paths, CSV utilities, command infrastructure, converters, icons, and pattern matching |
| `Models/` | Export contexts, jobs, settings, option models, and related data structures |
| `Services/` | Model discovery, opening, view preparation, naming, logging, reporting, settings, and export orchestration |
| `Startup/` | Revit application startup and add-in registration entry point |
| `ViewModels/` | Presentation state and UI command coordination |
| `Views/` | WPF application windows and their code-behind |

### Design approach

- Separation of concerns between UI, services, and export engines.
- MVVM-oriented presentation structure.
- Reusable command and service classes.
- Dedicated logging and reporting components.
- A shared export-engine abstraction for integration flexibility.

---

## Technology stack

| Technology | Purpose |
|---|---|
| C# 10 | Application implementation |
| .NET Framework 4.8 | Windows application runtime |
| WPF | Desktop user interface |
| MVVM | Presentation architecture |
| Autodesk Revit API 2024 | Revit application integration |
| NuGet | Optional Revit API reference packages for development environments |
| Bentley SYNCHRO export integration | SPX export workflow |

---

## Requirements

Before building or running the application, ensure that the environment includes:

1. Windows x64.
2. Autodesk Revit 2024.
3. .NET Framework 4.8.
4. Visual Studio 2022 or compatible MSBuild tooling.
5. The required Revit 2024 API assemblies, available through the Revit installation or the configured package references.
6. The appropriate SYNCHRO/Revit export components and licensing required by your installation.

**Important:** This add-in relies on the installed export integration for SYNCHRO-related functionality. It is not a standalone SYNCHRO application and does not guarantee that every Revit model can be exported without intervention.

---

## Build from source

### 1. Clone the repository

```powershell
git clone https://github.com/amdghedy/BimTools.SpxExporter.git
cd BimTools.SpxExporter
```

### 2. Open the solution

Open `BimTools.SpxExporter.sln` in Visual Studio.

### 3. Check Revit API references

The project targets `net48` and is configured for Revit 2024. Its project file supports two reference approaches:

- Use the Revit API DLLs from the configured Revit installation directory.
- Use the configured Revit 2024 API NuGet packages when the local API assemblies are unavailable.

Check `RevitInstallDir` in `BimTools.SpxExporter.csproj` if Revit is installed in a non-default location.

### 4. Build

Select the appropriate configuration and x64 platform, restore packages if prompted, and build the solution.

A successful build depends on the installed build tools, restored dependencies, and compatible Revit API references.

### 5. Review deployment behavior

The project includes a post-build deployment target named `DeployToRevit`. On Windows, it is configured to copy the built add-in assembly and manifest into the Revit 2024 add-ins directories.

Review the deployment paths in the project file before building if you do not want the build to install files into the machine-wide Revit add-ins directory.

---

## Installation and deployment

The project is configured around the following Revit 2024 add-in locations:

```text
%ProgramData%\Autodesk\Revit\Addins\2024\
    BimTools.SpxExporter.addin
    BimTools.SpxExporter\
        BimTools.SpxExporter.dll
```

The manifest references the assembly using a relative path:

```xml
<Assembly>BimTools.SpxExporter\BimTools.SpxExporter.dll</Assembly>
```

Keep the manifest and assembly folder structure consistent. If deploying manually, place the DLL and any required dependencies in the configured assembly directory and the manifest in the Revit 2024 add-ins directory.

Restart Revit after installation, then check the available add-in commands. Exact command visibility depends on successful loading and the current Revit session.

---

## Operational considerations

### Model files

- Ensure source files are accessible and are not locked by another process.
- Use suitable working copies and follow your project's model-management procedures.
- Validate model paths and output locations before running large batches.
- Test representative models before processing an entire project.

### Export integration

- Confirm the required SYNCHRO/Revit export components are installed and compatible.
- Verify that the integration is available on the machine running the add-in.
- Review application logs when a model fails to open or export.
- Check the generated output before relying on it for downstream coordination or 4D planning.

### Performance

Batch-processing performance varies with model size, model complexity, storage speed, available memory, and export settings. Large or damaged models may require individual troubleshooting.

---

## Troubleshooting

| Symptom | Checks |
|---|---|
| Add-in does not appear | Confirm Revit 2024 is being used, the manifest is in the correct directory, and the referenced DLL exists |
| Assembly fails to load | Check the manifest path, target framework, x64 configuration, and required dependencies |
| Build cannot resolve Revit API references | Verify `RevitInstallDir`, API DLL locations, NuGet restore, and package compatibility |
| Export integration is unavailable | Verify that the required SYNCHRO/Revit exporter is installed and configured |
| A model fails during batch processing | Check file accessibility, model integrity, permissions, logs, and model-specific requirements |
| Output is missing or incomplete | Verify output paths, export settings, integration status, and the processing report |

---

## Repository structure

```text
BimTools.SpxExporter/
├── Commands/
├── Engines/
├── Infrastructure/
├── Models/
├── Services/
├── Startup/
├── ViewModels/
├── Views/
├── BimTools.SpxExporter.addin
├── BimTools.SpxExporter.csproj
├── BimTools.SpxExporter.sln
└── README.md
```

---

## Version information

- **Project:** BIM TOOLS SPX Exporter
- **Assembly:** `BimTools.SpxExporter`
- **Current project version:** 1.3.3
- **Target Revit version:** 2024
- **Target framework:** .NET Framework 4.8
- **Platform:** x64

---

## Contributing

Contributions, bug reports, and improvement proposals are welcome.

When proposing a change:

1. Describe the problem or workflow improvement.
2. Keep changes focused and consistent with the existing architecture.
3. Build and test against the intended Revit 2024 environment.
4. Avoid committing build outputs, machine-specific configuration, credentials, or proprietary dependencies.
5. Include reproduction steps for reported defects where possible.

---

## Disclaimer

This is a BIM automation and integration tool intended to support Revit-to-SYNCHRO workflows. It does not replace model QA, contractual review, or validation of exported data.

Autodesk Revit and Bentley SYNCHRO are trademarks of their respective owners. This project is not presented as an official Autodesk or Bentley product.

---

## Author

**BIM TOOLS**

GitHub: [@amdghedy](https://github.com/amdghedy)

Project repository: [BimTools.SpxExporter](https://github.com/amdghedy/BimTools.SpxExporter)
