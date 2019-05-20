#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Shinden;
using Shinden.Models;

namespace Sanakan.Services.PocketWaifu
{
    public enum FightWinner
    {
        Card1, Card2, Draw
    }

    public enum FAEvent
    {
        None, ExtraShield
    }

    public enum BodereBonus
    {
        None, Minus, Plus
    }

    public class Waifu
    {
        private static CharacterIdUpdate CharId = new CharacterIdUpdate();

        private ImageProcessing _img;

        public Waifu(ImageProcessing img)
        {
            _img = img;
        }

        public static Rarity RandomizeRarity()
        {
            var num = Fun.GetRandomValue(1000);
            if (num < 5)   return Rarity.SS;
            if (num < 25)  return Rarity.S;
            if (num < 75)  return Rarity.A;
            if (num < 175) return Rarity.B;
            if (num < 370) return Rarity.C;
            if (num < 650) return Rarity.D;
            return Rarity.E;
        }

        public static Rarity RandomizeRarity(List<Rarity> rarityExcluded)
        {
            if (rarityExcluded == null) return RandomizeRarity();
            if (rarityExcluded.Count < 1) return RandomizeRarity();

            var list = new List<RarityChance>()
            {
                new RarityChance(5,    Rarity.SS),
                new RarityChance(25,   Rarity.S ),
                new RarityChance(75,   Rarity.A ),
                new RarityChance(175,  Rarity.B ),
                new RarityChance(370,  Rarity.C ),
                new RarityChance(650,  Rarity.D ),
                new RarityChance(1000, Rarity.E ),
            };

            var ex = list.Where(x => rarityExcluded.Any(c => c == x.Rarity)).ToList();
            foreach(var e in ex) list.Remove(e);

            var num = Fun.GetRandomValue(1000);
            foreach(var rar in list)
            {
                if (num < rar.Chance)
                    return rar.Rarity;
            }
            return list.Last().Rarity;
        }

        public static ItemType RandomizeItemFromFight()
        {
            var num = Fun.GetRandomValue(1000);
            if (num < 2) return ItemType.IncreaseUpgradeCnt;
            if (num < 20) return ItemType.CardParamsReRoll;
            if (num < 65) return ItemType.AffectionRecoveryBig;
            if (num < 200) return ItemType.DereReRoll;
            if (num < 380) return ItemType.AffectionRecoveryNormal;
            return ItemType.AffectionRecoverySmall;
        }

        public static ItemWithCost[] GetItemsWithCost()
        {
            return new ItemWithCost[]
            {
                new ItemWithCost(10,    ItemType.AffectionRecoverySmall.ToItem()),
                new ItemWithCost(35,    ItemType.AffectionRecoveryNormal.ToItem()),
                new ItemWithCost(300,   ItemType.AffectionRecoveryBig.ToItem()),
                new ItemWithCost(50,    ItemType.DereReRoll.ToItem()),
                new ItemWithCost(80,    ItemType.CardParamsReRoll.ToItem()),
                new ItemWithCost(5000,  ItemType.IncreaseUpgradeCnt.ToItem()),
                new ItemWithCost(100,   ItemType.RandomBoosterPackSingleE.ToItem()),
                new ItemWithCost(1800,  ItemType.RandomTitleBoosterPackSingleE.ToItem()),
                new ItemWithCost(1500,  ItemType.RandomNormalBoosterPackB.ToItem()),
            };
        }

        public static double GetExpToUpgrade(Card toUp, Card toSac, bool wild = false)
        {
            double rExp = 30f / (wild ? 100f : 30f);
            if (toUp.Id == toSac.Id) rExp = 30f / 5f;

            var sacVal = (int) toSac.Rarity;
            var upVal = (int) toUp.Rarity;
            var diff = upVal - sacVal;

            if (diff < 0)
            {
                diff = -diff;
                for (int i = 0; i < diff; i++) rExp /= 2;
            }
            else if (diff > 0)
            {
                for (int i = 0; i < diff; i++) rExp *= 1.5;
            }

            return rExp;
        }

