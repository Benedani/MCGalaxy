﻿/*
    Copyright 2015 MCGalaxy
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.osedu.org/licenses/ECL-2.0
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;

namespace MCGalaxy.Games {
    internal static class MovementCheck {
        
        public static bool InRange(Player a, Player b, int dist) {
            int dx = Math.Abs(a.pos[0] - b.pos[0]);
            int dy = Math.Abs(a.pos[1] - b.pos[1]);
            int dz = Math.Abs(a.pos[2] - b.pos[2]);
            return dx <= dist && dy <= dist && dz <= dist;
        }
        
        public static bool DetectNoclip(Player p, ushort x, ushort y, ushort z) {
            if (p.Game.Referee || Hacks.CanUseHacks(p, p.level)) return false;
            if (!p.CheckIfInsideBlock() || p.Game.NoclipLog.AddSpamEntry(5, 1))
                return false;
            
            Warn(ref p.Game.LastNoclipWarn, p, "noclip");
            return false;
        }
        
        public static bool DetectSpeedhack(Player p, ushort x, ushort y, ushort z, int maxMove) {
            if (p.Game.Referee || Hacks.CanUseHacks(p, p.level)) return false;
            int dx = Math.Abs(p.pos[0] - x), dz = Math.Abs(p.pos[2] - z);
            bool speedhacking = dx >= maxMove || dz >= maxMove;
            if (!speedhacking || p.Game.SpeedhackLog.AddSpamEntry(5, 1))
                return false;
            
            Warn(ref p.Game.LastSpeedhackWarn, p, "speedhack");
            p.SendPos(Entities.SelfID, p.pos[0], p.pos[1], p.pos[2], p.rot[0], p.rot[1]);
            return true;
        }
        
        static void Warn(ref DateTime last, Player p, string action) {
            DateTime now = DateTime.UtcNow;
            if (now >= last) {
                Player.Message(p, "%4Do not {0} &c- ops have been warned.", action);
                Chat.MessageOps(p.ColoredName + " &4appears to be " + action + "ing");
                Server.s.Log(p.name + " appears to be " + action + "ing");
                last = now.AddSeconds(5);
            }
        }
    }
}
