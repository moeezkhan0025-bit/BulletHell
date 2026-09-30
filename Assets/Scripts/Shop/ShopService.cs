using BulletHell.Core;
using BulletHell.Telemetry;
using BulletHell.Weapons;

namespace BulletHell.Shop
{
    public enum PurchaseResult { Bought, NotEnoughCurrency, InvalidEntry, AlreadySold }

    /// <summary>Buying: spends currency and puts the item in the arm or armament inventory. Rerolling and the crate.</summary>
    public static class ShopService
    {
        /// <summary>Buys an arm or an armament (not the crate: see <see cref="BuyCrate"/>).</summary>
        public static PurchaseResult TryBuy(RunState state, in ShopEntry entry)
        {
            if (!entry.IsValid || entry.Kind == ShopItemKind.Crate)
                return PurchaseResult.InvalidEntry;
            if (state.Currency < entry.Price)
                return PurchaseResult.NotEnoughCurrency;

            state.Currency -= entry.Price;
            if (entry.Kind == ShopItemKind.Arm)
                state.SpareArms.Add(new ArmInstance(entry.Arm));
            else
                state.Armaments.Add(entry.Armament);
            TelemetryEvents.RaiseSpent(entry.Price, SpendKind.Item);
            if (entry.Kind == ShopItemKind.Arm)
                TelemetryEvents.RaiseItemAcquired(ItemKind.Arm, entry.Arm.DisplayName);
            else
                TelemetryEvents.RaiseItemAcquired(ItemKind.Armament, entry.Armament.DisplayName);
            return PurchaseResult.Bought;
        }

        /// <summary>Buys offer number <paramref name="offer"/> of this visit and marks it SOLD.</summary>
        public static PurchaseResult BuyOffer(RunState state, ShopVisit visit, int offer, in ShopEntry entry)
        {
            if (visit.IsSold(offer))
                return PurchaseResult.AlreadySold;
            PurchaseResult result = TryBuy(state, entry);
            if (result == PurchaseResult.Bought)
                visit.MarkSold(offer);
            return result;
        }

        /// <summary>Cost of the next reroll of this visit.</summary>
        public static int RerollCost(ShopVisit visit, ShopTuning tuning) => tuning.RerollCost(visit.Rerolls);

        /// <summary>Pays for a reroll and gives the visit new stock. False (nothing changes) when the player cannot afford it.</summary>
        public static bool TryReroll(RunState state, ShopVisit visit, ShopTuning tuning)
        {
            int cost = RerollCost(visit, tuning);
            if (state.Currency < cost || visit.CratePending)
                return false;
            state.Currency -= cost;
            visit.NextStock();
            TelemetryEvents.RaiseSpent(cost, SpendKind.Reroll);
            return true;
        }

        /// <summary>
        /// Pays for the crate: its choice is now open and must be resolved with <see cref="PickCrate"/>. The crate is not SOLD
        /// (and nothing should be saved) until a choice is made.
        /// </summary>
        public static PurchaseResult BuyCrate(RunState state, ShopVisit visit, int crateOffer, in ShopEntry crate)
        {
            if (visit.IsSold(crateOffer) || visit.CratePending)
                return PurchaseResult.AlreadySold;
            if (crate.Kind != ShopItemKind.Crate)
                return PurchaseResult.InvalidEntry;
            if (state.Currency < crate.Price)
                return PurchaseResult.NotEnoughCurrency;
            state.Currency -= crate.Price;
            visit.CratePending = true;
            TelemetryEvents.RaiseSpent(crate.Price, SpendKind.Crate);
            return PurchaseResult.Bought;
        }

        /// <summary>Takes the chosen armament out of the open crate: it goes to the inventory and the crate is SOLD.</summary>
        public static bool PickCrate(RunState state, ShopVisit visit, int crateOffer, ArmamentData chosen)
        {
            if (!visit.CratePending || chosen == null)
                return false;
            state.Armaments.Add(chosen);
            visit.CratePending = false;
            visit.MarkSold(crateOffer);
            TelemetryEvents.RaiseItemAcquired(ItemKind.Armament, chosen.DisplayName);
            return true;
        }
    }
}
