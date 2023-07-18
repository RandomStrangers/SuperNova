/*
    Copyright 2015 SuperNova
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using SuperNova.Commands;
using SuperNova.Events.ServerEvents;

namespace SuperNova.Modules.Relay3.IRC3 
{   
    public sealed class IRCPlugin3 : Plugin 
    {
        public override string creator { get { return Server.SoftwareName + " team"; } }
        public override string SuperNova_Version { get { return Server.Version; } }
        public override string name { get { return "GlobalIRCRelay"; } }

        public static IRCBot3 Bot3 = new IRCBot3();
        
        public override void Load(bool startup) {
            Bot3.ReloadConfig();
            Bot3.Connect();
            OnConfigUpdatedEvent.Register(OnConfigUpdated, Priority.Low);
        }
        
        public override void Unload(bool shutdown) {
            OnConfigUpdatedEvent.Unregister(OnConfigUpdated);
            Bot3.Disconnect("Disconnecting GlobalIRC bot");
        }
        
        void OnConfigUpdated() { Bot3.ReloadConfig(); }
    }
    
    public sealed class CmdIRCBot3 : RelayBotCmd3 
    {
        public override string name { get { return "IRCBot3"; } }
        public override CommandAlias[] Aliases {
            get { return new[] { new CommandAlias("ResetBot3", "reset3"), new CommandAlias("ResetIRC3", "reset3") }; }
        }
        public override RelayBot3 Bot { get { return IRCPlugin3.Bot3; } }
    }
    
    public sealed class CmdIrcControllers3 : BotControllersCmd3 
    {
        public override string name { get { return "IRCControllers3"; } }
        public override string shortcut { get { return "IRCCtrl3"; } }
        public override RelayBot3 Bot { get { return IRCPlugin3.Bot3; } }
    }
}
