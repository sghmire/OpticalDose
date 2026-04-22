# Optical Dose

Optical Dose is a Windows desktop application for radiochromic film dosimetry, DICOM RT dose review, and planar dose comparison. It is built with WPF on .NET 8 and combines film calibration, TIFF film processing, DICOM RTDOSE/RTSTRUCT/RTPLAN visualization, gamma analysis, field-size analysis, and star-shot QA tools in one workflow.

> Clinical safety note: this software performs dosimetry calculations and should be independently commissioned, validated, and reviewed before any clinical use. Verify calibration, scanner behavior, dose scaling, DICOM orientation, and analysis settings against local QA procedures.

## Main Capabilities

- Load standard image files for visual inspection and 16-bit TIFF film scans for dose analysis.
- Build film calibration configurations from measured RGB/OD points and polynomial fits.
- Convert film optical density to dose using single-channel, dual-channel, or triple-channel calibration logic.
- Read DICOM RT dose grids, RT structure sets, and RT plans.
- Navigate RT dose in axial, coronal, and sagittal multi-planar views.
- Display DICOM structure contours and navigate to structure centers or maximum-dose locations.
- Extract DICOM dose planes for comparison with measured film dose.
- Compare film and planned dose with gamma analysis, spatial shifts, ROI cropping, profile plots, and pass-rate reporting.
- Measure distance, area, ROI dose, image profiles, and crosshair coordinates on film images.
- Perform FWHM field-size analysis with configurable plateau and jaw/field methods.
- Perform star-shot analysis from dose images and print QA reports.

## Technology Stack

- Language: C#
- UI: WPF
- Runtime: .NET 8 for Windows
- Project type: `WinExe`
- Plotting: `ScottPlot.WPF`
- DICOM: `fo-dicom`
- TIFF reading: `BitMiracle.LibTiff.NET`
- UI controls/theme: `WPF-UI`

NuGet dependencies are declared in `OpticalDose.csproj`.

## Repository Layout

```text
.
|-- OpticalDose.sln
|-- OpticalDose.csproj
|-- App.xaml / App.xaml.cs
|-- MainWindow.xaml / MainWindow.xaml.cs
|-- AnalysisControl.xaml / AnalysisControl.xaml.cs
|-- DicomControl.xaml / DicomControl.xaml.cs
|-- FieldSizeWindow.xaml / FieldSizeWindow.xaml.cs
|-- StarShotWindow.xaml / StarShotWindow.xaml.cs
|-- CalibrationConfig.cs
|-- DoseCalculator.cs
|-- FittingMath.cs
|-- ImageFilters.cs
|-- ImageTransforms.cs
|-- ColorMaps.cs
|-- Models.cs
|-- StructureSet.cs
|-- Icon.png
|-- SamplePlane.dcm
```

Generated build output lives under `bin/` and `obj/`. These directories are not part of the source design and can be recreated by building the project.

## Requirements

- Windows
- .NET 8 SDK
- Visual Studio 2022 or newer with .NET desktop development workload, or the `dotnet` CLI

Because the application targets `net8.0-windows` and uses WPF, it is Windows-only.

## Getting Started

Clone or open the repository, then restore and build:

```powershell
dotnet restore
dotnet build
```

Run the application:

```powershell
dotnet run --project OpticalDose.csproj
```

You can also open `OpticalDose.sln` in Visual Studio and run the `OpticalDose` project directly.

## Typical Workflow

1. Open the app and load a raw film TIFF scan from the calibration/film workspace.
2. Create or select a calibration configuration.
3. Convert the film image to a dose map.
4. Load DICOM RTDOSE, RTSTRUCT, and RTPLAN files in the DICOM view.
5. Navigate to the relevant plane and extract the planned dose plane.
6. Sync film and DICOM data into the Analysis tab.
7. Adjust alignment, choose gamma criteria, select/crop ROI if needed, and run gamma analysis.
8. Review pass rate, gamma map, dose profiles, and reports.

## Film Calibration

Calibration data is represented by `CalibrationConfig` and saved as text configuration files in the configured calibration folder. By default, the app creates a `Calibrations` folder beside the built executable unless a custom path is set in application settings.

