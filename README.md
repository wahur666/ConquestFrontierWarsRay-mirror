# ConquestFrontierWarsRay

`ConquestFrontierWarsRay` is a new reimplementation of *Conquest: Frontier Wars*
backed by Raylib rather than DirectX directly.

This project is the clean collection point for the rewrite foundation: typed
data, imported assets, runtime code, and future game modules can be tracked
here without being coupled to the legacy host layout elsewhere in the repo.

The main source of truth for game behavior, content shape, and gameplay intent
is:

- https://github.com/wahur666/Conquest-Frontier-Wars-Source

## Current scope

The first concrete step is establishing an isolated .NET application that can
own the harvested Conquest data and the Raylib runtime foundation from the
start.