        public static FightWinner GetFightWinner(CardInfo card1, CardInfo card2, BodereBonus diffInSex)
        {
            var FAcard1 = GetFA(card1, card2, out var evt1, diffInSex);
            var FAcard2 = GetFA(card2, card1, out var evt2, diffInSex);

            var winner = FightWinner.Draw;
            if (FAcard1 > FAcard2 + 1) winner = FightWinner.Card1;
            if (FAcard2 > FAcard1 + 1) winner = FightWinner.Card2;

            // extra shield from bonuses
            if ((evt1 != FAEvent.None || evt2 != FAEvent.None) && winner != FightWinner.Draw)
            {
                bool c1 = evt1 == FAEvent.ExtraShield && winner == FightWinner.Card2;
                bool c2 = evt2 == FAEvent.ExtraShield && winner == FightWinner.Card1;
                if (c1 || c2) winner = FightWinner.Draw;
            }
            
            // kamidere && deredere
            if (winner == FightWinner.Draw)
                return CheckKamidereAndDeredere(card1, card2);

            return winner;
        }

        private static FightWinner CheckKamidereAndDeredere(CardInfo card1, CardInfo card2)
        {
            if (card1.Card.Dere == Dere.Kamidere)
            {
                if (card2.Card.Dere == Dere.Kamidere)
                    return FightWinner.Draw;

                return FightWinner.Card1;
            }

            if (card2.Card.Dere == Dere.Kamidere)
            {
                return FightWinner.Card2;
            }

            bool card1Lose = false;
            bool card2Lose = false;

            if (card1.Card.Dere == Dere.Deredere)
                card1Lose = Fun.TakeATry(2);

            if (card2.Card.Dere == Dere.Deredere)
                card2Lose = Fun.TakeATry(2);

            if (card1Lose && card2Lose) return FightWinner.Draw;
            if (card1Lose) return FightWinner.Card2;
            if (card2Lose) return FightWinner.Card1;

            return FightWinner.Draw;
        }

        public static double GetFA(CardInfo target, CardInfo enemy, out FAEvent evt, BodereBonus bodere)
        {
            evt = FAEvent.None;

            double atk1 = target.Card.Attack;
            double def1 = target.Card.Defence;
            if (!target.Info.HasImage)
            {
                atk1 -= atk1 * 20 / 100;
                def1 -= def1 * 20 / 100;
            }

            TryApplyDereBonus(target.Card.Dere, ref atk1, ref def1, bodere);
            if (atk1 < 1) atk1 = 1;
            if (def1 < 1) def1 = 1;

            double atk2 = enemy.Card.Attack;
            double def2 = enemy.Card.Defence;
            if (!enemy.Info.HasImage)
            {
                atk2 -= atk2 * 20 / 100;
                def2 -= def2 * 20 / 100;
            }

            TryApplyDereBonus(enemy.Card.Dere, ref atk2, ref def2, bodere);
            if (atk2 < 1) atk2 = 1;
            if (def2 < 1) def2 = 1;

            if (def2 > 99) def2 = 99;
            if (def1 >= 100) evt = FAEvent.ExtraShield;

            return atk1 * (100 - def2) / 100;
        }

        private static void TryApplyDereBonus(Dere dere, ref double atk, ref double def, BodereBonus bodere)
        {
            if (dere == Dere.Bodere)
            {
                switch (bodere)
                {
                    case BodereBonus.Minus: 
                        atk -= atk / 10;
                    break;

                    case BodereBonus.Plus: 
                        atk += atk / 10;
                    break;

                    default:
                    break;
                }
            }
            else if (Fun.TakeATry(5))
            {
                var tenAtk = atk / 10;
                var tenDef = def / 10;

                switch(dere)
                {
                    case Dere.Yandere:
                        atk += tenAtk;
                    break;

                    case Dere.Dandere:
                        def += tenDef;
                    break;

                    case Dere.Kuudere:
                        atk -= tenAtk;
                        def += tenAtk;
                    break;

                    case Dere.Mayadere:
                        def -= tenDef;
                        atk += tenDef;
                    break;

                    default:
                    break;
                }
            }
        }

        public static Card GenerateNewCard(ulong characterId, string name) => GenerateNewCard(characterId, name, RandomizeRarity());

        public static Card GenerateNewCard(ulong characterId, string name, Rarity rarity)
        {
            return new Card
            {
                Defence = RandomizeDefence(rarity),
                Attack = RandomizeAttack(rarity),
                Character = characterId,
                Dere = RandomizeDere(),
                Rarity = rarity,
                Name = name,
            };
        }

        public static int RandomizeAttack(Rarity rarity)
            => Fun.GetRandomValue(rarity.GetAttackMin(), rarity.GetAttackMax() + 1);

        public static int RandomizeDefence(Rarity rarity)
            => Fun.GetRandomValue(rarity.GetDefenceMin(), rarity.GetDefenceMax() + 1);

