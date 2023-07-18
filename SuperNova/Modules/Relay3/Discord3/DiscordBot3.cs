﻿/*
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using SuperNova.Config;
using SuperNova.Events.GroupEvents;
using SuperNova.Events.PlayerEvents;
using SuperNova.Events.ServerEvents;
using SuperNova.Util;

namespace SuperNova.Modules.Relay3.Discord3 
{
    public sealed class DiscordBot3 : RelayBot3 
    {
        DiscordApiClient3 api;
        DiscordWebsocket3 socket;
        DiscordSession3 session;
        string botUserID;
        
        Dictionary<string, byte> channelTypes = new Dictionary<string, byte>();
        const byte CHANNEL_DIRECT = 0;
        const byte CHANNEL_TEXT   = 1;

        List<string> filter_triggers = new List<string>();
        List<string> filter_replacements = new List<string>();
        JsonArray allowed;

        public override string RelayName { get { return "Discord3"; } }
        public override bool Enabled     { get { return Config.Enabled; } }
        public override string UserID    { get { return botUserID; } }
        public Discord3Config Config;
        
        TextFile replacementsFile = new TextFile("text/discord/replacements3.txt",
                                        "// This file is used to replace words/phrases sent to discord3",
                                        "// Lines starting with // are ignored",
                                        "// Lines should be formatted like this:",
                                        "// example:http://example.org",
                                        "// That would replace 'example' in messages sent with 'http://example.org'");


        public override bool CanReconnect {
            get { return canReconnect && (socket == null || socket.CanReconnect); }
        }

        public override void DoConnect() {
            socket = new DiscordWebsocket3();
            socket.Session   = session;
            socket.Token     = Config.BotToken;
            socket.Presence  = Config.PresenceEnabled;
            socket.Status    = Config.Status;
            socket.Activity  = Config.Activity;
            socket.GetStatus = GetStatusMessage;
            
            socket.OnReady         = HandleReadyEvent;
            socket.OnResumed       = HandleResumedEvent;
            socket.OnMessageCreate = HandleMessageEvent;
            socket.OnChannelCreate = HandleChannelEvent;
            socket.Connect();
        }
                
        // mono wraps exceptions from reading in an AggregateException, e.g:
        //   * AggregateException - One or more errors occurred.
        //      * ObjectDisposedException - Cannot access a disposed object.
        // .NET sometimes wraps exceptions from reading in an IOException, e.g.:
        //   * IOException - The read operation failed, see inner exception.
        //      * ObjectDisposedException - Cannot access a disposed object.
        static Exception UnpackError(Exception ex) {
            if (ex.InnerException is ObjectDisposedException)
                return ex.InnerException;
            if (ex.InnerException is IOException)
                return ex.InnerException;
            
            // TODO can we ever get an IOException wrapping an IOException?
            return null;
        }

        public override void DoReadLoop() {
            try {
                socket.ReadLoop();
            } catch (Exception ex) {
                Exception unpacked = UnpackError(ex);
                // throw a more specific exception if possible
                if (unpacked != null) throw unpacked;
                
                // rethrow original exception otherwise
                throw;
            }
        }

        public override void DoDisconnect(string reason) {
            try {
                socket.Disconnect();
            } catch {
                // no point logging disconnect failures
            }
        }


        public override void ReloadConfig() {
            Config.Load();
            base.ReloadConfig();
            LoadReplacements();
        }

        public override void UpdateConfig() {
            Channels     = Config.Channels.SplitComma();
            OpChannels   = Config.OpChannels.SplitComma();
            IgnoredUsers = Config.IgnoredUsers.SplitComma();
            
            UpdateAllowed();
            LoadBannedCommands();
        }
        
        void UpdateAllowed() {
            JsonArray mentions = new JsonArray();
            if (Config.CanMentionUsers) mentions.Add("users");
            if (Config.CanMentionRoles) mentions.Add("roles");
            if (Config.CanMentionHere)  mentions.Add("everyone");
            allowed = mentions;
        }
        
        void LoadReplacements() {
            replacementsFile.EnsureExists();            
            string[] lines = replacementsFile.GetText();
            
            filter_triggers.Clear();
            filter_replacements.Clear();
            
            ChatTokens.LoadTokens(lines, (phrase, replacement) => 
                                  {
                                      filter_triggers.Add(phrase);
                                      filter_replacements.Add(MarkdownToSpecial(replacement));
                                  });
        }
        
        public override void LoadControllers() {
            Controllers = PlayerList.Load("text/discord/controllers3.txt");
        }
        
        
        string GetNick(JsonObject data) {
            if (!Config.UseNicks) return null;
            object raw;
            if (!data.TryGetValue("member", out raw)) return null;
            
            // Make sure this is really a member object first
            JsonObject member = raw as JsonObject;
            if (member == null) return null;
            
            member.TryGetValue("nick", out raw);
            return raw as string;
        }
        
        RelayUser3 ExtractUser(JsonObject data) {
            JsonObject author = (JsonObject)data["author"];
            string channel    = (string)data["channel_id"];
            string message    = (string)data["content"];
            
            RelayUser3 user = new RelayUser3();
            user.Nick = GetNick(data) ?? (string)author["username"];
            user.ID   =                  (string)author["id"];
            return user;
        }

        
        void HandleReadyEvent(JsonObject data) {
            JsonObject user = (JsonObject)data["user"];
            botUserID       = (string)user["id"];
            HandleResumedEvent(data);
        }
        
        void HandleResumedEvent(JsonObject data) {
            // May not be null when reconnecting
            if (api == null) {
                api = new DiscordApiClient3();
                api.Token = Config.BotToken;
                api.RunAsync();
            }
            OnReady();
        }
        
        void PrintAttachments(JsonObject data, string channel) {
            object raw;
            if (!data.TryGetValue("attachments", out raw)) return;
            
            JsonArray list = raw as JsonArray;
            if (list == null) return;
            RelayUser3 user = ExtractUser(data);
            
            foreach (object entry in list) {
                JsonObject attachment = entry as JsonObject;
                if (attachment == null) continue;
                
                string url = (string)attachment["url"];
                HandleChannelMessage(user, channel, url);
            }
        }
        
        void HandleMessageEvent(JsonObject data) {
            RelayUser3 user = ExtractUser(data);
            // ignore messages from self
            if (user.ID == botUserID) return;
            
            string channel = (string)data["channel_id"];
            string message = (string)data["content"];
            byte type;

            // Working out whether a channel is a direct message channel
            //  or not without querying the Discord API is a bit of a pain
            // In v6 api, a CHANNEL_CREATE event was always emitted for
            //  direct message channels - hence the relatively simple
            //  solution was to treat every other channels as text channels
            // However, in v8 api changelog the following entry is noted:
            //  "Bots no longer receive Channel Create Gateway Event for DMs"
            // Therefore the code is now forced to instead calculate which
            //  channels are probably text channels, and which aren't        
            if (!channelTypes.TryGetValue(channel, out type))
            {
                type = GuessChannelType(data);
                // channel is definitely a text/normal channel
                if (type == CHANNEL_TEXT) channelTypes[channel] = type;
            }
            
            if (type == CHANNEL_DIRECT) {
                HandleDirectMessage(user, channel, message);
            } else {
                HandleChannelMessage(user, channel, message);
                PrintAttachments(data, channel);
            }
        }
        
        void HandleChannelEvent(JsonObject data) {
            string channel = (string)data["id"];
            string type    = (string)data["type"];

            // 1 = direct/private message channel type
            if (type == "1") channelTypes[channel] = CHANNEL_DIRECT;
        }

        byte GuessChannelType(JsonObject data) {
            // As per discord's documentation:
            //  "The member object exists in MESSAGE_CREATE and MESSAGE_UPDATE
            //   events from text-based guild channels, provided that the
            //   author of the message is not a webhook"
            if (data.ContainsKey("member")) return CHANNEL_TEXT;

            // As per discord's documentation
            //  "You can tell if a message is generated by a webhook by
            //   checking for the webhook_id on the message object."
            if (data.ContainsKey("webhook_id")) return CHANNEL_TEXT;

            // TODO are there any other cases to consider?
            return CHANNEL_DIRECT; // unknown
        }


        static bool IsEscaped(char c) {
            // To match Discord: \a --> \a, \* --> *
            return (c >  ' ' && c <= '/') || (c >= ':' && c <= '@') 
                || (c >= '[' && c <= '`') || (c >= '{' && c <= '~');
        }
        public override string ParseMessage(string input) {
            StringBuilder sb = new StringBuilder(input);
            SimplifyCharacters(sb);
            
            // remove variant selector character used with some emotes
            sb.Replace("\uFE0F", "");
            
            // unescape \ escaped characters
            //  -1 in case message ends with a \
            int length = sb.Length - 1;
            for (int i = 0; i < length; i++) 
            {
                if (sb[i] != '\\') continue;
                if (!IsEscaped(sb[i + 1])) continue;
                
                sb.Remove(i, 1); length--;
            }
            return sb.ToString();
        }
        
        string GetStatusMessage() {
            string online = PlayerInfo.NonHiddenCount().ToString();
            return Config.StatusMessage.Replace("{PLAYERS}", online);
        }
        
        void UpdateDiscordStatus() {
            try { socket.UpdateStatus(); } catch { }
        }


        public override void OnStart() {
            session = new DiscordSession3();
            base.OnStart();
            
            OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.Low);
            OnPlayerDisconnectEvent.Register(HandlePlayerDisconnect, Priority.Low);
            OnPlayerActionEvent.Register(HandlePlayerAction, Priority.Low);
        }

        public override void OnStop() {
            socket = null;
            if (api != null) {
                api.StopAsync();
                api = null;
            }
            base.OnStop();
            
            OnPlayerConnectEvent.Unregister(HandlePlayerConnect);
            OnPlayerDisconnectEvent.Unregister(HandlePlayerDisconnect);
            OnPlayerActionEvent.Unregister(HandlePlayerAction);
        }
        
        void HandlePlayerConnect(Player p) { UpdateDiscordStatus(); }
        void HandlePlayerDisconnect(Player p, string reason) { UpdateDiscordStatus(); }
        
        void HandlePlayerAction(Player p, PlayerAction action, string message, bool stealth) {
            if (action != PlayerAction.Hide && action != PlayerAction.Unhide) return;
            UpdateDiscordStatus();
        }
        
        
        /// <summary> Asynchronously sends a message to the discord API </summary>
        public void Send(DiscordApiMessage3 msg) {
            // can be null in gap between initial connection and ready event received
            if (api != null) api.SendAsync(msg);
        }

        public override void DoSendMessage(string channel, string message) {
            ChannelSendMessage msg = new ChannelSendMessage(channel, message);
            msg.Allowed = allowed;
            Send(msg);
        }

        public override string ConvertMessage(string message) {
            message = base.ConvertMessage(message);
            message = Colors.StripUsed(message);
            message = EscapeMarkdown(message);
            message = SpecialToMarkdown(message);
            return message;
        }
        
        static readonly string[] markdown_special = {  @"\",  @"*",  @"_",  @"~",  @"`",  @"|" };
        static readonly string[] markdown_escaped = { @"\\", @"\*", @"\_", @"\~", @"\`", @"\|" };
        static string EscapeMarkdown(string message) {
            // don't let user use bold/italic etc markdown
            for (int i = 0; i < markdown_special.Length; i++) 
            {
                message = message.Replace(markdown_special[i], markdown_escaped[i]);
            }
            return message;
        }

        public override string PrepareMessage(string message) {
            // allow uses to do things like replacing '+' with ':green_square:'
            for (int i = 0; i < filter_triggers.Count; i++) 
            {
                message = message.Replace(filter_triggers[i], filter_replacements[i]);
            }
            return message;
        }


        // all users are already verified by Discord
        public override bool CheckController(string userID, ref string error) { return true; }

        public override string UnescapeFull(Player p) {
            return BOLD + base.UnescapeFull(p) + BOLD;
        }
        public override string UnescapeNick(Player p) {
            return BOLD + base.UnescapeNick(p) + BOLD;
        }
        
        
        static string FormatRank(OnlineListEntry e) {
            return string.Format(UNDERLINE + "{0}" + UNDERLINE + " (" + CODE + "{1}" + CODE + ")",
                                 e.group.GetFormattedName(), e.players.Count);
        }

        static string FormatNick(Player p, Player pl) {
            string flags  = OnlineListEntry.GetFlags(pl);
            string format;
            
            if (flags.Length > 0) {
                format = BOLD + "{0}" + BOLD + ITALIC + "{2}" + ITALIC + " (" + CODE + "{1}" + CODE + ")";
            } else {
                format = BOLD + "{0}" + BOLD                           + " (" + CODE + "{1}" + CODE + ")";
            }
            return string.Format(format, p.FormatNick(pl), 
                                 // level name must not have _ escaped as the level name is in a code block -
                                 //  otherwise the escaped "\_" actually shows as "\_" instead of "_" 
                                 pl.level.name.Replace('_', UNDERSCORE),
                                 flags);
        }
        
        static string FormatPlayers(Player p, OnlineListEntry e) {
            return e.players.Join(pl => FormatNick(p, pl), ", ");
        }

        public override void MessagePlayers(RelayPlayer3 p) {
            ChannelSendEmbed embed = new ChannelSendEmbed(p.ChannelID);
            int total;
            List<OnlineListEntry> entries = PlayerInfo.GetOnlineList(p, p.Rank, out total);
            
            embed.Color = Config.EmbedColor;
            embed.Title = string.Format("{0} player{1} currently online",
                                        total, total.Plural());
            
            foreach (OnlineListEntry e in entries) 
            {
                if (e.players.Count == 0) continue;
                
                embed.Fields.Add(
                    ConvertMessage(FormatRank(e)),
                    ConvertMessage(FormatPlayers(p, e))
                );
            }
            Send(embed);
        }
        
        
        // these characters are chosen specifically to lie within the unspecified unicode range,
        //  as those characters are "application defined" (EDCX = Escaped Discord Character #X)
        //  https://en.wikipedia.org/wiki/Private_Use_Areas
        const char UNDERSCORE = '\uEDC1'; // _
        const char TILDE      = '\uEDC2'; // ~
        const char STAR       = '\uEDC3'; // *
        const char GRAVE      = '\uEDC4'; // `
        const char BAR        = '\uEDC5'; // |
        
        public const string UNDERLINE     = "\uEDC1\uEDC1"; // __
        public const string BOLD          = "\uEDC3\uEDC3"; // **
        public const string ITALIC        = "\uEDC1"; // _
        public const string CODE          = "\uEDC4"; // `
        public const string SPOILER       = "\uEDC5\uEDC5"; // ||
        public const string STRIKETHROUGH = "\uEDC2\uEDC2"; // ~~
        
        static string MarkdownToSpecial(string input) {
            return input
                .Replace('_', UNDERSCORE)
                .Replace('~', TILDE)
                .Replace('*', STAR)
                .Replace('`', GRAVE)
                .Replace('|', BAR);
        }
        
        static string SpecialToMarkdown(string input) {
            return input
                .Replace(UNDERSCORE, '_')
                .Replace(TILDE,      '~')
                .Replace(STAR,       '*')
                .Replace(GRAVE,      '`')
                .Replace(BAR,        '|');
        }
    }
}
