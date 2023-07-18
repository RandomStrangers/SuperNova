/*
Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/SuperNova)
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
using System.Windows.Forms;
using SuperNova.Modules.Relay3.Discord3;
using SuperNova.Modules.Relay3.IRC3;
using SuperNova.SQL;

namespace SuperNova.Gui {

    public partial class PropertyWindow : Form { 
        
        void LoadRelayProps3() {
            LoadIRCProps3();
            LoadDiscordProps3();
        }
        
        void ApplyRelayProps3() {
            ApplyIRCProps3();
            ApplyDiscordProps3();
        }
        
        
        void LoadIRCProps3() {
            irc_chkEnabled.Checked = Server.Config.UseGlobalIRC;
            irc_txtServer.Text = Server.Config.GlobalIRCServer;
            irc_numPort.Value = Server.Config.GlobalIRCPort;
            irc_txtNick.Text = Server.Config.GlobalIRCNick;
            irc_txtChannel.Text = Server.Config.GlobalIRCChannels;
            irc_txtOpChannel.Text = Server.Config.GlobalIRCOpChannels;
            irc_chkPass.Checked = Server.Config.GlobalIRCIdentify;
            irc_txtPass.Text = Server.Config.GlobalIRCPassword;
            
            irc_cbTitles.Checked = Server.Config.GlobalIRCShowPlayerTitles;
            irc_cbWorldChanges.Checked = Server.Config.GlobalIRCShowWorldChanges;
            irc_cbAFK.Checked = Server.Config.GlobalIRCShowAFK;
            ToggleIrcSettings3(Server.Config.UseGlobalIRC);

            irc_cmbRank.Items.AddRange(GuiPerms.RankNames);
            GuiPerms.SetDefaultIndex(irc_cmbRank, Server.Config.GlobalIRCControllerRank);
            irc_cmbVerify.Items.AddRange(Enum.GetNames(typeof(IRCControllerVerify3)));
            irc_cmbVerify.SelectedIndex = (int)Server.Config.GlobalIRCVerify;
            irc_txtPrefix.Text = Server.Config.GlobalIRCCommandPrefix;
        }
        
        void ApplyIRCProps3() {
            Server.Config.UseGlobalIRC = irc_chkEnabled.Checked;
            Server.Config.GlobalIRCServer = irc_txtServer.Text;
            Server.Config.GlobalIRCPort = (int)irc_numPort.Value;
            Server.Config.GlobalIRCNick = irc_txtNick.Text;
            Server.Config.GlobalIRCChannels = irc_txtChannel.Text;
            Server.Config.GlobalIRCOpChannels = irc_txtOpChannel.Text;
            Server.Config.GlobalIRCIdentify = irc_chkPass.Checked;
            Server.Config.GlobalIRCPassword = irc_txtPass.Text;
            
            Server.Config.GlobalIRCShowPlayerTitles = irc_cbTitles.Checked;
            Server.Config.GlobalIRCShowWorldChanges = irc_cbWorldChanges.Checked;
            Server.Config.GlobalIRCShowAFK = irc_cbAFK.Checked;
            
            Server.Config.GlobalIRCControllerRank = GuiPerms.GetPermission(irc_cmbRank, LevelPermission.Admin);
            Server.Config.GlobalIRCVerify = (IRCControllerVerify3)irc_cmbVerify.SelectedIndex;
            Server.Config.GlobalIRCCommandPrefix = irc_txtPrefix.Text;  
        }        
                
        void ToggleIrcSettings3(bool enabled) {
            irc_txtServer.Enabled = enabled; irc_lblServer.Enabled = enabled;
            irc_numPort.Enabled = enabled; irc_lblPort.Enabled = enabled;
            irc_txtNick.Enabled = enabled; irc_lblNick.Enabled = enabled;
            irc_txtChannel.Enabled = enabled; irc_lblChannel.Enabled = enabled;
            irc_txtOpChannel.Enabled = enabled; irc_lblOpChannel.Enabled = enabled;    
            irc_chkPass.Enabled = enabled; irc_txtPass.Enabled = enabled && irc_chkPass.Checked;
            
            irc_cbTitles.Enabled = enabled;
            irc_cbWorldChanges.Enabled = enabled;
            irc_cbAFK.Enabled = enabled;           
            irc_lblRank.Enabled = enabled; irc_cmbRank.Enabled = enabled;
            irc_lblVerify.Enabled = enabled; irc_cmbVerify.Enabled = enabled;
            irc_lblPrefix.Enabled = enabled; irc_txtPrefix.Enabled = enabled;
        }
        
        void irc_chkEnabled_CheckedChanged3(object sender, EventArgs e) {
            ToggleIrcSettings3(irc_chkEnabled.Checked);
        }        
        
        void irc_chkPass_CheckedChanged3(object sender, EventArgs e) {
            irc_txtPass.Enabled = irc_chkEnabled.Checked && irc_chkPass.Checked;
        }
        
        
        void LoadDiscordProps3() {
            Discord3Config cfg = DiscordPlugin3.Config;
            dis_chkEnabled.Checked = cfg.Enabled;
            dis_txtToken.Text      = cfg.BotToken;
            dis_txtChannel.Text    = cfg.Channels;
            dis_txtOpChannel.Text  = cfg.OpChannels;
            dis_chkNicks.Checked   = cfg.UseNicks;
            ToggleDiscordSettings3(cfg.Enabled);
        }
        
        void ApplyDiscordProps3() {
            Discord3Config cfg = DiscordPlugin3.Config;
            cfg.Enabled    = dis_chkEnabled.Checked;
            cfg.BotToken   = dis_txtToken.Text;
            cfg.Channels   = dis_txtChannel.Text;
            cfg.OpChannels = dis_txtOpChannel.Text;
            cfg.UseNicks   = dis_chkNicks.Checked;
        }
        
        void SaveDiscordProps3() {
            DiscordPlugin3.Config.Save();
        }
                
        void ToggleDiscordSettings3(bool enabled) {
            dis_txtToken.Enabled = enabled; dis_lblToken.Enabled = enabled;
            dis_txtChannel.Enabled = enabled; dis_lblChannel.Enabled = enabled;
            dis_txtOpChannel.Enabled = enabled; dis_lblOpChannel.Enabled = enabled;
            dis_chkNicks.Enabled = enabled;
        }
        
        void dis_chkEnabled_CheckedChanged3(object sender, EventArgs e) {
            ToggleDiscordSettings3(dis_chkEnabled.Checked);
        }

        void dis_lnkHelp_LinkClicked3(object sender, LinkLabelLinkClickedEventArgs e) {
            GuiUtils.OpenBrowser(Updater.WikiURL + "/wiki/Discord-relay-bot/");
        }
    }
}
