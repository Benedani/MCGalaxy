﻿/*
    Copyright 2015 MCGalaxy
    
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
using System.Text;

namespace MCGalaxy {
    public static class Formatter {
        
        public static void PrintRanks(LevelPermission minRank, List<LevelPermission> allowed,
                                      List<LevelPermission> disallowed, StringBuilder builder) {
            builder.Append(Group.GetColoredName(minRank) + "%S+");
            if (allowed != null && allowed.Count > 0) {
                foreach (LevelPermission perm in allowed)
                    builder.Append(", " + Group.GetColoredName(perm));
                builder.Append("%S");
            }
            
            if (disallowed != null && disallowed.Count > 0) {
                builder.Append( " (except ");
                foreach (LevelPermission perm in disallowed)
                    builder.Append(Group.GetColoredName(perm) + ", ");
                builder.Remove(builder.Length - 2, 2);
                builder.Append("%S)");
            }
        }
        
        public static void PrintCommandInfo(Player p, Command cmd) {
            var perms = GrpCommands.allowedCommands.Find(C => C.commandName == cmd.name);
            StringBuilder builder = new StringBuilder();
            builder.Append("Usable by: ");
            if (perms == null) {
                builder.Append(Group.GetColoredName(cmd.defaultRank) + "%S+");
            } else {
                PrintRanks(perms.lowestRank, perms.allow, perms.disallow, builder);
            }
            Player.Message(p, builder.ToString());
            
            PrintAliases(p, cmd);
            CommandPerm[] addPerms = cmd.ExtraPerms;
            if (addPerms == null) return;
            
            Player.Message(p, "%TExtra permissions:");
            for (int i = 0; i < addPerms.Length; i++) {
                var extra = CommandOtherPerms.Find(cmd, i + 1);
                LevelPermission perm = (LevelPermission)extra.Permission;
                Player.Message(p, "{0}) {1}%S{2}", i + 1, Group.GetColoredName(perm), extra.Description);
            }
        }
        
        static void PrintAliases(Player p, Command cmd) {
            StringBuilder dst = new StringBuilder("Shortcuts: ");
            if (!String.IsNullOrEmpty(cmd.shortcut)) {
                dst.Append('/').Append(cmd.shortcut).Append(", ");
            }
            FindAliases(Alias.coreAliases, cmd, dst);
            FindAliases(Alias.aliases, cmd, dst);
            
            if (dst.Length == "Shortcuts: ".Length) return;
            Player.Message(p, dst.ToString(0, dst.Length - 2));
        }
        
        static void FindAliases(List<Alias> aliases, Command cmd, StringBuilder dst) {
            foreach (Alias a in aliases) {
                if (!a.Target.CaselessEq(cmd.name)) continue;
                
                dst.Append('/').Append(a.Trigger);
                string args = a.Prefix == null ? a.Suffix : a.Prefix;
                if (args == null) { dst.Append(", "); continue; }
                
                string name = String.IsNullOrEmpty(cmd.shortcut) ? cmd.name : cmd.shortcut;
                if (name.Length > cmd.name.Length) name = cmd.name;
                dst.Append(" for /").Append(name + " " + args);
                dst.Append(", ");
            }
        }
        
        public static void MessageBlock(Player p, string action, byte block) {
            StringBuilder builder = new StringBuilder("Only ");
            Block.Blocks perms = Block.BlockList[block];
            PrintRanks(perms.lowestRank, perms.allow, perms.disallow, builder);
            
            builder.Append( " %Scan ").Append(action);
            builder.Append(Block.Name(block)).Append(".");
            Player.Message(p, builder.ToString());
        }
        
        public static void MessageNeedMinPerm(Player p, string action, LevelPermission perm) {
            Group grp = Group.findPerm(perm);
            if (grp == null)
                Player.Message(p, "Only {0}%S+ can {1}", (int)perm, action);
            else
                Player.Message(p, "Only {0}%S+ can {1}", grp.ColoredName, action);
        }
        
        public static bool ValidName(Player p, string name, string type) {
            if (Player.ValidName(name)) return true;
            Player.Message(p, "\"{0}\" is not a valid {1} name.", name, type);
            return false;
        }
    }
}
