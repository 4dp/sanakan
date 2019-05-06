#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using Shinden;
using System;
using System.Threading.Tasks;

namespace Sanakan.Modules
{
    [Name("Shinden"), RequireCommandChannel, RequireUserRole]
    public class Shinden : SanakanModuleBase<SocketCommandContext>
    {
        private ShindenClient _shclient;

        public Shinden(ShindenClient client)
        {
            _shclient = client;
        }

        [Command("odcinki", RunMode = RunMode.Async)]
        [Alias("episodes")]
        [Summary("wyświetla nowo dodane epizody")]
        [Remarks("")]
        public async Task ShowNewEpisodesAsync()
        {
            var response = await _shclient.GetNewEpisodesAsync();
            if (response.IsSuccessStatusCode())
            {
                var episodes = response.Body;
                if (episodes?.Count > 0)
                {
                    var msg = await ReplyAsync("", embed: "Lista poszła na PW!".ToEmbedMessage(EMType.Success).Build());

                    try
                    {
                        var dm = await Context.User.GetOrCreateDMChannelAsync();
                        foreach (var ep in episodes)
                        {
                            await dm.SendMessageAsync("", false, ep.ToEmbed());
                            await Task.Delay(500);
                        }
                        await dm.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        await msg.ModifyAsync(x => x.Embed = $"{Context.User.Mention} nie udało się wyłać PW! ({ex.Message})".ToEmbedMessage(EMType.Error).Build());
                    }

                    return;
                }
            }
            
            await ReplyAsync("", embed: "Nie udało się pobrać listy odcinków.".ToEmbedMessage(EMType.Error).Build());
        }
    }
}