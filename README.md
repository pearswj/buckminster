# da-thesis

Private repository for Digital Architectonics thesis project

> Provides components for the generation of structural frames from free-form surfaces in *Rhino/Grasshopper* using mesh operators and surface modelling techniques.

## Instructions for Visual Studio 2012

Clone the repository and load the solution file.

The *Math.Net Numerics* library is currently required for its matrix operations. Either install the package directly or configure NuGet to get the package during build.

### A. Direct Install

```
PM> Install-Package MathNet.Numerics
```

### B. During Build

Tools > Library Package Manager > Package Manager Settings

Make sure "*Allow NuGet to download missing packages during build*" is **checked**.

Right click on the solution in the Solution Explorer and click "*Enable NuGet Package Restore*".

Source: http://docs.nuget.org/docs/workflows/using-nuget-without-committing-packages

## Copy Math.NET to Grasshopper Folder

Once the Math.NET Numerics package has been downloaded, locate the DLL (`packages\MathNet.Numerics.2.5.0\lib\net40\MathNet.Numerics.dll`) and copy it to the Grasshopper Library folder (usually this is `AppData\Roaming\Grasshopper\Libraries\`).