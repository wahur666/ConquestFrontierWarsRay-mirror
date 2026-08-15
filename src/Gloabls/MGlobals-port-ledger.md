# MGlobals Port Ledger

This file is the explicit migration ledger for the native
[`src/Conquest/Include/MGlobals.h`](D:/git2/Conquest-Frontier-Wars-Source2/src/Conquest/Include/MGlobals.h)
surface.

Status meanings:

- `Ported`: implemented in `ConquestFrontierWarsRay/src/Gloabls`
- `Partial`: some state or API shape is present, but the native behavior is not fully carried over
- `Pending`: still needs a real managed port
- `Drop`: native-only glue or obsolete architecture; do not port as-is
- `Move`: should be implemented later, but in another subsystem rather than `Gloabls`

## Ported

- `GetUpgradeLevel`
- `SetAlly`
- `AreAllies`
- `GetOneWayAllyMask`
- `GetAllyMask`
- `GetAllyData`
- `SetAllyData`
- `GetVisibilityMask`
- `SetVisibilityMask`
- `SetPlayerVisibility`
- `GetPlayerFromPartID`
- `GetOwnerFromPartID`
- `IsHost`
- `GetColorID`
- `CreateNewPartID`
- `CreateSubordinatePartID`
- `GetCurrentTechLevel`
- `SetCurrentTechLevel`
- `GetWorkingTechLevel`
- `SetWorkingTechLevel`
- `GetTechAvailable`
- `SetTechAvailable`
- `GetCurrentGas`
- `SetCurrentGas`
- `GetCurrentMetal`
- `SetCurrentMetal`
- `GetCurrentCrew`
- `SetCurrentCrew`
- `GetCurrentTotalComPts`
- `SetCurrentTotalComPts`
- `GetCurrentUsedComPts`
- `SetCurrentUsedComPts`
- `GetMaxGas`
- `SetMaxGas`
- `GetMaxMetal`
- `SetMaxMetal`
- `GetMaxCrew`
- `SetMaxCrew`
- `IsGlobalLighting`
- `EnableGlobalLighting`
- `HasPlayerResigned`
- `SetPlayerResignedBySlot`
- `GetMaxControlPoints`
- `SetMaxControlPoints`
- `GetUpdateCount`
- `SetUpdateCount`: only as stored state; native anti-hack side behavior is intentionally not reproduced
- `GetPlayerRace`
- `SetPlayerRace`
- `GetLastStreamID`
- `SetLastStreamID`
- `GetLastTeletypeID`
- `SetLastTeletypeID`
- `GetScriptUIControl`
- `SetScriptUIControl`
- `SetMissionName`
- `GetMissionName`
- `SetMissionID`
- `GetMissionID`
- `SetMissionDescription`
- `GetMissionDescription`
- `AddToObjectiveList`
- `RemoveFromObjectiveList`
- `MarkObjectiveCompleted`
- `IsObjectiveCompleted`
- `IsObjectiveSecondary`
- `MarkObjectiveFailed`
- `IsObjectiveFailed`
- `IsObjectiveInList`
- `GetNumberObjectives`
- `GetObjectiveStringID`
- `IsHQ`
- `IsPlatform`
- `IsRefinery`
- `IsShipyard`
- `IsRepairPlat`
- `IsTenderPlat`
- `IsMilitaryShip`
- `IsObjectThreatening`
- `IsGunboat`
- `IsLightGunboat`
- `IsMediumGunboat`
- `IsHeavyGunboat`
- `IsCarrier`
- `IsTroopship`
- `IsFlagship`
- `IsMinelayer`
- `IsHarvester`
- `IsSupplyShip`
- `IsFabricator`
- `IsJumpPlat`
- `IsGunPlat`
- `IsSeekerShip`
- `GetThisPlayer`
- `GetGroupID`: available as constant only via `ConquestGlobalsConstants.GroupId`
- `CreateNewGroupPartID`
- `CreateNewJumpgatePartID`
- `SetScriptName`
- `GetScriptName`
- `SetTerrainFilename`
- `GetTerrainFilename`
- `ResetResourceMax`
- all game-stat getters/setters:
  - `GetNumUnitsBuilt`
  - `SetNumUnitsBuilt`
  - `GetUnitsDestroyed`
  - `SetUnitsDestroyed`
  - `GetUnitsLost`
  - `SetUnitsLost`
  - `GetNumPlatformsBuilt`
  - `SetNumPlatformsBuilt`
  - `GetNumAdmiralsBuilt`
  - `SetNumAdmiralsBuilt`
  - `GetPlatformsDestroyed`
  - `SetPlatformsDestroyed`
  - `GetPlatformsLost`
  - `SetPlatformsLost`
  - `GetUnitsConverted`
  - `SetUnitsConverted`
  - `GetPlatformsConverted`
  - `SetPlatformsConverted`
  - `GetNumJumpgatesControlled`
  - `SetNumJumpgatesControlled`
  - `GetGasGained`
  - `SetGasGained`
  - `GetMetalGained`
  - `SetMetalGained`
  - `GetCrewGained`
  - `SetCrewGained`
  - `GetResearchCompleted`
  - `SetResearchCompleted`
  - `GetExploredSystemsRatio`
  - `SetExploredSystemsRatio`
