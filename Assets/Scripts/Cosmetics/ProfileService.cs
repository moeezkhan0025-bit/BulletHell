using System;
using System.Collections.Generic;
using BulletHell.Save;
using UnityEngine;

namespace BulletHell.Cosmetics
{
    /// <summary>
    /// The player's chosen look. Edits are live (the customization preview reads them) but only Save writes the profile
    /// file; Revert drops unsaved edits. A missing or unknown cosmetic ID falls back to the slot's default (the first
    /// item of that slot in the registry), so a removed asset can't break a profile.
    /// </summary>
    public sealed class ProfileService
    {
        private readonly AssetRegistry registry;
        private readonly JsonFileStore<ProfileData> store;
        private readonly List<CosmeticData>[] optionsBySlot = new List<CosmeticData>[CosmeticSlots.Count];

        public ProfileData Data { get; private set; }

        /// <summary>Raised whenever the chosen look changes (edit, randomize, revert).</summary>
        public event Action Changed;

        public ProfileService(AssetRegistry assetRegistry, JsonFileStore<ProfileData> fileStore)
        {
            registry = assetRegistry;
            store = fileStore;
            Revert();
        }

        /// <summary>Loads the file again (or the defaults when there is none), dropping unsaved edits.</summary>
        public void Revert()
        {
            if (store.TryLoad(out ProfileData loaded) && loaded.version == ProfileData.CurrentVersion && loaded.cosmetics != null)
            {
                Data = loaded;
                if (Data.cosmetics.Length != CosmeticSlots.Count)
                    Array.Resize(ref Data.cosmetics, CosmeticSlots.Count);
            }
            else
            {
                Data = new ProfileData();
            }
            Changed?.Invoke();
        }

        public void Save() => store.Save(Data);

        /// <summary>All items of a slot, in registry order (the first is the default). Empty when the registry has none.</summary>
        public IReadOnlyList<CosmeticData> Options(CosmeticSlot slot)
        {
            List<CosmeticData> list = optionsBySlot[(int)slot];
            if (list == null)
            {
                list = new List<CosmeticData>();
                if (registry != null)
                    registry.GetCosmetics(slot, list);
                optionsBySlot[(int)slot] = list;
            }
            return list;
        }

        /// <summary>The chosen item of a slot; its default when none/unknown is stored. Null only if the slot has no items at all.</summary>
        public CosmeticData Get(CosmeticSlot slot)
        {
            IReadOnlyList<CosmeticData> options = Options(slot);
            if (options.Count == 0)
                return null;

            string id = Data.cosmetics[(int)slot];
            for (int i = 0; i < options.Count; i++)
                if (options[i].Id == id)
                    return options[i];
            return options[0];
        }

        public int IndexOf(CosmeticSlot slot)
        {
            IReadOnlyList<CosmeticData> options = Options(slot);
            CosmeticData current = Get(slot);
            for (int i = 0; i < options.Count; i++)
                if (options[i] == current)
                    return i;
            return 0;
        }

        public void Set(CosmeticSlot slot, CosmeticData item)
        {
            Data.cosmetics[(int)slot] = item != null ? item.Id : "";
            Changed?.Invoke();
        }

        /// <summary>Steps to the next (+1) or previous (-1) item of a slot, wrapping around.</summary>
        public void Cycle(CosmeticSlot slot, int direction)
        {
            IReadOnlyList<CosmeticData> options = Options(slot);
            if (options.Count == 0)
                return;
            int next = ((IndexOf(slot) + direction) % options.Count + options.Count) % options.Count;
            Set(slot, options[next]);
        }

        /// <summary>Picks a random item for every slot. The picker returns a number in [0, count).</summary>
        public void Randomize(Func<int, int> pick = null)
        {
            pick ??= count => UnityEngine.Random.Range(0, count);
            for (int i = 0; i < CosmeticSlots.Count; i++)
            {
                IReadOnlyList<CosmeticData> options = Options((CosmeticSlot)i);
                if (options.Count > 0)
                    Data.cosmetics[i] = options[Mathf.Clamp(pick(options.Count), 0, options.Count - 1)].Id;
            }
            Changed?.Invoke();
        }
    }
}
