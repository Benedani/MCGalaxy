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

namespace MCGalaxy.Drawing.Brushes {
    public sealed class CloudyBrushFactory : BrushFactory {       
        public override string Name { get { return "Cloudy"; } }        
        public override string[] Help { get { return HelpString; } }
        
        public static string[] HelpString = new [] {
            "%TArguments: [block1/frequency] [block2] <args>..",
            "%HDraws by selecting blocks from the given [blocks] using perlin noise.",
            "%Hfrequency is optional (defaults to 1), and specifies the number of times " +
                "the block should appear (as a fraction of the total of all the frequencies).",
            "%HOptional args format: %T<first letter of argument>=<value>",
            "%HArguments: %Ta%Hmplitude, %Tf%Hrequency (scale), %Ts%Heed, " +
            "%To%Hctaves, %Tp%Hersistence (turbulence), %Tl%Hacunarity",
        };
        
        public override Brush Construct(BrushArgs args) {
            NoiseArgs n = default(NoiseArgs);
            // Constants borrowed from fCraft to match it
            n.Amplitude = 1;
            n.Frequency = 0.08f;
            n.Octaves = 3;
            n.Seed = int.MinValue;
            n.Persistence = 0.75f;
            n.Lacunarity = 2;
            
            int[] count;
            ExtBlock[] toAffect = FrequencyBrush.GetBlocks(args, out count,
                                            Filter, arg => Handler(arg, args.Player, ref n));
            
            if (toAffect == null) return null;
            return new CloudyBrush(toAffect, count, n);
        }
        
        // Only want to handle non block options.
        static bool Filter(string arg) { return arg.Length < 2 || !(arg[1] == '_' || arg[1] == '='); }
        
        static bool Handler(string arg, Player p, ref NoiseArgs args) {
            char opt = arg[0];
            arg = arg.Substring(2); // get part after _ or =
            
            if (opt == 'l') return ParseDecimal(p, arg, ref args.Lacunarity, 2.00f);
            if (opt == 'a') return ParseDecimal(p, arg, ref args.Amplitude, 1.00f);
            if (opt == 'f') return ParseDecimal(p, arg, ref args.Frequency, 0.08f);
            if (opt == 'p') return ParseDecimal(p, arg, ref args.Persistence, 0.75f);
            
            if (opt == 'o') {
                if (byte.TryParse(arg, out args.Octaves)
                    && args.Octaves > 0 && args.Octaves <= 16) return true;
                Player.Message(p, "\"{0}\" was not an integer between 1 and 16.", arg);
            } else if (opt == 's') {
                if (int.TryParse(arg, out args.Seed)) return true;
                Player.Message(p, "\"{0}\" was not a valid integer.", arg);
            } else {
                Player.Message(p, "\"{0}\" was not a valid argument name.", opt);
            }
            return false;
        }
        
        static bool ParseDecimal(Player p, string arg, ref float target, float baseValue) {
            float temp;
            if (!Utils.TryParseDecimal(arg, out temp)) {
                Player.Message(p, "\"{0}\" was not a valid decimal.", arg); return false;
            }
            
            target = temp * baseValue;
            return true;
        }
    }
    
    public struct NoiseArgs {
        public byte Octaves;
        public int Seed;
        public float Frequency, Amplitude, Persistence, Lacunarity;
    }
}