        public static Dere RandomizeDere()
        {
            var allDere = Enum.GetValues(typeof(Dere)).Cast<Dere>();
            return Fun.GetOneRandomFrom(allDere);
        }

        public static Card GenerateNewCard(ICharacterInfo character)
            => GenerateNewCard(character.Id, character.ToString());

        public static Card GenerateNewCard(ICharacterInfo character, Rarity rarity)
            => GenerateNewCard(character.Id, character.ToString(), rarity);

        public static Card GenerateNewCard(ICharacterInfo character, List<Rarity> rarityExcluded)
        {
            var rarity = RandomizeRarity(rarityExcluded);

            return new Card
            {
                Defence = RandomizeDefence(rarity),
                Attack = RandomizeAttack(rarity),
                Name = character.ToString(),
                Character = character.Id,
                Dere = RandomizeDere(),
                Rarity = rarity,
            };
        }

        private static int ScaleNumber(int oMin, int oMax, int nMin, int nMax, int value)
        {
            var m = (double)(nMax - nMin)/(double)(oMax - oMin);
            var c = (oMin * m) - nMin;

            return (int)((m * value) - c);
        }

        public static int GetAttactAfterLevelUp(Rarity oldRarity, int oldAtk)
        {
            var newRarity = oldRarity - 1;
            var newMax = newRarity.GetAttackMax();
            var newMin = newRarity.GetAttackMin();
            var range = newMax - newMin;

            var oldMax = oldRarity.GetAttackMax();
            var oldMin = oldRarity.GetAttackMin();

            var relNew = ScaleNumber(oldMin, oldMax, newMin, newMax, oldAtk);
            var relMin = relNew - (range * 6 / 100);
            var relMax = relNew + (range * 8 / 100);

            var nAtk = Fun.GetRandomValue(relMin, relMax + 1);
            if (nAtk > newMax) nAtk = newMax;
            if (nAtk < newMin) nAtk = newMin;

            return nAtk;
        }

        public static int GetDefenceAfterLevelUp(Rarity oldRarity, int oldDef)
        {
            var newRarity = oldRarity - 1;
            var newMax = newRarity.GetDefenceMax();
            var newMin = newRarity.GetDefenceMin();
            var range = newMax - newMin;

            var oldMax = oldRarity.GetDefenceMax();
            var oldMin = oldRarity.GetDefenceMin();

            var relNew = ScaleNumber(oldMin, oldMax, newMin, newMax, oldDef);
            var relMin = relNew - (range * 6 / 100);
            var relMax = relNew + (range * 8 / 100);

            var nDef = Fun.GetRandomValue(relMin, relMax + 1);
            if (nDef > newMax) nDef = newMax;
            if (nDef < newMin) nDef = newMin;

            return nDef;
        }

        public static Embed GetActiveList(List<Card> list)
        {
            var embed = new EmbedBuilder()
            {
                Color = EMType.Info.Color(),
                Description = "**Twoje aktywne karty to**:\n\n",
            };

            foreach(var card in list) 
                embed.Description += card.GetString(false, false, true) + "\n";
            
            return embed.Build();
        }

        public static async Task<ICharacterInfo> GetRandomCharacterAsync(ShindenClient shinden)
        {
            int check = 2;
            if (CharId.IsNeedForUpdate())
            {
                var characters = await shinden.Ex.GetAllCharactersFromAnimeAsync();
                if (!characters.IsSuccessStatusCode()) return null;

                CharId.Update(characters.Body);
            }

            ulong id = Fun.GetOneRandomFrom(CharId.Ids);
            var response = await shinden.GetCharacterInfoAsync(id);

            while (!response.IsSuccessStatusCode())
            {
                id = Fun.GetOneRandomFrom(CharId.Ids);
                response = await shinden.GetCharacterInfoAsync(id);

                if (check-- == 0) 
                    return null;
            }
            return response.Body;
        }

        public async Task<string> GetWaifuProfileImageAsync(Card card, ICharacterInfo character, ITextChannel trashCh)
        {
            using (var cardImage = await _img.GetWaifuCardNoStatsAsync(character, card))
            {
                cardImage.SaveToPath($"./GOut/Profile/P{card.Id}.png");

                using (var stream = cardImage.ToPngStream())
                {
                    var fs = await trashCh.SendFileAsync(stream, $"P{card.Id}.png");
                    var im = fs.Attachments.FirstOrDefault();
                    return im.Url;
                }
            }
        }
    }
}