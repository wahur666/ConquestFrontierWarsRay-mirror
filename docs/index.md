# ConquestFrontierWarsRay Framework Docs

This site combines hand-written framework notes with API reference generated from the `ConquestFrontierWarsRay.Framework` project.

## Docs

- [Framework Status](framework.md)
- [Framework Pain Points](framework-pain-points.md)
- [Plan](plan.md)
- [API Reference](api/ConquestFrontierWarsRay.Framework.yml)

## Build

From the `ConquestFrontierWarsRay` directory:

```powershell
dotnet build
dotnet tool restore
dotnet docfx docs/docfx.json --serve
```

The generated site is written to `docs/_site/`.