- `GetGameStats`
- `SetGameStats`
- `GetGameScores`
- `SetGameScores`
- `SetPlayerScore`
- `GetPlayerScore`
- `SetRegenMode`
- `GetGameSettings`
- `IsSinglePlayer`

## Partial

- `SetCurrentTechLevel`:
  player tech-band derivation is ported, but native live-object upgrade propagation is not
- `CreateNewPartID` / `CreateSubordinatePartID`:
  legacy id bit layout is preserved, but integration with live mission-object creation is not
- mission objective methods:
  state bookkeeping is ported, native event-system notifications are replaced with local .NET events
- color/resource data from `Globals.h`:
  ported as JSON manifest, not as Win32 resource tables

## Pending

- no purely local state helpers are pending in `Gloabls` after the second pass

## Move

These still need migration, but not into `Gloabls` as a pure global-state module.

- `CreateInstance`:
  belongs in the gameplay object/archetype runtime
- `InitMissionData`:
  belongs in the gameplay object runtime
- `UpgradeMissionObj`:
  belongs in the gameplay object/unit runtime
- `GetHarvestUpgrade`
- `GetTenderUpgrade`
- `GetFleetUpgrade`
- `GetFighterUpgrade`
- `GetBaseTargetingAccuracy`
- `GetEffectiveDamage`
- `GetIndExperienceLevel`
- `GetAdmiralExperienceLevel`
- `GetAIBonus`
- `AdvancedAI`
- `IsNightmareAI`
- `CanTroopship`
- `GetNextSubPartID`

## Drop

These are native shell, persistence glue, session glue, or other architecture that should not be reproduced verbatim in `Gloabls`.

- `IsUpdateFrame`
- player/slot/network identity helpers:
  - `GetPlayerNameBySlot`
  - `GetPlayerNameFromDPID`
  - `GetPlayerIDFromDPID`
  - `GetPlayerIDFromSlot`
  - `GetPlayerDPIDForPlayerID`
  - `GetSlotIDFromDPID`
  - `IsPlayerInGame`
  - `GetZoneSeatFromSlot`
  - `SetZoneSeatFromSlot`
  - `GetSlotIDForPlayerID`
  - `IsHostOnlyPlayerLeft`
  - `SetupComputerCharacter`
  - `RemoveDPIDFromPlayerID`
- save/load and map-file metadata glue:
  - `Save`
  - `Load`
  - `QuickSave`
  - `New`
  - `Close`
  - `GetFileDescription`
  - `SetFileDescription`
  - `GetFileMaxPlayers`
  - `SetFileMaxPlayers`
- native/private assignment helpers:
  - `AssignPlayers`
  - `AssignThisPlayer`
  - `SetLastPartID`

## Practical boundary

As of this ledger:

- `Gloabls` is the port of the portable shared-state/core-contract slice of `MGlobals`
- it is **not** a full gameplay-runtime port of all native `MGlobals.cpp`
- anything listed under `Pending` or `Move` still requires real migration work

This file is the source of truth for the module’s current completion boundary.
