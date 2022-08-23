#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models
{
    public enum ItemType
    {
        AffectionRecoverySmall,
        AffectionRecoveryNormal,
        AffectionRecoveryBig,
        IncreaseUpgradeCnt,
        CardParamsReRoll,
        DereReRoll,
        RandomBoosterPackSingleE,
        RandomNormalBoosterPackB,
        RandomTitleBoosterPackSingleE,
        RandomNormalBoosterPackA,
        RandomNormalBoosterPackS,
        RandomNormalBoosterPackSS,
        AffectionRecoveryGreat,
        BetterIncreaseUpgradeCnt,
        CheckAffection,
        SetCustomImage,
        IncreaseExpSmall,
        IncreaseExpBig,
        ChangeStarType,
        SetCustomBorder,
        ChangeCardImage,

        PreAssembledMegumin,
        PreAssembledGintoki,
        PreAssembledAsuna,

        FigureSkeleton,
        FigureUniversalPart,
        FigureHeadPart,
        FigureBodyPart,
        FigureLeftArmPart,
        FigureRightArmPart,
        FigureLeftLegPart,
        FigureRightLegPart,
        FigureClothesPart,

        BigRandomBoosterPackE,
        ResetCardValue,
        LotteryTicket,

        IncreaseUltimateAttack,
        IncreaseUltimateDefence,
        IncreaseUltimateHealth,
        IncreaseUltimateAll
    }

    public class Item
    {
        public ulong Id { get; set; }
        public long Count { get; set; }
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public Quality Quality { get; set; }

        public ulong GameDeckId { get; set; }
        [JsonIgnore]
        public virtual GameDeck GameDeck { get; set; }
    }
}