Supported calibration modes include:

- `Single: Red`
- `Single: Green`
- `Single: Blue`
- `Dual: Red/Blue`
- `Dual: Green/Blue`
- `Triple: Red|Green|Blue`

The calibration UI lets you enter known dose points with red, green, and blue channel values. Fits are calculated with polynomial regression from `FittingMath.cs`, and the resulting coefficients are stored in `CalibrationConfig`.

Dose conversion is handled in `DoseCalculator.cs`:

- Raw channel values are converted to optical density using `-log10(channel / 65535)`.
- Single-channel modes evaluate one polynomial fit directly.
- Dual-channel modes use an OD ratio against blue, then evaluate a second fit.
- Triple-channel mode evaluates red, green, and blue fits and averages the resulting doses.

## Film Image Tools

The main film view supports:

- Loading common image formats for display.
- Loading 16-bit TIFF film scans for dosimetry.
- Reading red, green, and blue channel data.
- Applying TIFF orientation metadata.
- Rotation and horizontal/vertical flips.
- Manual and center cropping.
- Median, average/box, Gaussian, noise, and ROI filters.
- Nearest, linear, and cubic interpolation.
- Undo and redo of image-processing operations.
- ROI extraction with freehand or fixed-size regions.
- Dose-map export and import as text files.
- Color map display with gray, jet, hot, and viridis mappings.

The core helper files for these operations are:

- `ImageFilters.cs`
- `ImageTransforms.cs`
- `ColorMaps.cs`
- `Models.cs`

## DICOM RT Workflow

The DICOM control reads DICOM files through `fo-dicom`. The primary supported RT modalities are:

- `RTDOSE` for dose grids.
- `RTSTRUCT` for structure contours.
- `RTPLAN` for isocenter and fraction information.

The DICOM viewer provides:

- Multi-file loading of RD, RS, and RP files.
- Multi-planar reconstruction views: axial, coronal, and sagittal.
- LPS coordinate display.
- TPS-relative coordinate mode using the loaded or manually set isocenter.
- Cursor dose readout in cGy.
- Structure contour rendering.
- Navigation to selected structure center.
- Navigation to maximum-dose location.
- Measurement mode in MPR views.
- Extraction of axial, coronal, or sagittal dose planes.
- Import of pre-extracted single-frame DICOM planes.
- Export of extracted dose planes as text maps.

DICOM dose values are scaled using `DoseGridScaling` and converted to cGy. When a planned fraction count is available in RTPLAN, it is used automatically; otherwise the app prompts for the number of fractions before extraction/export.

## Gamma Analysis

The Analysis tab compares measured film dose against planned DICOM dose. The workflow is implemented in `AnalysisControl.xaml.cs`.

Features include:

- Sync measured film dose from the film workspace.
- Sync planned dose from the DICOM viewer.
- Display measured, planned, and gamma heatmaps.
- Apply X/Y spatial shifts in millimeters.
- Apply dose scaling.
- Choose gamma dose-difference and distance-to-agreement criteria.
- Choose global or local normalization.
- Apply low-dose thresholding.
- Use bilinear or bicubic interpolation for planned dose sampling.
- Select and crop a shared ROI.
- Pick profile points and view X/Y dose profiles.
- Generate report snapshots for printing.

Advanced gamma settings are stored in `AppSettings`:

- `GammaUncertainty`
- `GammaSearchStep`
- `GammaSmoothingSigma`
- `GammaUseBicubic`

These are editable from the application settings dialog.

## Field-Size Analysis

`FieldSizeWindow` provides FWHM field-size and alignment analysis.

Capabilities include:

- Nominal field width/height setup in millimeters.
- Drag-and-nudge reticle alignment.
- Small rotation adjustments.
- Plateau-region setup.
- Maximum, mean, or median peak selection.
- FWHM X/Y calculation from 50% edge crossings.
- X-axis and Y-axis profile plots.
- Print report generation.
- Quick 3-point calibration using 0 MU, 300 MU, and 600 MU regions.
- Loading separate calibration and analysis TIFF scans.

The field-size calculations sample dose profiles through a plateau window and interpolate the 50% edge positions.

## Star-Shot Analysis

