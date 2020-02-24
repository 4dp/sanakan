#pragma warning disable 1591

using Sanakan.Database.Models;

namespace Sanakan.Extensions
{
    public static class ChestExtension
    {
        public static int GetMaxExpTransferToCard(this ExpContainer c)
        {
            switch (c.Level)
            {
                case ExpContainerLevel.Level1:
                    return 30;

                case ExpContainerLevel.Level2:
                    return 80;

                case ExpContainerLevel.Level3:
                    return 200;

                default:
                case ExpContainerLevel.Disabled:
                    return 0;
            }
        }

        public static int GetMaxExpTransferToChest(this ExpContainer c)
        {
            switch (c.Level)
            {
                case ExpContainerLevel.Level1:
                    return 50;

                case ExpContainerLevel.Level2:
                    return 100;

                case ExpContainerLevel.Level3:
                    return -1; //unlimited

                default:
                case ExpContainerLevel.Disabled:
                    return 0;
            }
        }

        public static int GetTransferCTCost(this ExpContainer c)
        {
            switch (c.Level)
            {
                case ExpContainerLevel.Level1:
                    return 10;

                case ExpContainerLevel.Level2:
                    return 15;

                case ExpContainerLevel.Level3:
                    return 12;

                default:
                case ExpContainerLevel.Disabled:
                    return 100;
            }
        }

        public static int GetChestUpgradeCostInCards(this ExpContainer c)
        {
            switch (c.Level)
            {
                case ExpContainerLevel.Disabled:
                case ExpContainerLevel.Level1:
                    return 1;

                case ExpContainerLevel.Level2:
                    return 2;

                default:
                case ExpContainerLevel.Level3:
                    return -1; //can't upgrade
            }
        }

        public static int GetChestUpgradeCostInBlood(this ExpContainer c)
        {
            switch (c.Level)
            {
                case ExpContainerLevel.Disabled:
                    return 5;

                case ExpContainerLevel.Level1:
                    return 10;

                case ExpContainerLevel.Level2:
                    return 15;

                default:
                case ExpContainerLevel.Level3:
                    return -1; //can't upgrade
            }
        }
    }
}
