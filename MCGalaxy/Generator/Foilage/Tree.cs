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
using MCGalaxy.Drawing.Brushes;

namespace MCGalaxy.Generator.Foilage {

    public delegate void TreeOutput(ushort x, ushort y, ushort z, byte block);
    
    public abstract class Tree {
        protected internal byte height, top, size;
        protected Random rnd;
        
        public abstract void SetData(Random rnd);
        
        public abstract void Output(ushort x, ushort y, ushort z, TreeOutput output);

        /// <summary> Returns true if any green or trunk blocks are in the cube centred at (x, y, z) of extent 'size'. </summary>
        public static bool TreeCheck(Level lvl, ushort x, ushort y, ushort z, short size) { //return true if tree is near
            for (short dy = (short)-size; dy <= +size; ++dy)
                for (short dz = (short)-size; dz <= +size; ++dz)
                    for (short dx = (short)-size; dx <= +size; ++dx)
            {
                byte tile = lvl.GetTile((ushort)(x + dx), (ushort)(y + dy), (ushort)(z + dz));
                if (tile == Block.trunk || tile == Block.green) return true;
            }
            return false;
        }
        
        
        public static Dictionary<string, Func<Tree>> TreeTypes = 
            new Dictionary<string, Func<Tree>>() {
            { "Fern", () => new NormalTree() }, { "Cactus", () => new CactusTree() },
            { "Notch", () => new ClassicTree() }, { "Swamp", () => new SwampTree() },
            { "Bamboo", () => new BambooTree() }, { "Palm", () => new PalmTree() },
        };
        
        public static Tree Find(string name) {
            foreach (var entry in TreeTypes) {
                if (entry.Key.CaselessEq(name)) return entry.Value();
            }
            return null;
        }
    }
}
