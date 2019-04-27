using Discord.Commands;
using Sanakan.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sanakan.Services
{
    public class Helper
    {
        private IConfig _config;

        public IEnumerable<ModuleInfo> PublicModulesInfo { get; set; }
        public Dictionary<string, ModuleInfo> PrivateModulesInfo { get; set; }

        public Helper(IConfig config)
        {
            _config = config;

            PublicModulesInfo = new List<ModuleInfo>();
            PrivateModulesInfo = new Dictionary<string, ModuleInfo>();
        }

        public string GivePublicHelp()
        {
            string commands = "**Lista poleceń:**\n";
            foreach (var item in GetInfoAboutModules(PublicModulesInfo))
            {
                List<string> sSubInfo = new List<string>();
                foreach (var mod in item.Modules)
                {
                    string info = "";
                    if (!string.IsNullOrWhiteSpace(mod.Prefix))
                        info += $"      ***{mod.Prefix}***";

                    sSubInfo.Add(info + " " + string.Join("  ", mod.Commands));
                }

                commands += $"\r\n**{item.Name}:**" + string.Join("\n", sSubInfo);
            }
            commands += $"\r\n\r\nUżyj `{_config.Get().Prefix}pomoc [polecenie]` aby uzyskać informacje dotyczące danego polecenia.";
            //commands += "\r\n\r\nPogrubione wyrazy napisane kursywą to przedrostki i należy ich używać. np. `s.gildia członkowie`";
            return commands;
        }

        public string GivePrivateHelp(string moduleName)
        {
            var item = GetInfoAboutModule(PrivateModulesInfo[moduleName]);
            return $"**Lista poleceń:**\n\n**{item.Prefix}:** " + string.Join("  ", item.Commands);
        }

        public string GiveHelpAboutPrivateCmd(string moduleName, string command)
        {
            var info = PrivateModulesInfo[moduleName];

            var thisCommands = info.Commands.FirstOrDefault(x => x.Name == command);

            if (thisCommands == null)
                thisCommands = info.Commands.FirstOrDefault(x => x.Aliases.Any(c => c == command));

            if (thisCommands != null)
                return GetCommandInfo(thisCommands);

            throw new Exception("Polecenie nie istnieje!");
        }

        public string GetCommandInfo(CommandInfo cmd)
        {
            string modulePrefix = GetModGroupPrefix(cmd.Module);
            string botPrefix = _config.Get().Prefix;

            string command = $"**{botPrefix}{modulePrefix}{cmd.Name}**";

            if (cmd.Parameters.Count > 0)
                foreach (var param in cmd.Parameters) command += $"`{param.Name}`";

            command += $" - {cmd.Summary}\n";

            if (cmd.Parameters.Count > 0)
                foreach (var param in cmd.Parameters) command += $"*{param.Name}* - *{param.Summary}*\n";

            if (cmd.Aliases.Count > 1)
            {
                command += "\n**Aliasy:**\n";
                foreach (var alias in cmd.Aliases)
                    if (alias != cmd.Name) command += $"`{alias}` ";
            }

            command += $"\n\nnp. `{botPrefix}{modulePrefix}{cmd.Name} {cmd.Remarks}`";

            return command;
        }

        public string GiveHelpAboutPublicCmd(string command)
        {
            foreach(var module in PublicModulesInfo)
            {
                var thisCommands = module.Commands.FirstOrDefault(x => x.Name == command);

                if (thisCommands == null)
                    thisCommands = module.Commands.FirstOrDefault(x => x.Aliases.Any(c => c == command));

                if (thisCommands != null)
                    return GetCommandInfo(thisCommands);
            }
            throw new Exception("Polecenie nie istnieje!");
        }

        private List<SanakanModuleInfo> GetInfoAboutModules(IEnumerable<ModuleInfo> modules)
        {
            List<SanakanModuleInfo> mod = new List<SanakanModuleInfo>();
            foreach (var item in modules)
            {
                var mInfo = new SanakanModuleInfo()
                {
                    Name = item.Name,
                    Modules = new List<SanakanSubModuleInfo>()
                };

                if (mod.Any(x => x.Name.Equals(item.Name)))
                    mInfo = mod.First(x => x.Name.Equals(item.Name));
                else mod.Add(mInfo);

                var subMInfo = new SanakanSubModuleInfo()
                {
                    Prefix = GetModGroupPrefix(item, false),
                    Commands = new List<string>()
                };

                foreach (var cmd in item.Commands)
                    if (!string.IsNullOrEmpty(cmd.Name))
                        subMInfo.Commands.Add("`" + cmd.Name + "`");

                mInfo.Modules.Add(subMInfo);
            }
            return mod;
        }

        private SanakanSubModuleInfo GetInfoAboutModule(ModuleInfo module)
        {
            var subMInfo = new SanakanSubModuleInfo()
            {
                Prefix = module.Name,
                Commands = new List<string>()
            };

            foreach (var cmd in module.Commands)
                if (!string.IsNullOrEmpty(cmd.Name))
                    subMInfo.Commands.Add("`" + cmd.Name + "`");

            return subMInfo;
        }

        private string GetModGroupPrefix(ModuleInfo mod, bool space = true)
        {
            string prefix = "";
            var att = mod.Aliases.FirstOrDefault();
            if (!string.IsNullOrEmpty(att))
            {
                if (space) att += " ";
                prefix = att;
            }
            return prefix;
        }

        private struct SanakanModuleInfo
        {
            public string Name { get; set; }
            public List<SanakanSubModuleInfo> Modules { get; set; }
        }

        private struct SanakanSubModuleInfo
        {
            public string Prefix { get; set; }
            public List<string> Commands { get; set; }
        }
    }
}
