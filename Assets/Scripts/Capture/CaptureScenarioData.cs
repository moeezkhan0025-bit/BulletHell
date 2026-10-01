using System;
using System.Collections.Generic;
using BulletHell.Enemies;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Capture
{
    /// <summary>One arm placed in a loadout slot, with the armaments it carries (assets only; the run state gets fresh instances).</summary>
    [Serializable]
    public struct CaptureArmSetup
    {
        [Tooltip("Loadout slot 0-7 (N, NE, E, SE, S, SW, W, NW).")]
        [Range(0, ArmLoadout.SlotCount - 1)] public int slot;
        public WeaponArmData arm;
        [Tooltip("Armaments put on this arm in order; extras beyond the arm's slot count or stack limit are ignored.")]
        public ArmamentData[] armaments;
    }

    /// <summary>Extra enemies kept alive on top of the round's own waves (denser, busier screen).</summary>
    [Serializable]
    public struct CaptureExtraSpawn
    {
        public EnemyData enemy;
        [Min(1)] public int keepAlive;
        [Tooltip("Spawned per top-up step.")]
        [Min(1)] public int batch;
    }

    /// <summary>
    /// R1 portfolio capture kit - one capture scenario as data: which round, which loadout and armaments, how much extra
    /// enemy density, how long to capture, and whether the autopilot plays. Applied by <see cref="CaptureDirector"/> to a
    /// FRESH run through the normal services; no asset is ever modified (the run state gets new ArmInstances).
    /// Assets live in Assets/Data/Capture; the Capture menu has one entry per scenario.
    /// </summary>
    [CreateAssetMenu(fileName = "Scenario_", menuName = "BulletHell/Capture/Scenario")]
    public sealed class CaptureScenarioData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Scenario";
        [Tooltip("Start of the file names: hero-loop -> hero-loop_2026-10-01_143005.mp4.")]
        [SerializeField] private string filePrefix = "scenario";

        [Header("Run")]
        [Tooltip("The round this scenario starts in (1-based). The round's own waves play; extras are added on top.")]
        [SerializeField, Min(1)] private int round = 1;

        [Header("Loadout")]
        [Tooltip("Off: the game's starting loadout and ammo. On: the arms below replace the whole loadout.")]
        [SerializeField] private bool customLoadout;
        [SerializeField] private CaptureArmSetup[] arms = new CaptureArmSetup[0];
        [Tooltip("Off: the starting ammo slots. On: the four ammo slots below (empty entries stay empty).")]
        [SerializeField] private bool customAmmo;
        [SerializeField] private AmmoTypeData[] ammo = new AmmoTypeData[AmmoSlotSet.Count];

        [Header("Density")]
        [SerializeField] private CaptureExtraSpawn[] extraSpawns = new CaptureExtraSpawn[0];
        [Tooltip("Seconds after the combat begins during which the extras are topped up (the round can end after that). 0 = no extras.")]
        [SerializeField, Min(0f)] private float extraSpawnSeconds;
        [Tooltip("Seconds between top-up steps.")]
        [SerializeField, Min(0.05f)] private float topUpInterval = 0.4f;

        [Header("Capture")]
        [Tooltip("How long the scenario is meant to be recorded, counted from the start of combat. With 'Auto record scenarios' the recording stops here.")]
        [SerializeField, Min(1f)] private float captureSeconds = 20f;
        [Tooltip("Autopilot numbers (Assets/Data/Capture/CaptureTuning.asset). Needed when the autopilot is on.")]
        [SerializeField] private CaptureTuning tuning;
        [Tooltip("The autopilot plays: dodges bullets, aims, fires, swaps ammo and jumps the boss smash.")]
        [SerializeField] private bool autopilot = true;

        public string DisplayName => displayName;
        public string FilePrefix => filePrefix;
        public int Round => round;
        public bool CustomLoadout => customLoadout;
        public IReadOnlyList<CaptureArmSetup> Arms => arms ?? Array.Empty<CaptureArmSetup>();
        public bool CustomAmmo => customAmmo;
        public IReadOnlyList<AmmoTypeData> Ammo => ammo ?? Array.Empty<AmmoTypeData>();
        public IReadOnlyList<CaptureExtraSpawn> ExtraSpawns => extraSpawns ?? Array.Empty<CaptureExtraSpawn>();
        public float ExtraSpawnSeconds => extraSpawnSeconds;
        public float TopUpInterval => topUpInterval;
        public float CaptureSeconds => captureSeconds;
        public bool Autopilot => autopilot;
        public CaptureTuning Tuning => tuning;

        /// <summary>Problems that would make the scenario misbehave (empty list = valid). Used by the tests and the Capture menu.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(filePrefix))
                problems.Add("file prefix is empty");
            else if (CaptureNaming.Sanitize(filePrefix) != filePrefix)
                problems.Add($"file prefix '{filePrefix}' is not file-safe (use lower-case letters, digits and dashes)");
            if (round < 1)
                problems.Add("round must be 1 or more");
            if (autopilot && tuning == null)
                problems.Add("autopilot is on but no CaptureTuning is assigned");
            if (captureSeconds <= 0f)
                problems.Add("capture seconds must be above 0");

            if (customLoadout)
            {
                if (arms == null || arms.Length == 0)
                    problems.Add("custom loadout has no arms");
                var used = new HashSet<int>();
                for (int i = 0; arms != null && i < arms.Length; i++)
                {
                    if (arms[i].arm == null)
                        problems.Add($"arm entry {i} has no arm");
                    if (arms[i].slot < 0 || arms[i].slot >= ArmLoadout.SlotCount)
                        problems.Add($"arm entry {i} has slot {arms[i].slot} (0-{ArmLoadout.SlotCount - 1})");
                    else if (!used.Add(arms[i].slot))
                        problems.Add($"slot {arms[i].slot} is used twice");
                    if (arms[i].armaments != null)
                        foreach (ArmamentData armament in arms[i].armaments)
                            if (armament == null)
                                problems.Add($"arm entry {i} has an empty armament entry");
                }
            }

            if (customAmmo)
            {
                bool any = false;
                if (ammo != null)
                    foreach (AmmoTypeData a in ammo)
                        any |= a != null;
                if (!any)
                    problems.Add("custom ammo has no ammo type");
                if (ammo != null && ammo.Length > AmmoSlotSet.Count)
                    problems.Add($"more than {AmmoSlotSet.Count} ammo entries");
            }

            if (extraSpawnSeconds > 0f && (extraSpawns == null || extraSpawns.Length == 0))
                problems.Add("extra spawn seconds is set but there are no extra spawns");
            for (int i = 0; extraSpawns != null && i < extraSpawns.Length; i++)
                if (extraSpawns[i].enemy == null)
                    problems.Add($"extra spawn {i} has no enemy");
            return problems;
        }

#if UNITY_EDITOR
        /// <summary>Editor setup only (the Capture asset creator and the tests): fills every field.</summary>
        public void EditorConfigure(string name, string prefix, int roundNumber, float seconds, bool autopilotOn,
                                    CaptureArmSetup[] armSetups, AmmoTypeData[] ammoSlots,
                                    CaptureExtraSpawn[] extras, float extraSeconds, CaptureTuning autopilotTuning, float topUpSeconds = 0.4f)
        {
            displayName = name;
            filePrefix = prefix;
            round = roundNumber;
            captureSeconds = seconds;
            autopilot = autopilotOn;
            customLoadout = armSetups != null && armSetups.Length > 0;
            arms = armSetups ?? new CaptureArmSetup[0];
            customAmmo = ammoSlots != null && ammoSlots.Length > 0;
            ammo = ammoSlots ?? new AmmoTypeData[AmmoSlotSet.Count];
            extraSpawns = extras ?? new CaptureExtraSpawn[0];
            extraSpawnSeconds = extraSeconds;
            topUpInterval = topUpSeconds;
            tuning = autopilotTuning;
        }
#endif
    }
}
