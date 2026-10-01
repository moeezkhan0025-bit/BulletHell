using System;
using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Capture
{
    public enum ArtStatus { Placeholder = 0, Final = 1 }

    public enum ArtAssetKind { Sprite = 0, Audio = 1, Other = 2 }

    /// <summary>Order is the order of the Art Slot Board sections.</summary>
    public enum ArtSection { Player = 0, Enemies, Boss, Projectiles, Arena, Vfx, Ui, Audio, Other }

    public enum RuleVerdict { Final = 0, Placeholder = 1, FromChecklist = 2 }

    public enum ChecklistState { NotFound = 0, Done, Partial, Pending }

    /// <summary>One path rule of the provenance engine. First matching rule wins. Data only.</summary>
    [Serializable]
    public sealed class ProvenanceRule
    {
        public string name = "";
        [Tooltip("Project-relative path prefix with forward slashes, e.g. Assets/Art/Player/. Empty matches everything.")]
        public string pathPrefix = "";
        [Tooltip("File name wildcard (* only), e.g. icon_armament_*. Empty or * matches every file.")]
        public string nameGlob = "";
        public RuleVerdict verdict = RuleVerdict.Placeholder;
        [Tooltip("FromChecklist: text of the Docs/ART_CHECKLIST.md entry that decides (done = final, partial or pending = placeholder).")]
        public string checklistText = "";
        [Tooltip("FromChecklist: verdict when the entry is not found in the checklist.")]
        public ArtStatus checklistFallback = ArtStatus.Placeholder;
        [TextArea] public string reason = "";
    }

    /// <summary>Maps where a slot lives (owner asset path, owner type, property path) to a board section. First match wins; all given parts must match.</summary>
    [Serializable]
    public sealed class SectionRule
    {
        public ArtSection section = ArtSection.Other;
        [Tooltip("Substring of the owner asset path (case-insensitive). '|' separates alternatives.")]
        public string ownerPathContains = "";
        [Tooltip("Substring of the owner type name (ScriptableObject or component type). '|' separates alternatives.")]
        public string ownerTypeContains = "";
        [Tooltip("Substring of the property path. '|' separates alternatives.")]
        public string propertyContains = "";
    }

    /// <summary>The spec text a final asset must meet, per slot kind. First match wins. {now} is replaced by the import settings read from the current asset.</summary>
    [Serializable]
    public sealed class SpecRule
    {
        public bool matchSection;
        public ArtSection section = ArtSection.Other;
        [Tooltip("Substring of 'ownerPath|assetPath|propertyPath' (case-insensitive). '|' separates alternatives.")]
        public string contains = "";
        [TextArea] public string needs = "";
    }

    /// <summary>
    /// The few classification rules that cannot be derived from the project. Everything else comes from facts: an ArtSource master PNG
    /// with the same relative path as the game PNG, Docs/ART_CHECKLIST.md check marks, Docs/CREDITS.md audio entries.
    /// Asset: Assets/Data/Capture/ArtProvenanceRules.asset (created by BulletHell/Capture/Create Art Provenance Rules).
    /// </summary>
    [CreateAssetMenu(fileName = "ArtProvenanceRules", menuName = "BulletHell/Capture/Art Provenance Rules")]
    public sealed class ArtProvenanceRules : ScriptableObject
    {
        public const string AssetPath = "Assets/Data/Capture/ArtProvenanceRules.asset";

        [Tooltip("A game PNG under Assets/Art/<x> whose master ArtSource/<x> exists is the user's painted art: final.")]
        public bool artSourceMasterMeansFinal = true;
        [Tooltip("Folder of the 4x masters (project root relative), mirrors Assets/Art.")]
        public string artSourceFolder = "ArtSource";
        public string artFolder = "Assets/Art";
        public string checklistPath = "Docs/ART_CHECKLIST.md";
        public string creditsPath = "Docs/CREDITS.md";
        [Tooltip("Verdict when no rule matches a sprite or texture.")]
        public ArtStatus defaultStatus = ArtStatus.Placeholder;
        public List<ProvenanceRule> rules = new List<ProvenanceRule>();
        public List<SectionRule> sectionRules = new List<SectionRule>();
        public List<SpecRule> specRules = new List<SpecRule>();

        private void Reset() => FillDefaults();

        public void FillDefaults()
        {
            rules = new List<ProvenanceRule>
            {
                new ProvenanceRule { name = "Player art", pathPrefix = "Assets/Art/Player/", verdict = RuleVerdict.Final,
                    reason = "The developer's own player art (CLAUDE.md: never replaced)" },
                new ProvenanceRule { name = "Arm art", pathPrefix = "Assets/Art/Arms/", verdict = RuleVerdict.Final,
                    reason = "The developer's own arm art (CLAUDE.md: never replaced)" },
                new ProvenanceRule { name = "Placeholder folder", pathPrefix = "Assets/Art/Placeholder/", verdict = RuleVerdict.Placeholder,
                    reason = "Lives in Assets/Art/Placeholder (stand-in shapes, M7.5 art, generated VoxVfx textures)" },
                new ProvenanceRule { name = "Menu backdrop", pathPrefix = "Assets/Art/UI/Backdrop/", verdict = RuleVerdict.FromChecklist,
                    checklistText = "Menu background", checklistFallback = ArtStatus.Placeholder,
                    reason = "Generated by BulletHell/Vox/5; ART_CHECKLIST marks the menu background as a placeholder" },
                new ProvenanceRule { name = "Armament icons", pathPrefix = "Assets/Art/UI/VoxKit/", nameGlob = "icon_armament_*", verdict = RuleVerdict.FromChecklist,
                    checklistText = "Armament icons x5", checklistFallback = ArtStatus.Final,
                    reason = "VoxKit icon; the checklist entry decides" },
                new ProvenanceRule { name = "Crate icon", pathPrefix = "Assets/Art/UI/VoxKit/", nameGlob = "icon_harvest_crate*", verdict = RuleVerdict.FromChecklist,
                    checklistText = "Card back", checklistFallback = ArtStatus.Final,
                    reason = "VoxKit icon; the checklist entry (card back / crate) decides" },
                new ProvenanceRule { name = "VoxKit", pathPrefix = "Assets/Art/UI/VoxKit/", verdict = RuleVerdict.Final,
                    reason = "VoxKit is the project's single UI theme (CLAUDE.md); ART_CHECKLIST UI2 marks the kit as done" },
                new ProvenanceRule { name = "Scale test without master", pathPrefix = "Assets/Art/ScaleTest/", verdict = RuleVerdict.Placeholder,
                    reason = "Scale-test art with no ArtSource master" },
                new ProvenanceRule { name = "Audio", pathPrefix = "Assets/Audio/", verdict = RuleVerdict.Placeholder,
                    reason = "CC0 pack sound (Docs/CREDITS.md: all audio is a CC0 placeholder)" },
            };

            sectionRules = new List<SectionRule>
            {
                new SectionRule { section = ArtSection.Ui, propertyContains = "icon" },
                new SectionRule { section = ArtSection.Boss, ownerPathContains = "Assets/Data/Bosses/|Enemy_Pumpking|Enemy_BossPlaceholder|Boss.prefab" },
                new SectionRule { section = ArtSection.Boss, ownerTypeContains = "BossData" },
                new SectionRule { section = ArtSection.Projectiles, ownerPathContains = "Assets/Data/Enemies/Patterns|Assets/Data/Ammo/|Projectile.prefab|EnemyBulletPalette" },
                new SectionRule { section = ArtSection.Projectiles, ownerTypeContains = "AttackPattern|AmmoTypeData" },
                new SectionRule { section = ArtSection.Enemies, ownerPathContains = "Assets/Data/Enemies/|Enemy.prefab" },
                new SectionRule { section = ArtSection.Player, ownerPathContains = "Assets/Data/Player/|Assets/Data/Arms/|Assets/Data/Cosmetics/|Assets/Data/Loadouts/|Assets/Data/AssetRegistry|Player.prefab|Arm.prefab|GladiatorDoll.prefab" },
                new SectionRule { section = ArtSection.Vfx, ownerPathContains = "Assets/Prefabs/Vfx/|Assets/Data/Feedback/|Assets/Art/Materials/" },
                new SectionRule { section = ArtSection.Arena, ownerPathContains = "Assets/Data/Arenas/|Assets/Data/Settings/ArenaArt|Obstacle.prefab|Trap.prefab|Coin.prefab|AmmoPickup.prefab|Assets/Data/Pickups/" },
                new SectionRule { section = ArtSection.Ui, ownerPathContains = "Assets/Data/UI/|Assets/Prefabs/UI/|Assets/Data/Armaments/|Assets/Data/Shop/" },
            };

            specRules = new List<SpecRule>
            {
                new SpecRule { contains = "Playersprite", needs = "Player body is the developer's final art. Keep the 302x315 canvas at PPU 273.9, pivot centre. Full spec: idle, run (4), jump takeoff and land, facings down / side / up." },
                new SpecRule { matchSection = true, section = ArtSection.Player, contains = "Art/Arms/|arm_", needs = "Arm drawn pointing right, 256x256 at PPU 347.8, pivot on the attach point (shoulder). Dark outline, light from top-left." },
                new SpecRule { matchSection = true, section = ArtSection.Player, contains = "part_|Cosmetics", needs = "Paper-doll part on the same canvas as Playersprite (302x315, PPU 273.9, pivot centre), identical neck / shoulder joins, uncompressed. Layer order body < armor < head < accessory." },
                new SpecRule { matchSection = true, section = ArtSection.Player, needs = "Character canvas 768x768 (2x export of the 1536 master), PPU 191.3 (1.15x lock), feet on the pivot, 4 px padding." },
                new SpecRule { matchSection = true, section = ArtSection.Boss, needs = "Boss canvas 1280x1280 (2560 master), PPU 191.3, Single sprite, bilinear, CompressedHQ, pivot at the base of the art. Key poses: idle, windup, attack. Pumpking is about 2.7 P tall." },
                new SpecRule { matchSection = true, section = ArtSection.Enemies, needs = "Character canvas 768x768 (1536 master), PPU 191.3, Single sprite, bilinear, CompressedHQ, pivot at the feet, side facing (flipped in code), key poses idle / windup / attack." },
                new SpecRule { matchSection = true, section = ArtSection.Projectiles, needs = "Bullet canvas 192x192 (384 master). Player bullets WHITE / light grey (tinted by the arm). Enemy bullets: bright core + dark outline, reserved Electric Violet #B44BFF, boss Hot Magenta #FF3DCB, never floor red. 0.15-0.3 P." },
                new SpecRule { matchSection = true, section = ArtSection.Arena, contains = "backdrop|ArenaArt", needs = "Arena 3840x2160 (7680x4320 master), PPU 220, LAYERED: floor + decals, back wall / crowd, side walls, foreground railing and front crowd over gameplay. Never one flattened image." },
                new SpecRule { matchSection = true, section = ArtSection.Arena, contains = "LowWall|Crate|Cabbage", needs = "Low obstacle (<= 0.5 P): 256x128 (512x256 master), pivot at the footprint centre, 'jumpable' cap colour, separate _shadow sprite. Breakable: intact / dmg1 / dmg2 / flat debris." },
                new SpecRule { matchSection = true, section = ArtSection.Arena, contains = "Pillar|Pumpkin", needs = "Tall obstacle (>= 1.5 P): 256x384 (512x768 master), pivot at the footprint centre, separate _shadow sprite. Breakable: dmg1 / dmg2 / debris + chunks." },
                new SpecRule { matchSection = true, section = ArtSection.Arena, contains = "Trap", needs = "Floor trap canvas 768x576 (1536x1152 master), flat (x0.6). States: idle, telegraph (2-4 frames, DANGER #FF4D33), active (3-4), cooldown (2-3)." },
                new SpecRule { matchSection = true, section = ArtSection.Arena, contains = "Coin|Pickup", needs = "Pickup 0.3-0.4 P, 128 canvas, coin spin 6 frames, flat floor glow ellipse (x0.6) underneath." },
                new SpecRule { matchSection = true, section = ArtSection.Arena, needs = "Arena prop: pivot at the footprint centre, flat things squashed by F = 0.6, light from top-left, dark outline." },
                new SpecRule { matchSection = true, section = ArtSection.Vfx, needs = "Small white texture, tinted in code (soft dot, spark streak, smoke puff, shard); floor effects flat (x0.6); telegraph glow tinted DANGER #FF4D33. PPU = pixel width." },
                new SpecRule { matchSection = true, section = ArtSection.Ui, contains = "icon", needs = "Icon, rarity rim drawn white (tinted in code). VoxKit import: Single sprite, PPU 200, uncompressed, 2x art (ART_SPEC icon canvas 384)." },
                new SpecRule { matchSection = true, section = ArtSection.Ui, contains = "Backdrop", needs = "Key art or wide arena shot for the menus, 16:9, blurred or darkened behind the panels." },
                new SpecRule { matchSection = true, section = ArtSection.Ui, contains = "VoxKit", needs = "VoxKit sprite: 2x art, PPU 200, uncompressed, 9-slice borders and tokens from vox_ui_kit_manifest.json (BulletHell/Vox/1 Import Kit Sprites)." },
                new SpecRule { matchSection = true, section = ArtSection.Ui, needs = "Themed UI sprite: VoxKit look (marble, soil-brown outline, corn-gold focus), PPU 200, 9-slice friendly even borders." },
                new SpecRule { matchSection = true, section = ArtSection.Audio, contains = "Music", needs = "Music: stereo Vorbis (quality 0.5), Load Type Streaming, seamless loop points." },
                new SpecRule { matchSection = true, section = ArtSection.Audio, needs = "SFX: mono Vorbis (quality 0.6), Decompress On Load, short (rapid-fire cues under 0.3 s). Reassign the clip on its SfxData / AudioLibrary asset." },
                new SpecRule { needs = "Painted HD PNG RGBA, bilinear, pivot at the base, 4 px padding, dark outline, light from top-left (ART_SPEC section 1)." },
            };
        }

        public static ArtProvenanceRules CreateDefault()
        {
            var r = CreateInstance<ArtProvenanceRules>();
            r.FillDefaults();
            return r;
        }
    }

    /// <summary>Everything the engine needs to know about one asset. Gathered by the editor from the AssetDatabase, built by hand in tests.</summary>
    public sealed class ProvenanceFacts
    {
        public string assetPath = "";
        public ArtAssetKind kind = ArtAssetKind.Sprite;
        public bool hasArtSourceMaster;
        public bool isBuiltIn;
        public bool mentionedInCredits;
    }

    public readonly struct ProvenanceResult
    {
        public readonly ArtStatus status;
        public readonly string rule;
        public readonly string reason;

        public ProvenanceResult(ArtStatus status, string rule, string reason)
        {
            this.status = status;
            this.rule = rule;
            this.reason = reason;
        }

        public bool IsFinal => status == ArtStatus.Final;
    }

    /// <summary>The check marks of Docs/ART_CHECKLIST.md: "- [x] text", "[ ]" pending, "[~]" partial. Several entries may share a line.</summary>
    public sealed class ChecklistIndex
    {
        private readonly List<(ChecklistState state, string text)> entries = new List<(ChecklistState, string)>();

        public int Count => entries.Count;

        public static ChecklistIndex Parse(string markdown)
        {
            var index = new ChecklistIndex();
            if (string.IsNullOrEmpty(markdown))
                return index;
            foreach (string line in markdown.Split('\n'))
            {
                int i = 0;
                while (i < line.Length)
                {
                    int open = line.IndexOf('[', i);
                    if (open < 0 || open + 2 >= line.Length)
                        break;
                    char c = line[open + 1];
                    if (line[open + 2] == ']' && (c == ' ' || c == 'x' || c == 'X' || c == '~'))
                    {
                        int next = FindNextBox(line, open + 3);
                        string text = line.Substring(open + 3, (next < 0 ? line.Length : next) - (open + 3)).Trim();
                        ChecklistState state = c == ' ' ? ChecklistState.Pending : c == '~' ? ChecklistState.Partial : ChecklistState.Done;
                        index.entries.Add((state, text.ToLowerInvariant()));
                        i = next < 0 ? line.Length : next;
                    }
                    else
                    {
                        i = open + 1;
                    }
                }
            }
            return index;
        }

        private static int FindNextBox(string line, int from)
        {
            for (int i = from; i + 2 < line.Length; i++)
            {
                if (line[i] != '[' || line[i + 2] != ']')
                    continue;
                char c = line[i + 1];
                if (c == ' ' || c == 'x' || c == 'X' || c == '~')
                    return i;
            }
            return -1;
        }

        public ChecklistState Lookup(string textFragment)
        {
            if (string.IsNullOrEmpty(textFragment))
                return ChecklistState.NotFound;
            string f = textFragment.ToLowerInvariant();
            foreach (var e in entries)
                if (e.text.Contains(f))
                    return e.state;
            return ChecklistState.NotFound;
        }
    }

    /// <summary>The rule engine: facts + rules (+ checklist) in, FINAL or PLACEHOLDER out. No AssetDatabase access, so it is unit-testable.</summary>
    public static class ArtProvenanceEngine
    {
        public static ProvenanceResult Classify(ArtProvenanceRules rules, ProvenanceFacts facts, ChecklistIndex checklist)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            string path = (facts.assetPath ?? "").Replace('\\', '/');

            if (facts.isBuiltIn || string.IsNullOrEmpty(path))
                return new ProvenanceResult(ArtStatus.Placeholder, "built-in", "Unity built-in or runtime-generated, no source file");

            if (facts.kind == ArtAssetKind.Audio && facts.mentionedInCredits)
                return new ProvenanceResult(ArtStatus.Placeholder, "credits", "Listed in Docs/CREDITS.md as a CC0 placeholder");

            if (facts.kind != ArtAssetKind.Audio && rules.artSourceMasterMeansFinal && facts.hasArtSourceMaster)
                return new ProvenanceResult(ArtStatus.Final, "master", "A 4x master exists in " + rules.artSourceFolder + "/ (painted art)");

            string file = path.Substring(path.LastIndexOf('/') + 1);
            foreach (ProvenanceRule rule in rules.rules)
            {
                if (!string.IsNullOrEmpty(rule.pathPrefix) && !path.StartsWith(rule.pathPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!WildcardMatch(rule.nameGlob, file))
                    continue;
                switch (rule.verdict)
                {
                    case RuleVerdict.Final:
                        return new ProvenanceResult(ArtStatus.Final, rule.name, rule.reason);
                    case RuleVerdict.Placeholder:
                        return new ProvenanceResult(ArtStatus.Placeholder, rule.name, rule.reason);
                    default:
                        ChecklistState s = checklist != null ? checklist.Lookup(rule.checklistText) : ChecklistState.NotFound;
                        if (s == ChecklistState.NotFound)
                            return new ProvenanceResult(rule.checklistFallback, rule.name, rule.reason + " (checklist entry not found)");
                        return new ProvenanceResult(s == ChecklistState.Done ? ArtStatus.Final : ArtStatus.Placeholder, rule.name,
                            rule.reason + " (checklist: " + s.ToString().ToLowerInvariant() + ")");
                }
            }
            return new ProvenanceResult(rules.defaultStatus, "default", "No rule matches this path");
        }

        public static ArtSection SectionOf(ArtProvenanceRules rules, ArtAssetKind assetKind, string ownerPath, string ownerType, string propertyPath)
        {
            if (assetKind == ArtAssetKind.Audio)
                return ArtSection.Audio;
            foreach (SectionRule r in rules.sectionRules)
            {
                if (!ContainsAny(ownerPath, r.ownerPathContains)) continue;
                if (!ContainsAny(ownerType, r.ownerTypeContains)) continue;
                if (!ContainsAny(propertyPath, r.propertyContains)) continue;
                return r.section;
            }
            return ArtSection.Other;
        }

        public static string SpecFor(ArtProvenanceRules rules, ArtSection section, string ownerPath, string assetPath, string propertyPath)
        {
            string hay = (ownerPath ?? "") + "|" + (assetPath ?? "") + "|" + (propertyPath ?? "");
            foreach (SpecRule r in rules.specRules)
            {
                if (r.matchSection && r.section != section) continue;
                if (!string.IsNullOrEmpty(r.contains) && !ContainsAny(hay, r.contains)) continue;
                return r.needs;
            }
            return "";
        }

        /// <summary>Empty pattern = match. Several alternatives separated by '|'.</summary>
        public static bool ContainsAny(string haystack, string alternatives)
        {
            if (string.IsNullOrEmpty(alternatives))
                return true;
            if (haystack == null)
                return false;
            foreach (string alt in alternatives.Split('|'))
            {
                if (alt.Length == 0) continue;
                if (haystack.IndexOf(alt, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary>'*' wildcard only, case-insensitive. Empty pattern matches everything.</summary>
        public static bool WildcardMatch(string pattern, string text)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "*")
                return true;
            string[] parts = pattern.Split('*');
            int pos = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                if (p.Length == 0) continue;
                int idx = text.IndexOf(p, pos, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return false;
                if (i == 0 && idx != 0) return false;
                pos = idx + p.Length;
            }
            string last = parts[parts.Length - 1];
            return last.Length == 0 || text.EndsWith(last, StringComparison.OrdinalIgnoreCase);
        }
    }
}
