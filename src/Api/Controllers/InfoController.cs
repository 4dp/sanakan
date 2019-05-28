#pragma warning disable 1591

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Discord.Commands;
using System.Linq;
using System;
using Sanakan.Services;
using Sanakan.Config;
using Sanakan.Extensions;
using Sanakan.Api.Models;

namespace Sanakan.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InfoController : ControllerBase
    {
        private readonly Helper _helper;
        private readonly IConfig _config;

        public InfoController(Helper helper, IConfig config)
        {
            _helper = helper;
            _config = config;
        }

        /// <summary>
        /// Pobiera publiczną liste poleceń bota
        /// </summary>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("commands")]
        public async Task<Commands> GetCommansInfoAsync()
        {
            try
            {
                return new Commands
                {
                    Prefix = _config.Get().Prefix,
                    Modules = GetInfoAboutModules(_helper.PublicModulesInfo)
                };
            }
            catch(Exception ex)
            {
                await ex.Message.ToResponse(500).ExecuteResultAsync(ControllerContext);
                return null;
            }
        }

        private List<Models.Module> GetInfoAboutModules(IEnumerable<ModuleInfo> modules)
        {
            var mod = new List<Models.Module>();
            foreach (var item in modules)
            {
                var mInfo = new Models.Module
                {
                    Name = item.Name,
                    SubModules = new List<SubModule>()
                };

                if (mod.Any(x => x.Name.Equals(item.Name)))
                    mInfo = mod.First(x => x.Name.Equals(item.Name));
                else mod.Add(mInfo);

                var subMInfo = new SubModule()
                {
                    Prefix = item.Aliases.FirstOrDefault(),
                    Commands = new List<Command>(),
                    PrefixAliases = new List<string>()
                };

                foreach(var ali in item.Aliases)
                {
                    if (!ali.Equals(subMInfo.Prefix))
                        subMInfo.PrefixAliases.Add(ali);
                }

                foreach (var cmd in item.Commands)
                {
                    if (!string.IsNullOrEmpty(cmd.Name))
                    {
                        var cc = new Command()
                        {
                            Name = cmd.Name,
                            Example = cmd.Remarks,
                            Description = cmd.Summary,
                            Aliases = new List<string>(),
                            Attributes = new List<Models.CommandAttribute>()
                        };

                        foreach(var atr in cmd.Parameters)
                        {
                            cc.Attributes.Add(new Models.CommandAttribute
                            {
                                Name = atr.Name,
                                Description = atr.Summary
                            });
                        }

                        foreach(var al in cmd.Aliases)
                        {
                            var alss = GetBoreboneCmdAlias(subMInfo.PrefixAliases, subMInfo.Prefix, al);
                            if (!cc.Aliases.Any(x => x.Equals(alss)))
                                cc.Aliases.Add(alss);
                        }
                        subMInfo.Commands.Add(cc);
                    }
                }
                mInfo.SubModules.Add(subMInfo);
            }
            return mod;
        }

        private string GetBoreboneCmdAlias(List<string> moduleAli, string modulePrex, string cmdAlias)
        {
            if (!string.IsNullOrEmpty(modulePrex))
                cmdAlias = cmdAlias.Replace(modulePrex + " ", "");

            foreach(var ali in moduleAli)
                cmdAlias  = cmdAlias.Replace(ali + " ", "");

            return cmdAlias;
        }
    }
}