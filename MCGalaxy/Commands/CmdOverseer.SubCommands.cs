﻿/*
    Copyright 2011 MCForge
        
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
using MCGalaxy.Commands.CPE;
using MCGalaxy.Commands.World;

namespace MCGalaxy.Commands {
    public sealed partial class CmdOverseer : Command {
        
        static void HandleEnv(Player p, string type, string value) {
            string arg = value == "" ? "normal" : value;
            if (CmdEnvironment.Handle(p, type.ToLower(), arg)) return;
            Player.MessageLines(p, envHelp);
        }

        static void HandleGoto(Player p, string map, string ignored) {
            byte mapNum = 0;
            if (map == "" || map == "1") {
                map = FirstMapName(p);
            } else {
                if (!byte.TryParse(map, out mapNum)) {
                    Player.MessageLines(p, gotoHelp);
                    return;
                }
                map = p.name.ToLower() + map;
            }
            
            Level[] loaded = LevelInfo.Loaded.Items;
            if (LevelInfo.FindExact(map) == null)
                CmdLoad.LoadLevel(p, map, "0", Server.AutoLoad);
            if (LevelInfo.FindExact(map) != null)
                PlayerActions.ChangeMap(p, map);
        }
        
        static void HandleKick(Player p, string name, string ignored) {
            if (name == "") { p.SendMessage("You must specify a player to kick."); return; }
            Player pl = PlayerInfo.FindMatches(p, name);
            if (pl == null) return;
            
            if (pl.level.name == p.level.name) {
                PlayerActions.ChangeMap(pl, Server.mainLevel);
            } else {
                p.SendMessage("Player is not on your level!");
            }
        }
        
        static void HandleKickAll(Player p, string ignored1, string ignored2) {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) {
                if (pl.level == p.level && pl.name != p.name)
                    PlayerActions.ChangeMap(pl, Server.mainLevel);
            }
        }
        
        static void HandleLevelBlock(Player p, string arg1, string arg2) {
            string lbArgs = (arg1 + " " + arg2).Trim();
            CustomBlockCommand.Execute(p, lbArgs, false, "/os lb");
        }
		
        
        static void HandleMap(Player p, string opt, string value) {
            bool mapOnly = !(opt == "ADD" || opt == "DELETE" || opt == "SAVE");
            if (mapOnly && !OwnsMap(p, p.level)) {
                Player.Message(p, "You may only perform that action on your own map."); return;
            }
            
            if (opt == "ADD") {
                AddMap(p, value);
            } else if (opt == "PHYSICS") {
                if (value == "0" || value == "1" || value == "2" || value == "3" || value == "4" || value == "5") {
                    CmdPhysics.SetPhysics(p.level, int.Parse(value));
                } else {
                    Player.Message(p, "Accepted numbers are: 0, 1, 2, 3, 4 or 5");
                }
            } else if (opt == "DELETE") {
                DeleteMap(p, value);
            } else if (opt == "SAVE") {
                byte mapNum = 0;
                if (value == "") {
                    Player.Message(p, "To save one of your maps type %T/os map save [map number]");
                } else if (value == "1") {
                    Command.all.Find("save").Use(p, FirstMapName(p));
                    Player.Message(p, "Map 1 has been saved.");
                } else if (byte.TryParse(value, out mapNum)) {
                    Command.all.Find("save").Use(p, p.name.ToLower() + value);
                    Player.Message(p, "Map " + value + " has been saved.");
                } else {
                    Player.MessageLines(p, mapHelp);
                }
            } else if (opt == "RESTORE") {
                Command.all.Find("restore").Use(p, value);
            } else if (opt == "PERVISIT") {
                string rank = value == "" ? Server.defaultRank : value;
                Command.all.Find("pervisit").Use(p, rank);
            } else if (opt == "PERBUILD") {
                string rank = value == "" ? Server.defaultRank : value;
                Command.all.Find("perbuild").Use(p, rank);
            } else if (opt == "TEXTURE") {
                if (value == "") {
                    Command.all.Find("texture").Use(p, "level normal");
                } else {
                    Command.all.Find("texture").Use(p, "level " + value);
                }
            } else if (opt == "TEXTUREZIP") {
                if (value == "") {
                    Command.all.Find("texture").Use(p, "levelzip normal");
                } else {
                    Command.all.Find("texture").Use(p, "levelzip " + value);
                }
            } else {
                opt = LevelOptions.Map(opt.ToLower());
                if (opt == "physicspeed" || opt == "overload" || opt == "realmowner") {
                    Player.Message(p, "&cYou cannot change that map option via /os map."); return;
                }
                if (CmdMap.SetMapOption(p, p.level, opt, value)) return;
                
                Player.MessageLines(p, mapHelp);
            }
        }
        
        static void AddMap(Player p, string value) {
            if (p.group.OverseerMaps == 0) {
                Player.Message(p, "Your rank is not allowed to create any /os maps."); return;
            }
            string level = NextLevel(p);
            if (level == null) return;

            if (value == "") value = "128 64 128 flat";
            else if (value.IndexOf(' ') == -1) value = "128 64 128 " + value;
            
            string[] args = value.TrimEnd().Split(' ');
            if (args.Length == 3) value += " flat";

            CmdNewLvl newLvl = (CmdNewLvl)Command.all.Find("newlvl"); // TODO: this is a nasty hack, find a better way
            if (!newLvl.GenerateMap(p, level + " " + value)) return;
            
            // Set default perbuild permissions
            CmdLoad.LoadLevel(null, level);
            Level lvl = LevelInfo.FindExact(level);
            if (lvl == null) return;
            
            lvl.RealmOwner = p.name;
            Command.all.Find("perbuild").Use(null, lvl.name + " +" + p.name);
            CmdZone.ZoneAll(lvl, p.name);
            
            LevelPermission osPerm = Server.osPerbuildDefault;
            if (osPerm == LevelPermission.Nobody)
                osPerm = GrpCommands.MinPerm(Command.all.Find("overseer"));
            Group grp = Group.findPerm(osPerm);
            if (grp == null) return;
            
            Command.all.Find("perbuild").Use(null, lvl.name + " " + grp.name);
            Player.Message(p, "Use %T/os zone add [name] %Sto allow " +
                           "players ranked below " + grp.ColoredName + " %Sto build in the map.");
        }
        
        static void DeleteMap(Player p, string value) {
            byte mapNum = 0;
            if (value == "") {
                Player.Message(p, "To delete one of your maps, type %T/os map delete [map number]");
            } else if (value == "1") {
                string map = FirstMapName(p);
                if (!LevelInfo.ExistsOffline(map)) {
                    Player.Message(p, "You don't have a map with that map number."); return;
                }
                
                Player.Message(p, "Created backup.");
                LevelActions.Delete(map);
                Player.Message(p, "Map 1 has been removed.");
            } else if (byte.TryParse(value, out mapNum)) {
                string map = p.name.ToLower() + value;
                if (!LevelInfo.ExistsOffline(map)) {
                    Player.Message(p, "You don't have a map with that map number."); return;
                }
                
                Player.Message(p, "Created backup.");
                LevelActions.Delete(map);
                Player.Message(p, "Map " + value + " has been removed.");
            } else {
                Player.MessageLines(p, mapHelp);
            }
        }


        static void HandlePreset(Player p, string preset, string ignored) {
            Command.all.Find("env").Use(p, "preset " + preset);
        }

        static void HandleSpawn(Player p, string ignored1, string ignored2) {
            Command.all.Find("setspawn").Use(p, "");
        }
		
        
        static void HandleZone(Player p, string cmd, string name) {
            if (cmd == "LIST") {
                Command.all.Find("zone").Use(p, "");
            } else if (cmd == "ADD") {
                if (name == "") { Player.Message(p, "You need to provide a player name."); return; }
                AddBuildPlayer(p, name);
            } else if (cmd == "DEL") {
                if (name == "") { Player.Message(p, "You need to provide a player name, or \"ALL\"."); return; }
                DeleteBuildPlayer(p, name);
            } else if (cmd == "BLOCK") {
                if (name == "") { Player.Message(p, "You need to provide a player name."); return; }
                name = PlayerInfo.FindMatchesPreferOnline(p, name);
                if (name == null) return;
                
                if (name.CaselessEq(p.name)) { Player.Message(p, "You can't blacklist yourself"); return; }
                RemoveVisitPlayer(p, name);
            } else if (cmd == "UNBLOCK") {
                if (name == "") { Player.Message(p, "You need to provide a player name."); return; }
                if (!Formatter.ValidName(p, name, "player")) return;
                AddVisitPlayer(p, name);
            } else if (cmd == "BLACKLIST") {
                List<string> blacklist = p.level.VisitAccess.Blacklisted;
                if (blacklist.Count > 0) {
                    Player.Message(p, "Blacklisted players: " + blacklist.Join());
                } else {
                    Player.Message(p, "There are no blacklisted players on this map.");
                }
            } else {
                Player.MessageLines(p, zoneHelp);
            }
        }
        
        static void AddBuildPlayer(Player p, string name) {
            string[] zoneArgs = name.Split(' ');
            name = zoneArgs[0];
            string reason = zoneArgs.Length > 1 ? zoneArgs[1] : "";
            name = CmdZone.FindZoneOwner(p, "os zone add", name, ref reason);
            if (name == null) return;
            
            CmdZone.ZoneAll(p.level, name);
            Player.Message(p, "Added zone for &b" + name);

            LevelAccessController access = p.level.BuildAccess;
            if (access.Blacklisted.CaselessRemove(name)) {
                access.OnListChanged(p, name, true, true);
            }
            if (!access.Whitelisted.CaselessContains(name)) {
                access.Whitelisted.Add(name);
                access.OnListChanged(p, name, true, false);
            }
        }
        
        static void DeleteBuildPlayer(Player p, string name) {
            if (name.CaselessEq("all")) {
                CmdZone.DeleteAll(p);
            } else if (Formatter.ValidName(p, name, "player")) {
                CmdZone.DeleteWhere(p, zone => zone.Owner.CaselessEq(name));
                LevelAccessController access = p.level.BuildAccess;
                if (access.Whitelisted.CaselessRemove(name)) {
                    access.OnListChanged(p, name, false, true);
                }
            }
        }
        
        static void AddVisitPlayer(Player p, string name) {
            List<string> blacklist = p.level.VisitAccess.Blacklisted;
            if (!blacklist.CaselessContains(name)) {
                Player.Message(p, name + " is not blacklisted."); return;
            }
            blacklist.CaselessRemove(name);
            p.level.VisitAccess.OnListChanged(p, name, true, true);
        }
        
        static void RemoveVisitPlayer(Player p, string name) {
            List<string> blacklist = p.level.VisitAccess.Blacklisted;
            if (blacklist.CaselessContains(name)) {
                Player.Message(p, name + " is already blacklisted."); return;
            }
            blacklist.Add(name);
            p.level.VisitAccess.OnListChanged(p, name, false, false);
        }
    }
}