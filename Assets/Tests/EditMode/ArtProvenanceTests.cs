using BulletHell.Capture;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class ArtProvenanceTests
    {
        private ArtProvenanceRules rules;

        [SetUp]
        public void SetUp() => rules = ArtProvenanceRules.CreateDefault();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(rules);

        private static ProvenanceFacts Sprite(string path, bool master = false) =>
            new ProvenanceFacts { assetPath = path, kind = ArtAssetKind.Sprite, hasArtSourceMaster = master };

        private ProvenanceResult Run(ProvenanceFacts f, string checklist = "") =>
            ArtProvenanceEngine.Classify(rules, f, ChecklistIndex.Parse(checklist));

        [Test]
        public void PlayerAndArmArtAreFinal()
        {
            Assert.IsTrue(Run(Sprite("Assets/Art/Player/Playersprite.png")).IsFinal);
            Assert.IsTrue(Run(Sprite("Assets/Art/Arms/arm_red.png")).IsFinal);
        }

        [Test]
        public void PlaceholderFolderIsPlaceholder()
        {
            Assert.IsFalse(Run(Sprite("Assets/Art/Placeholder/Circle.png")).IsFinal);
            Assert.IsFalse(Run(Sprite("Assets/Art/Placeholder/Vfx/Vox/glow.png")).IsFinal);
        }

        [Test]
        public void ArtSourceMasterMakesPaintedArtFinal()
        {
            Assert.IsTrue(Run(Sprite("Assets/Art/ScaleTest/enemy_chaser_idle_side.png", master: true)).IsFinal);
            Assert.IsFalse(Run(Sprite("Assets/Art/ScaleTest/unfinished.png", master: false)).IsFinal);
        }

        [Test]
        public void MasterRuleCanBeSwitchedOff()
        {
            rules.artSourceMasterMeansFinal = false;
            Assert.IsFalse(Run(Sprite("Assets/Art/ScaleTest/enemy_chaser_idle_side.png", master: true)).IsFinal);
        }

        [Test]
        public void PlaceholderFolderWinsOverNothingButMasterWinsOverPath()
        {
            // a master next to a placeholder-folder file still means painted art (the master is the stronger fact)
            Assert.IsTrue(Run(Sprite("Assets/Art/Placeholder/x.png", master: true)).IsFinal);
        }

        [Test]
        public void VoxKitIsFinalButPendingChecklistIconsArePlaceholder()
        {
            string checklist = "- [ ] [2] Armament icons x5: Homing, Auto-fire\n- [x] [2] Ammo icons x4: Basic";
            Assert.IsTrue(Run(Sprite("Assets/Art/UI/VoxKit/panel_marble.png"), checklist).IsFinal);
            Assert.IsTrue(Run(Sprite("Assets/Art/UI/VoxKit/icon_ammo_basic.png"), checklist).IsFinal);
            ProvenanceResult r = Run(Sprite("Assets/Art/UI/VoxKit/icon_armament_homing.png"), checklist);
            Assert.IsFalse(r.IsFinal);
            StringAssert.Contains("pending", r.reason);
        }

        [Test]
        public void CheckedChecklistEntryTurnsIconFinal()
        {
            string checklist = "- [x] [2] Armament icons x5: Homing";
            Assert.IsTrue(Run(Sprite("Assets/Art/UI/VoxKit/icon_armament_pierce.png"), checklist).IsFinal);
        }

        [Test]
        public void MissingChecklistEntryUsesFallback()
        {
            Assert.IsTrue(Run(Sprite("Assets/Art/UI/VoxKit/icon_armament_pierce.png"), "").IsFinal);
        }

        [Test]
        public void AudioIsPlaceholderWhenCreditedOrUnderAudioFolder()
        {
            var credited = new ProvenanceFacts { assetPath = "Assets/Elsewhere/boom.ogg", kind = ArtAssetKind.Audio, mentionedInCredits = true };
            Assert.IsFalse(Run(credited).IsFinal);
            var folder = new ProvenanceFacts { assetPath = "Assets/Audio/Sfx/jump.ogg", kind = ArtAssetKind.Audio };
            Assert.IsFalse(Run(folder).IsFinal);
        }

        [Test]
        public void BuiltInAndUnknownAreHandledConservatively()
        {
            Assert.IsFalse(Run(new ProvenanceFacts { assetPath = "", isBuiltIn = true }).IsFinal);
            Assert.IsFalse(Run(Sprite("Assets/Somewhere/mystery.png")).IsFinal);
            rules.defaultStatus = ArtStatus.Final;
            Assert.IsTrue(Run(Sprite("Assets/Somewhere/mystery.png")).IsFinal);
        }

        [Test]
        public void ChecklistParserReadsAllThreeStatesAndSeveralEntriesPerLine()
        {
            var index = ChecklistIndex.Parse("- [x] Player (body)\n- [~] [2] Heart: full, empty\n- [ ] idle (1)  - [x] run cycle (4)");
            Assert.AreEqual(ChecklistState.Done, index.Lookup("player (body)"));
            Assert.AreEqual(ChecklistState.Partial, index.Lookup("Heart"));
            Assert.AreEqual(ChecklistState.Pending, index.Lookup("idle"));
            Assert.AreEqual(ChecklistState.Done, index.Lookup("run cycle"));
            Assert.AreEqual(ChecklistState.NotFound, index.Lookup("unicorn"));
            Assert.AreEqual(4, index.Count);
        }

        [Test]
        public void WildcardMatching()
        {
            Assert.IsTrue(ArtProvenanceEngine.WildcardMatch("icon_armament_*", "icon_armament_homing.png"));
            Assert.IsFalse(ArtProvenanceEngine.WildcardMatch("icon_armament_*", "icon_ammo_basic.png"));
            Assert.IsTrue(ArtProvenanceEngine.WildcardMatch("*.png", "a.PNG"));
            Assert.IsTrue(ArtProvenanceEngine.WildcardMatch("", "anything"));
            Assert.IsFalse(ArtProvenanceEngine.WildcardMatch("arm_*", "x_arm_red.png"));
        }

        [Test]
        public void SectionsFollowOwnerAndProperty()
        {
            Assert.AreEqual(ArtSection.Boss, ArtProvenanceEngine.SectionOf(rules, ArtAssetKind.Sprite, "Assets/Data/Enemies/Enemy_Pumpking.asset", "EnemyData", "paintedSprite"));
            Assert.AreEqual(ArtSection.Enemies, ArtProvenanceEngine.SectionOf(rules, ArtAssetKind.Sprite, "Assets/Data/Enemies/Enemy_Grunt.asset", "EnemyData", "paintedSprite"));
            Assert.AreEqual(ArtSection.Ui, ArtProvenanceEngine.SectionOf(rules, ArtAssetKind.Sprite, "Assets/Data/Ammo/Ammo_Basic.asset", "AmmoTypeData", "icon"));
            Assert.AreEqual(ArtSection.Projectiles, ArtProvenanceEngine.SectionOf(rules, ArtAssetKind.Sprite, "Assets/Data/Ammo/Ammo_Basic.asset", "AmmoTypeData", "projectileSprite"));
            Assert.AreEqual(ArtSection.Player, ArtProvenanceEngine.SectionOf(rules, ArtAssetKind.Sprite, "Assets/Data/Arms/Arm_Red.asset", "WeaponArmData", "sprite"));
            Assert.AreEqual(ArtSection.Arena, ArtProvenanceEngine.SectionOf(rules, ArtAssetKind.Sprite, "Assets/Data/Settings/ArenaArt.asset", "ArenaArt", "backdrop"));
            Assert.AreEqual(ArtSection.Audio, ArtProvenanceEngine.SectionOf(rules, ArtAssetKind.Audio, "Assets/Data/Audio/Sfx_Jump.asset", "SfxData", "clips.Array.data[0]"));
            Assert.AreEqual(ArtSection.Other, ArtProvenanceEngine.SectionOf(rules, ArtAssetKind.Sprite, "Assets/Resources/GameConfig.asset", "GameConfig", "x"));
        }

        [Test]
        public void SpecComesFromTheFirstMatchingRule()
        {
            string boss = ArtProvenanceEngine.SpecFor(rules, ArtSection.Boss, "Assets/Data/Enemies/Enemy_Pumpking.asset", "Assets/Art/Bosses/Pumpking/boss_pumpking_idle.png", "paintedSprite");
            StringAssert.Contains("1280x1280", boss);
            string sfx = ArtProvenanceEngine.SpecFor(rules, ArtSection.Audio, "Assets/Data/Audio/Sfx_Jump.asset", "Assets/Audio/Sfx/jump.ogg", "clips");
            StringAssert.Contains("mono", sfx);
            string music = ArtProvenanceEngine.SpecFor(rules, ArtSection.Audio, "Assets/Data/Audio/AudioLibrary.asset", "Assets/Audio/Music/music_boss.ogg", "music");
            StringAssert.Contains("Streaming", music);
            Assert.IsNotEmpty(ArtProvenanceEngine.SpecFor(rules, ArtSection.Other, "", "", ""));
        }
    }
}
