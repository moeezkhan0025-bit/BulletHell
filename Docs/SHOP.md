# Shop (M9b)

The card-based Shop between Round Results and the Armory. Screenshots: `Docs/Screenshots/M9b/`.

## Layout
Merchant placeholder on the left, a stall panel with 2 arm cards (top row), 3 armament cards (bottom row) and the crate; currency top-right, Reroll and Leave at the bottom, a BAG icon bottom-left (bought cards fly there). The screen is built by `Editor/M9bSetup.cs` from the `ShopCard` prefab (`Assets/Prefabs/UI/ShopCard.prefab`); every visual is a child object with a swappable sprite, so final art replaces placeholders without code. Frames, panels, buttons, the tooltip and the price tag read the UI1 `UITheme`.

## Cards
Icon, name, rarity frame (tint from the `RarityTable`) and a price tag. Items with no icon art yet show a tinted circle with the item's first letter. The focused card lifts (PrimeTween scale + rise) and gets a focus ring. An unaffordable price is red. Buying flies the icon into the BAG and stamps SOLD; an unaffordable purchase shakes the card and says why.

## Stock, prices and rerolls (all numbers are data)
- `ShopPool`: the candidate arms and armaments. `RarityTable`: per rarity a frame colour, a base weight, a weight gained per round, the first round it can appear, and a price multiplier; plus the base price of each price tier and the price growth per round. `ShopTuning`: card counts, crate, reroll cost, debug currency.
- Stock is picked by rarity weight, without repeats, from a seed: the same (seed, round, reroll) always gives the same stock, so the saved visit reproduces it exactly. Better rarities unlock and gain weight in later rounds (Epic from round 3, Legendary from round 5 by default).
- Price = tier base x rarity multiplier x (1 + growth x (round - 1)), rounded to 5.
- Reroll costs `base + step x rerolls this visit`, gives new stock and clears SOLD; it resets every visit. Stock never changes otherwise.

## Crate
Costs a fixed price. Buying it commits and opens 3 armament choices: pick one (Circle is disabled). Nothing is saved until the pick, so quitting mid-choice leaves the crate unbought and unpaid.

## Tooltip
Beside the focused card: the M9a generated description (stat changes, effects, tags, "Max N per arm"), which of your arms it fits and their free slots, and the stats before -> after. Square toggles the detailed view (every stat, every fitting arm, the resulting shot behaviour and why an arm does not fit).

## Controls
Stick / D-pad move focus, Cross buys, Triangle (or T) rerolls, Square (or Q) toggles details, Circle (or Esc) and Start leave. Mouse: hover focuses, click buys. `F7` (debug) gives currency.

## Saving
The run saves after every buy, reroll and crate pick. `SaveData` has `shopSeed`, `shopRerolls` and `shopSoldMask` (additive fields, no version bump), so Continue shows the same stock, SOLD cards and reroll cost, with your purchases. A save without these fields gets a fresh visit.
