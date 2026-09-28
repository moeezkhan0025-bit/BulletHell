using BulletHell.Core;
using BulletHell.Weapons;

namespace BulletHell.Shop
{
    public enum PurchaseResult { Bought, NotEnoughCurrency, InvalidEntry }

    /// <summary>Buying: spends currency and puts the item in the arm or armament inventory.</summary>
    public static class ShopService
    {
        public static PurchaseResult TryBuy(RunState state, in ShopEntry entry)
        {
            if (!entry.IsValid)
                return PurchaseResult.InvalidEntry;
            if (state.Currency < entry.Price)
                return PurchaseResult.NotEnoughCurrency;

            state.Currency -= entry.Price;
            if (entry.Kind == ShopItemKind.Arm)
                state.SpareArms.Add(new ArmInstance(entry.Arm));
            else
                state.Armaments.Add(entry.Armament);
            return PurchaseResult.Bought;
        }
    }
}