`StarShotWindow` provides star-shot QA analysis from a dose image.

Capabilities include:

- Draw an analysis circle over the image.
- Set detection threshold.
- Detect beam/spoke crossings.
- Estimate radiation isocenter.
- Report isocenter diameter and offset in millimeters.
- Draw detected spokes and isocenter overlays.
- Plot analysis results with ScottPlot.
- Print a star-shot report.

## Reporting

The app uses WPF `FlowDocument` and print dialogs for reporting. Reports are available for:

- Main dose comparison/gamma analysis.
- Field-size analysis.
- Star-shot analysis.

Reports include calculated metrics and captured visual elements such as maps and profile plots.

## Data And Settings

The application writes runtime settings beside the built executable:

- `app_settings.json`

Older settings may be read from:

- `roi_settings.json`

Calibration files are stored in the configured calibration folder. If no custom folder is selected, the default is:

```text
<application base directory>/Calibrations
```

Dose maps exported from film or DICOM workflows are text files with metadata headers and dose values.

## Important Source Files

- `MainWindow.xaml.cs`: Main application shell, film loading, calibration UI, image processing, dose conversion, measurement tools, reports, settings, and navigation between major views.
- `AnalysisControl.xaml.cs`: Film/plan synchronization, gamma analysis, ROI handling, profile plots, heatmaps, report snapshot capture, and comparison display.
- `DicomControl.xaml.cs`: DICOM RT dose loading, MPR navigation, RTSTRUCT parsing, RTPLAN isocenter/fraction parsing, plane extraction, contour drawing, and DICOM measurements.
- `FieldSizeWindow.xaml.cs`: FWHM field-size QA, quick calibration, profile plotting, and report generation.
- `StarShotWindow.xaml.cs`: Star-shot detection, isocenter calculation, overlays, plotting, and report generation.
- `DoseCalculator.cs`: Pixel-level film dose conversion from calibration coefficients.
- `FittingMath.cs`: Polynomial fitting, polynomial evaluation, R-squared calculation, and linear algebra helpers.
- `ImageFilters.cs`: 2D filtering, smoothing, interpolation, bilinear and bicubic sampling.
- `ImageTransforms.cs`: Array cloning, rotation, flipping, and cropping.
- `CalibrationConfig.cs`: Serializable calibration configuration model.
- `StructureSet.cs`: RT structure contour model.
- `Models.cs`: Shared app settings, calibration point, image state, and command helpers.

## Build Notes

Common developer commands:

```powershell
dotnet restore
dotnet build
dotnet run --project OpticalDose.csproj
dotnet clean
```

Release build:

```powershell
dotnet build -c Release
```

Publish example:

```powershell
dotnet publish OpticalDose.csproj -c Release -r win-x64 --self-contained false
```

For a self-contained build, change `--self-contained false` to `true`.

## Current Testing Status

No automated test project is present in this repository. Before relying on changes, manually verify at least:

- TIFF loading and orientation handling.
- Calibration fit creation and saved config reload.
- Dose conversion against known calibration points.
- DICOM RTDOSE loading and dose scaling.
- RTSTRUCT contour display on known slices.
- RTPLAN fraction/isocenter parsing.
- Plane extraction orientation and spacing.
- Gamma pass-rate calculation on a known film/plan pair.
- Field-size FWHM on a known field.
- Star-shot result on a known QA image.

## Known Implementation Notes

- The app currently keeps most UI logic and workflow logic in WPF code-behind files.
- `AutoAlign_Click` is present but currently empty, so automatic image alignment is not implemented.
- `bin/` and `obj/` contain generated outputs and can grow large.
- `SamplePlane.dcm` is included as a sample DICOM plane.
- The app assumes Windows desktop APIs and WPF printing support.

## Suggested Future Improvements

- Add automated unit tests for calibration math, filtering, interpolation, gamma search, and DICOM plane orientation.
- Split large code-behind files into services for dose math, DICOM extraction, reporting, and image processing.
- Add example calibration and dose-map files with expected outputs.
- Add validation datasets and acceptance criteria for commissioning.
- Add CI build verification for `dotnet build`.
- Consider packaging/publishing scripts for repeatable release builds.

