namespace BulletHell.Shop
{
    /// <summary>
    /// One visit to the Shop: which stock the player sees and what has been bought. The stock itself is never stored: it is
    /// generated again from (Seed, round, Rerolls), so saving these three numbers is enough for Continue to show exactly the
    /// same shop. Offers are numbered: arm cards first, then armament cards, then the crate.
    /// </summary>
    public sealed class ShopVisit
    {
        /// <summary>Random seed of this visit. 0 means "no visit yet".</summary>
        public int Seed;
        /// <summary>How many times the stock was rerolled this visit (the reroll cost rises with it).</summary>
        public int Rerolls;
        /// <summary>Bit i is set when offer i is SOLD.</summary>
        public int SoldMask;
        /// <summary>The crate was bought and its choice is still open (not saved: quitting leaves the crate unbought).</summary>
        public bool CratePending;

        public static ShopVisit Create(int seed) => new ShopVisit { Seed = seed };

        public bool IsSold(int offer) => offer >= 0 && offer < 31 && (SoldMask & (1 << offer)) != 0;

        public void MarkSold(int offer)
        {
            if (offer >= 0 && offer < 31)
                SoldMask |= 1 << offer;
        }

        /// <summary>A reroll gives new stock: every offer is for sale again.</summary>
        public void NextStock()
        {
            Rerolls++;
            SoldMask = 0;
            CratePending = false;
        }
    }
}
