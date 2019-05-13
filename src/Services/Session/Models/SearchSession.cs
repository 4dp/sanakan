#pragma warning disable 1591

using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Sanakan.Extensions;
using Shinden;
using Shinden.Models;

namespace Sanakan.Services.Session.Models
{
    public class SearchSession : Session
    {
        public IMessage[] Messages { get; set; }
        public List<IQuickSearch> SList { get; set; }
        public List<IPersonSearch> PList { get; set; }
        public ShindenClient ShindenClient { get; set; }

        public SearchSession(IUser owner, ShindenClient client) : base(owner)
        {
            Event = ExecuteOn.Message;
            RunMode = RunMode.Async;
            ShindenClient = client;
            TimeoutMs = 120000;

            Messages = null;
            SList = null;
            PList = null;

            OnExecute = ExecuteAction;
            OnDispose = DisposeAction;
        }

        private async Task<bool> ExecuteAction(SessionContext context, Session session)
        {
            var content = context.Message?.Content;
            if (content == null) return false;

            if (content.ToLower() == "koniec") 
                return true;

            if (int.TryParse(content, out int number))
            {
                if (SList != null)
                {
                    if (number > 0 && SList.Count >= number)
                    {
                        var info = (await ShindenClient.Title.GetInfoAsync(SList.ToArray()[number - 1])).Body;
                        await context.Channel.SendMessageAsync("", false, info.ToEmbed());
                        await context.Message.DeleteAsync();
                        return true;
                    }
                }
                if (PList != null)
                {
                    if (number > 0 && PList.Count >= number)
                    {
                        var info = (await ShindenClient.GetCharacterInfoAsync(PList.ToArray()[number - 1])).Body;
                        await context.Channel.SendMessageAsync("", false, info.ToEmbed());
                        await context.Message.DeleteAsync();
                        return true;
                    }
                }
            }
            
            return false;
        }

        private async Task DisposeAction()
        {
            if (Messages != null)
            {
                foreach (var message in Messages)
                {
                    var msg = await message.Channel.GetMessageAsync(message.Id);
                    if (msg != null) await msg.DeleteAsync();
                }

                Messages = null;
            }

            SList = null;
            PList = null;
        }
    }
}