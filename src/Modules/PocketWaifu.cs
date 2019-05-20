#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services;
using Sanakan.Services.Commands;
using Sanakan.Services.PocketWaifu;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;
using Sden = Shinden;

namespace Sanakan.Modules
{
    [Name("PocketWaifu"), RequireUserRole]
    public class PocketWaifu : SanakanModuleBase<SocketCommandContext>
    {
        private Database.GuildConfigContext _dbGuildConfigContext;
        private Database.UserContext _dbUserContext;
        private Sden.ShindenClient _shclient;
        private SessionManager _session;
        private Waifu _waifu;

        public PocketWaifu(Waifu waifu, Sden.ShindenClient client, Database.UserContext userContext, 
            Database.GuildConfigContext dbGuildConfigContext, SessionManager session)
        {
            _waifu = waifu;
            _shclient = client;
            _session = session;
            _dbUserContext = userContext;
            _dbGuildConfigContext = dbGuildConfigContext;
        }
        
        [Command("karcianka", RunMode = RunMode.Async)]
        [Alias("cpf")]
        [Summary("wyświetla profil PocketWaifu")]
        [Remarks("Karna"), RequireWaifuCommandChannel]
        public async Task ShowProfileAsync([Summary("użytkownik")]SocketGuildUser usr = null)
        {
            var user = (usr ?? Context.User) as SocketGuildUser;
            if (user == null) return;

            var bUser = await _dbUserContext.GetCachedFullUserAsync(user.Id);
            if (bUser == null)
            {
                await ReplyAsync("", embed: $"{user.Mention} nie ma konta w bocie!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var ssCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.SS);
            var sCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.S);
            var aCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.A);
            var bCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.B);
            var cCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.C);
            var dCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.D);
            var eCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.E);

            var a1vs1 = bUser.GameDeck?.PvPStats?.Count(x => x.Type == FightType.vs1);
            var a1vs1ac = bUser.GameDeck?.PvPStats?.Count(x => x.Type == FightType.vs3);
            
            var w1vs1 = bUser.GameDeck?.PvPStats?.Count(x => x.Result == FightResult.Win && x.Type == FightType.vs1);
            var d1vs1 = bUser.GameDeck?.PvPStats?.Count(x => x.Result == FightResult.Draw && x.Type == FightType.vs1);
            var w1vs1ac = bUser.GameDeck?.PvPStats?.Count(x => x.Result == FightResult.Win && x.Type == FightType.vs3);

            var embed = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder().WithUser(user),
                Description = $"**Posiadane karty**: {bUser.GameDeck.Cards.Count}\n"
                            + $"**SS**: {ssCnt} **S**: {sCnt} **A**: {aCnt} **B**: {bCnt} **C**: {cCnt} **D**: {dCnt} **E**:{eCnt}\n\n"
                            + $"**1vs1** Rozegrane: {a1vs1} Wygrane: {w1vs1} Remis: {d1vs1}\n"
                            + $"**1vs1 AC** Rozegrane: {a1vs1ac} Wygrane: {w1vs1ac}\n"
                            + $"**GMwK** Rozegrane: 0 Wygrane: 0"
            };

            if (bUser.GameDeck?.Waifu != 0)
            {
                var tChar = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == bUser.GameDeck.Waifu);
                if (tChar != null)
                {
                    var response = await _shclient.GetCharacterInfoAsync(tChar.Id);
                    if (response.IsSuccessStatusCode())
                    {
                        var config = await _dbGuildConfigContext.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                        var channel = Context.Guild.GetTextChannel(config.WaifuConfig.TrashCommandsChannel);

                        embed.WithImageUrl(await _waifu.GetWaifuProfileImageAsync(tChar, response.Body, channel));
                        string wfi = (response.Body.Gender == Sden.Models.Sex.Male) ? "Husbando" : "Waifu";
                        embed.WithFooter(new EmbedFooterBuilder().WithText($"{wfi}: {response.Body}"));
                    }
                }
            }

            await ReplyAsync("", embed: embed.Build());
        }
    }
}