# Armory (M9c)

Between the Shop and the next round. Code: `Assets/Scripts/Armory/`, built by `Assets/Editor/M9cSetup.cs` (menu BulletHell > M9c > Setup Armory).

## Layout
- LEFT: the gladiator on a pedestal (uGUI Images, follows `GameConfig.showCustomizationInGame`), the 8 arm slots on the
  ArmRingTuning ellipse around the feet (back arms behind the body, front arms in front, depth cue), an info panel
  (arm stats / item text / stats before -> after).
- Selecting an arm pops its 1-3 armament bubbles above it, fanned in an arc, tied by glowing tethers, bobbing.
  Empty = "+", filled = icon with a rarity rim, focused = scaled with an outline. PrimeTween in/out.
- RIGHT: Arms / Armaments tabs (L1/R1) over a 3-column grid of the Shop's `ShopCard`. Cards that do not fit the
  current selection are dimmed (armament limits from `ArmInstance.CanEquipAt`; arms only place into an empty slot).

## Flow (controller)
| Level | Input |
|---|---|
| Ring | stick/D-pad cycle slots; Cross on an arm opens its bubbles; Cross on an empty slot jumps to the Arms tab; Triangle removes the arm (with its armaments) to the arm inventory; the last arm cannot be removed |
| Bubbles | left/right between bubbles; Cross jumps to the Armaments tab for that bubble; Triangle on a filled bubble returns the armament; Circle back to the ring |
| Picking | focusing a card previews before -> after; Cross confirms (the icon flies into the bubble / slot); Circle back |

Start/Options or the Fight! button starts the next round. Mouse: hover focuses, click acts (same as Cross).
Menu input map additions: `TabPrev`/`TabNext` (L1/R1, `[`/`]`), `Remove` (Triangle, `X`).

## Saving
Every change calls `RunManager.SaveRun()` (same rule as the Shop), so quitting and Continue keep the loadout,
armaments and both inventories.
