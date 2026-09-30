using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Save
{
    /// <summary>Converts between the live RunState and the plain SaveData, going through the AssetRegistry for IDs.</summary>
    public static class RunSaveMapper
    {
        public static SaveData ToSave(RunState state)
        {
            var data = new SaveData
            {
                round = state.Round,
                currency = state.Currency,
                loadout = new ArmSave[ArmLoadout.SlotCount],
                ammoSlots = new string[AmmoSlotSet.Count],
                activeAmmoSlot = state.Ammo.ActiveIndex,
                armamentInventory = new string[state.Armaments.Count],
                spareArms = new ArmSave[state.SpareArms.Count],
                shopSeed = state.Shop != null ? state.Shop.Seed : 0,
                shopRerolls = state.Shop != null ? state.Shop.Rerolls : 0,
                shopSoldMask = state.Shop != null ? state.Shop.SoldMask : 0,
            };

            for (int i = 0; i < ArmLoadout.SlotCount; i++)
                data.loadout[i] = ArmToSave(state.Loadout[i]);
            for (int i = 0; i < AmmoSlotSet.Count; i++)
                data.ammoSlots[i] = AssetRegistry.IdOf(state.Ammo.Get(i));
            for (int i = 0; i < state.Armaments.Count; i++)
                data.armamentInventory[i] = AssetRegistry.IdOf(state.Armaments.Get(i));
            for (int i = 0; i < state.SpareArms.Count; i++)
                data.spareArms[i] = ArmToSave(state.SpareArms.Get(i));
            return data;
        }

        /// <summary>
        /// Rebuilds a run from a save. Unknown IDs (an asset that no longer exists) are skipped with a warning.
        /// Returns false when the save can't make a playable run (no arms at all).
        /// </summary>
        public static bool TryFromSave(SaveData data, AssetRegistry registry, out RunState state)
        {
            state = null;
            if (data == null || registry == null)
                return false;

            var result = new RunState { Round = Mathf.Max(1, data.round), Currency = Mathf.Max(0, data.currency) };

            var overflow = new List<ArmamentData>(); // saved armaments that no longer fit (fewer slots, stack limits): back to the inventory
            int armCount = 0;
            for (int i = 0; i < ArmLoadout.SlotCount && data.loadout != null && i < data.loadout.Length; i++)
            {
                result.Loadout[i] = ArmFromSave(data.loadout[i], registry, overflow);
                if (result.Loadout[i] != null)
                    armCount++;
            }
            if (armCount == 0)
                return false;

            if (data.spareArms != null)
                foreach (ArmSave saved in data.spareArms)
                    result.SpareArms.Add(ArmFromSave(saved, registry, overflow));

            if (data.armamentInventory != null)
                foreach (string id in data.armamentInventory)
                    result.Armaments.Add(FindArmament(id, registry));

            foreach (ArmamentData extra in overflow)
                result.Armaments.Add(extra);

            for (int i = 0; i < AmmoSlotSet.Count && data.ammoSlots != null && i < data.ammoSlots.Length; i++)
            {
                if (string.IsNullOrEmpty(data.ammoSlots[i]))
                    continue;
                AmmoTypeData ammo = registry.GetAmmo(data.ammoSlots[i]);
                if (ammo == null)
                    Debug.LogWarning($"Save refers to unknown ammo '{data.ammoSlots[i]}'; slot {i + 1} left empty.");
                else
                    result.Ammo.Set(i, ammo);
            }
            result.Ammo.TrySelect(data.activeAmmoSlot);
            if (data.shopSeed != 0)
                result.Shop = new BulletHell.Shop.ShopVisit { Seed = data.shopSeed, Rerolls = System.Math.Max(0, data.shopRerolls), SoldMask = data.shopSoldMask };

            state = result;
            return true;
        }

        private static ArmSave ArmToSave(ArmInstance arm)
        {
            var saved = new ArmSave();
            if (arm == null)
                return saved;

            saved.armId = AssetRegistry.IdOf(arm.Data);
            saved.armamentIds = new string[arm.SlotCount];
            for (int i = 0; i < arm.SlotCount; i++)
                saved.armamentIds[i] = AssetRegistry.IdOf(arm.GetArmament(i));
            return saved;
        }

        private static ArmInstance ArmFromSave(ArmSave saved, AssetRegistry registry, List<ArmamentData> overflow)
        {
            if (saved == null || string.IsNullOrEmpty(saved.armId))
                return null;

            WeaponArmData data = registry.GetArm(saved.armId);
            if (data == null)
            {
                Debug.LogWarning($"Save refers to unknown arm '{saved.armId}'; skipped.");
                return null;
            }

            var arm = new ArmInstance(data);
            var scratch = new ArmamentInventory(); // equip goes through an inventory; this one is thrown away
            for (int slot = 0; saved.armamentIds != null && slot < saved.armamentIds.Length; slot++)
            {
                ArmamentData armament = FindArmament(saved.armamentIds[slot], registry);
                if (armament == null)
                    continue;
                scratch.Add(armament);
                if (!arm.TryEquipAt(slot, armament, scratch))
                {
                    // No such slot on this arm any more, or its stack limit is full: the armament goes back to the inventory.
                    scratch.Remove(armament);
                    overflow.Add(armament);
                    Debug.LogWarning($"Saved armament '{armament.DisplayName}' no longer fits arm '{data.DisplayName}' (slot {slot + 1}); returned to the inventory.");
                }
            }
            return arm;
        }

        private static ArmamentData FindArmament(string id, AssetRegistry registry)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            ArmamentData armament = registry.GetArmament(id);
            if (armament == null)
                Debug.LogWarning($"Save refers to unknown armament '{id}'; skipped.");
            return armament;
        }
    }
}
