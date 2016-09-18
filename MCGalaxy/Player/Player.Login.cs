﻿/*
Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
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
using System.Data;
using System.IO;
using MCGalaxy.Commands.World;
using MCGalaxy.Games;
using MCGalaxy.SQL;

namespace MCGalaxy {
    public sealed partial class Player : IDisposable {
        
        void HandleLogin(byte[] packet) {
            LastAction = DateTime.UtcNow;
            if (loggedIn) return;

            byte version = packet[1];
            name = enc.GetString(packet, 2, 64).Trim();
            if (name.Length > 16) {
                Leave("Usernames must be 16 characters or less", true); return;
            }
            truename = name;
            skinName = name;
            
            int altsCount = 0;
            lock (pendingLock) {
                DateTime now = DateTime.UtcNow;
                foreach (PendingItem item in pendingNames) {
                    if (item.Name == truename && (now - item.Connected).TotalSeconds <= 60)
                        altsCount++;
                }
                pendingNames.Add(new PendingItem(name));
            }
            if (altsCount > 0) {
                Leave("Already logged in!", true); return;
            }

            string verify = enc.GetString(packet, 66, 32).Trim();
            verifiedName = false;
            if (Server.verify) {
                byte[] hash = null;
                lock (md5Lock)
                    hash = md5.ComputeHash(enc.GetBytes(Server.salt + truename));
                
                string hashHex = BitConverter.ToString(hash);
                if (!verify.CaselessEq(hashHex.Replace("-", ""))) {
                    if (!IPInPrivateRange(ip)) {
                        Leave("Login failed! Try signing in again.", true); return;
                    }
                } else {
                    verifiedName = true;
                }
            }
            
            DisplayName = name;
            if (Server.ClassicubeAccountPlus) name += "+";
            isDev = Server.Devs.CaselessContains(truename);
            isMod = Server.Mods.CaselessContains(truename);

            try {
                Server.TempBan tBan = Server.tempBans.Find(tB => tB.name.ToLower() == name.ToLower());
                if (tBan.expiryTime < DateTime.UtcNow) {
                    Server.tempBans.Remove(tBan);
                } else {
                    string reason = String.IsNullOrEmpty(tBan.reason) ? "" :
                        " (" + tBan.reason + ")";
                    Kick("You're still temp banned!" + reason, true);
                }
            } catch { }

            if (!CheckWhitelist()) { Leave("This is a private server!", true); return; }
            Group foundGrp = Group.findPlayerGroup(name);
            
            // ban check
            if (Server.bannedIP.Contains(ip) && (!Server.useWhitelist || !onWhitelist)) {
                Kick(Server.defaultBanMessage, true);  return;
            }
            
            if (foundGrp.Permission == LevelPermission.Banned) {
                string[] data = Ban.GetBanData(name);
                if (data != null) {
                    Kick(Ban.FormatBan(data[0], data[1]), true);
                } else {
                    Kick(Server.defaultBanMessage, true);
                }
                return;
            }

            // maxplayer check
            if (!CheckPlayersCount(foundGrp)) return;
            if (version != Server.version) { Leave("Wrong version!", true); return; }
            
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players) {
                if (p.name != name) continue;
                
                if (Server.verify) {
                    string reason = p.ip == ip ? "(Reconnecting)" : "(Reconnecting from a different IP)";
                    p.Leave(reason); break;
                } else {
                    Leave("Already logged in!", true); return;
                }
            }
            
            byte type = packet[130];
            if (type == 0x42) { hasCpe = true; SendCpeExtensions(); }

            group = foundGrp;
            Loading = true;
            if (disconnected) return;
            id = NextFreeId();
            
            if (type != 0x42)
                CompleteLoginProcess();
        }
        
        bool CheckPlayersCount(Group foundGrp) {
            if (Server.vip.Contains(name)) return true;
            
            Player[] online = PlayerInfo.Online.Items;
            if (online.Length >= Server.players && !IPInPrivateRange(ip)) { Leave("Server full!", true); return false; }
            if (foundGrp.Permission > LevelPermission.Guest) return true;
            
            online = PlayerInfo.Online.Items;
            int guests = 0;
            foreach (Player p in online) {
                if (p.Rank <= LevelPermission.Guest) guests++;
            }
            if (guests < Server.maxGuests) return true;
            
            if (Server.guestLimitNotify) Chat.MessageOps("Guest " + DisplayName + " couldn't log in - too many guests.");
            Server.s.Log("Guest " + name + " couldn't log in - too many guests.");
            Leave("Server has reached max number of guests", true);
            return false;
        }
        
        void SendCpeExtensions() {
            Send(Packet.MakeExtInfo(22), true);
            
            Send(Packet.MakeExtEntry(CpeExt.EnvMapAppearance, 1), true); // fix for classicube client, doesn't reply if only send EnvMapAppearance with version 2
            Send(Packet.MakeExtEntry(CpeExt.ClickDistance, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.CustomBlocks, 1), true);
            
            Send(Packet.MakeExtEntry(CpeExt.HeldBlock, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.TextHotkey, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.EnvColors, 1), true);
            
            Send(Packet.MakeExtEntry(CpeExt.SelectionCuboid, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.BlockPermissions, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.ChangeModel, 1), true);
            
            Send(Packet.MakeExtEntry(CpeExt.EnvMapAppearance, 2), true);
            Send(Packet.MakeExtEntry(CpeExt.EnvWeatherType, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.HackControl, 1), true);
            
            Send(Packet.MakeExtEntry(CpeExt.EmoteFix, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.FullCP437, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.LongerMessages, 1), true);
            
            Send(Packet.MakeExtEntry(CpeExt.BlockDefinitions, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.BlockDefinitionsExt, 2), true);
            Send(Packet.MakeExtEntry(CpeExt.TextColors, 1), true);
            
            Send(Packet.MakeExtEntry(CpeExt.BulkBlockUpdate, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.MessageTypes, 1), true);
            Send(Packet.MakeExtEntry(CpeExt.ExtPlayerList, 2), true);
            
            Send(Packet.MakeExtEntry(CpeExt.EnvMapAspect, 1), true);
        }
        
        bool CheckWhitelist() {
            if (!Server.useWhitelist) return true;
            if (Server.verify) return Server.whiteList.Contains(name);
            
            // Verify names is off, check if the player is on the same IP.
            return Server.whiteList.Contains(name) && PlayerInfo.FindAccounts(ip).Contains(name);
        }
        
        void CompleteLoginProcess() {
            LevelPermission adminChatRank = CommandOtherPerms.FindPerm("adminchat", LevelPermission.Admin);
            
            SendUserMOTD();
            SendMap(null);
            if (disconnected) return;
            loggedIn = true;

            PlayerInfo.Online.Add(this);
            connections.Remove(this);
            RemoveFromPending();
            Server.s.PlayerListUpdate();
            
            //OpenClassic Client Check
            SendBlockchange(0, 0, 0, 0);
            timeLogged = DateTime.Now;
            lastLogin = DateTime.Now;
            time = new TimeSpan(0, 0, 0, 1);
            DataTable playerDb = Database.Backend.GetRows("Players", "*", "WHERE Name=@0", name);
            
            if (playerDb.Rows.Count == 0)
                InitPlayerStats(playerDb);
            else
                LoadPlayerStats(playerDb);
            
            Server.MainScheduler.QueueOnce(ShowAltsTask, name, TimeSpan.Zero);
            CheckState();
            ZombieStats stats = Server.zombie.LoadZombieStats(name);
            Game.MaxInfected = stats.MaxInfected; Game.TotalInfected = stats.TotalInfected;
            Game.MaxRoundsSurvived = stats.MaxRounds; Game.TotalRoundsSurvived = stats.TotalRounds;
            
            if (!Directory.Exists("players"))
                Directory.CreateDirectory("players");
            PlayerDB.Load(this);
            Game.Team = Team.FindTeam(this);
            SetPrefix();
            playerDb.Dispose();
            LoadCpeData();
            
            if (Server.verifyadmins && group.Permission >= Server.verifyadminsrank)
                adminpen = true;
            if (Server.noEmotes.Contains(name))
                parseEmotes = !Server.parseSmiley;

            hidden = group.CanExecute("hide") && Server.hidden.Contains(name);
            if (hidden) SendMessage("&8Reminder: You are still hidden.");
            if (group.Permission >= adminChatRank && Server.adminsjoinsilent) {
                hidden = true; adminchat = true;
            }
            
            if (PlayerConnect != null)
                PlayerConnect(this);
            OnPlayerConnectEvent.Call(this);
            if (cancellogin) { cancellogin = false; return; }
            
            
            string joinm = "&a+ " + FullName + " %S" + PlayerDB.GetLoginMessage(this);
            if (hidden) joinm = "&8(hidden)" + joinm;
            const LevelPermission perm = LevelPermission.Guest;
            if (group.Permission > perm || (Server.guestJoinNotify && group.Permission <= perm)) {
                Player[] players = PlayerInfo.Online.Items;
                foreach (Player pl in players) {
                    if (Entities.CanSee(pl, this)) Player.Message(pl, joinm);
                }
            }

            if (Server.agreetorulesonentry && group.Permission == LevelPermission.Guest && !Server.agreed.Contains(name)) {
                SendMessage("&9You must read the &c/rules&9 and &c/agree&9 to them before you can build and use commands!");
                agreed = false;
            }

            if (Server.verifyadmins && group.Permission >= Server.verifyadminsrank) {
                if (!Directory.Exists("extra/passwords") || !File.Exists("extra/passwords/" + name + ".dat"))
                    SendMessage("&cPlease set your admin verification password with &a/setpass [Password]!");
                else
                    SendMessage("&cPlease complete admin verification with &a/pass [Password]!");
            }

            Server.s.Log(name + " [" + ip + "] has joined the server.");
            Game.InfectMessages = PlayerDB.GetInfectMessages(this);
            Server.zombie.PlayerJoinedServer(this);
            
            ushort x = (ushort)((0.5 + level.spawnx) * 32);
            ushort y = (ushort)((1 + level.spawny) * 32);
            ushort z = (ushort)((0.5 + level.spawnz) * 32);
            pos = new ushort[3] { x, y, z };
            rot = new byte[2] { level.rotx, level.roty };
            
            Entities.SpawnEntities(this, x, y, z, rot[0], rot[1]);
            PlayerActions.CheckGamesJoin(this, null);
            Loading = false;
        }
        
        void LoadCpeData() {
            string line = Server.skins.Find(name);
            if (line != null) {
                int sep = line.IndexOf(' ');
                if (sep >= 0) skinName = line.Substring(sep + 1);
            }
            
            line = Server.models.Find(name);
            if (line != null) {
                int sep = line.IndexOf(' ');
                if (sep >= 0) model = line.Substring(sep + 1);
            }
        }
        
        void InitPlayerStats(DataTable playerDb) {
            SendMessage("Welcome " + DisplayName + "! This is your first visit.");
            PlayerData.Create(this);
        }
        
        void LoadPlayerStats(DataTable playerDb) {
            PlayerData.Load(playerDb, this);
            SendMessage("Welcome back " + color + prefix + DisplayName + "%S! " +
                        "You've been here " + totalLogins + " times!");
        }
        
        void CheckState() {
            if (Server.muted.Contains(name)) {
                muted = true;
                Chat.MessageAll("{0} is still muted from the last time they went offline.", DisplayName);
                Player.Message(this, "!%cYou are still %8muted%c since your last login.");
            }
            if (Server.frozen.Contains(name)) {
                frozen = true;
                Chat.MessageAll("{0} is still frozen from the last time they went offline.", DisplayName);
                Player.Message(this, "!%cYou are still %8frozen%c since your last login.");
            }
        }
        
        static void ShowAltsTask(SchedulerTask task) {
            string name = (string)task.State;
            Player p = PlayerInfo.FindExact(name);
            if (p == null || p.ip == "127.0.0.1") return;
            
            List<string> alts = PlayerInfo.FindAccounts(p.ip);
            // Remove online accounts from the list of accounts on the IP
            for (int i = alts.Count - 1; i >= 0; i--) {
                if (PlayerInfo.FindExact(alts[i]) == null) continue;
                alts.RemoveAt(i);
            }
            if (alts.Count == 0) return;
            
            LevelPermission adminChatRank = CommandOtherPerms.FindPerm("adminchat", LevelPermission.Admin);
            string altsMsg = p.ColoredName + " %Sis lately known as: " + alts.Join();
            if (p.group.Permission < adminChatRank || !Server.adminsjoinsilent) {
                Chat.MessageOps(altsMsg);
                //IRCBot.Say(temp, true); //Tells people in op channel on IRC
            }
            Server.s.Log(altsMsg);
        }
    }
}
