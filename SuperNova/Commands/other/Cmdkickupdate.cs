using System;
using System.Threading;
namespace SuperNova.Commands.Misc 
{
    public sealed class Cmdkickupdate : Command2 
    {
        public override string name { get { return "10"; } }
        public override string shortcut { get { return "10"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool SuperUseable { get { return true; } }
        public override bool UseableWhenFrozen { get { return true; } }
        
        public override void Use(Player p, string message, CommandData data) {
            p.Leave("Updating server...");
        }

        public override void Help(Player p) {
            p.Message("");
        }
        }
        }