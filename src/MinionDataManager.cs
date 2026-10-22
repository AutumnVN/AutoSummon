using Terraria;

namespace AutoSummon
{
    public static class MinionUtil
    {
        public static float CountMinionSlotsUsed()
        {
            float minionSlotsUsed = 0f;
            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active && proj.owner == Main.myPlayer && proj.minion)
                {
                    minionSlotsUsed += proj.minionSlots;
                }
            }

            return minionSlotsUsed;
        }

        public static void RemoveAllSummonMinions()
        {
            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active && proj.owner == Main.myPlayer && proj.minion)
                {
                    proj.Kill();
                }
            }
        }

        public static bool CheckItemSummonsMinions(Item item)
        {
            return item != null && item.summon && !item.sentry;
        }
    }

    public class MinionColumnItems
    {
        public int Length { get; } = 5;

        private Item[] previousMinionSlotItem;

        public MinionColumnItems()
        {
            previousMinionSlotItem = new Item[Length];
            for (int i = 0; i < Length; i++)
            {
                previousMinionSlotItem[i] = new Item();
            }
        }

        public bool CheckItemChange(int minionColumnIndex)
        {
            Player player = Main.player[Main.myPlayer];

            for (int i = 0; i < Length; ++i)
            {
                Item current = player.inventory[minionColumnIndex + 10 * i];
                if (previousMinionSlotItem[i] != current &&
                   (MinionUtil.CheckItemSummonsMinions(previousMinionSlotItem[i])
                    || MinionUtil.CheckItemSummonsMinions(current)))
                {
                    return true;
                }
            }

            return false;
        }

        public void UpdateItems(int minionColumnIndex)
        {
            Player player = Main.player[Main.myPlayer];

            for (int i = 0; i < Length; ++i)
            {
                previousMinionSlotItem[i] = player.inventory[minionColumnIndex + 10 * i];
            }
        }
    }
}
