# Bug list

How to log a bug (one entry each, newest at the top of "Open"):
- **Title:** short description
- **Steps:** exactly what to do to make it happen (round, loadout, what you pressed)
- **Expected:** what should happen
- **Actual:** what happens instead (paste Console errors if any)
- **How often:** always / sometimes / once
- **Severity:** crash/blocker, major (breaks a feature), minor (looks/feels wrong)

## Open

### Sprite_Character materials disable 2D SRP batching (console warning)
- **Steps:** enter Play mode; the Console shows "Material Mat_SpriteCharacter (and Mat_SpriteOutline) has _TexelSize / _ST texture properties which are not supported by 2D SRP Batcher"
- **Expected:** no warning
- **Actual:** warning once per material; those renderers (player, enemies, arms) are drawn without SRP batching. Comes from the _MainTex property Unity's own Sprite Unlit graph template declares, so it is harmless for now; revisit in the M12 performance pass if draw calls matter.
- **How often:** always
- **Severity:** minor

### Example: Ricochet bullets pass through low walls
- **Steps:** Round 2, equip Ricochet on the red arm, fire at the low wall near the left gate
- **Expected:** bullet bounces off the wall
- **Actual:** bullet goes straight through
- **How often:** always
- **Severity:** major

## Fixed
<!-- Claude moves entries here with a one-line note of the cause and the fix -->
