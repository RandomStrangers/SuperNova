/*
	Copyright 2010 MCLawl Team - Written by Valek (Modified for use with MCForge)

	Dual-licensed under the	Educational Community License, Version 2.0 and
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
using System.IO;
using SuperNova.Network;

namespace SuperNova.Commands
{
    public class CmdCCHeartbeat : Command
    {
        public override string name { get { return "ccheartbeat"; } }
        public override string shortcut { get { return "ccbeat"; } }
        public override string type { get { return "moderation"; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }

        public override void Use(Player p, string message)
        {
            try
            {
                Heartbeat.Heartbeats[0].Pump();
                p.Message("Heartbeat pump sent.");
                p.Message("Server URL: " + ((ClassiCubeBeat)Heartbeat.Heartbeats[0]).LastResponse);

            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Error with ClassiCube pump.", e);
            }
        }
        public override void Help(Player p)
        {
            p.Message("/ccheartbeat - Forces a pump for the ClassiCube heartbeat.  DEBUG PURPOSES ONLY.");
        }
    }
    public sealed class CmdUrl : Command2
    {
        public override string name { get { return "ServerUrl"; } }
        public override string shortcut { get { return "url"; } }
        public override string type { get { return "information"; } }
        public override bool SuperUseable { get { return true; } }

        public override void Use(Player p, string message, CommandData data)
        {
#if DEV_BUILD_NOVA
            if (p.IsNova)
#else
                if (p.IsConsole)
#endif
            {
                p.Message("Seriously? Just go look at it!");
                p.cancelcommand = true;
            }
            else
            {
                string file = "./text/externalurl.txt";
                string contents = File.ReadAllText(file);
                p.Message("Server URL: " + contents);
                return;
            }
        }
        public override void Help(Player p)
        {
            p.Message("%T/ServerUrl %H- Shows the server's ClassiCube URL.");
        }
    }
}