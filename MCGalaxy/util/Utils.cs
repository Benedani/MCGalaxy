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
using System.Collections;
using System.Globalization;
using System.Text;

namespace MCGalaxy { 
    public static class Utils {

        public static bool CheckHex(Player p, ref string arg) {
            if (arg.Length > 0 && arg[0] == '#')
                arg = arg.Substring(1);
            if (arg.Length != 6 || !IsValidHex(arg)) {
                Player.Message(p, "\"#{0}\" is not a valid HEX color.", arg); return false;
            }
            return true;
        }

        static bool IsValidHex(string hex) {
            for (int i = 0; i < hex.Length; i++) {
                if (!Colors.IsStandardColor(hex[i])) return false;
            }
            return true;
        }
        
        public static unsafe void memset(IntPtr srcPtr, byte value, int startIndex, int bytes) {
            byte* srcByte = (byte*)srcPtr + startIndex;
            // Make sure we do an aligned write/read for the bulk copy
            while (bytes > 0 && (startIndex & 0x7) != 0) {
                *srcByte = value; srcByte++; bytes--;
                startIndex++;
            }
            uint valueInt = (uint)((value << 24) | (value << 16) | (value << 8) | value );
            
            if (IntPtr.Size == 8) {
                ulong valueLong = ((ulong)valueInt << 32) | valueInt;
                ulong* srcLong = (ulong*)srcByte;
                while (bytes >= 8) {
                    *srcLong = valueLong; srcLong++; bytes -= 8;
                }
                srcByte = (byte*)srcLong;
            } else {
                uint* srcInt = (uint*)srcByte;
                while (bytes >= 4) {
                    *srcInt = valueInt; srcInt++; bytes -= 4;
                }
                srcByte = (byte*)srcInt;
            }
            
            for (int i = 0; i < bytes; i++) {
                *srcByte = value; srcByte++;
            }
        }
        
        public static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct {
            try {
                result = (TEnum)Enum.Parse(typeof(TEnum), value, true);
                return true;
            } catch (Exception) {
                result = default(TEnum);
                return false;
            }
        }

        public static int Clamp(int value, int lo, int hi) {
            return Math.Max(Math.Min(value, hi), lo);
        }
        
        public static decimal Clamp(decimal value, decimal lo, decimal hi) {
            return Math.Max(Math.Min(value, hi), lo);
        }

        public static double Clamp(double value, double lo, double hi) {
            return Math.Max(Math.Min(value, hi), lo);
        }
        
        // Not all languages use . as their decimal point separator
        public static bool TryParseDecimal(string s, out float result) {
            result = 0;
            float temp;
            const NumberStyles style = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite 
                | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
            
            if (!Single.TryParse(s, style, NumberFormatInfo.InvariantInfo, out temp)) return false;
            if (Single.IsInfinity(temp) || Single.IsNaN(temp)) return false;
            result = temp;
            return true;
        }
                
        
        const StringComparison comp = StringComparison.OrdinalIgnoreCase;
        public static T FindMatches<T>(Player pl, string name, out int matches, IEnumerable items,
                                             Predicate<T> filter, Func<T, string> nameGetter, string type, int limit = 5)  {
            T match = default(T); matches = 0;
            name = name.ToLower();
            StringBuilder matchNames = new StringBuilder();

            foreach (T item in items) {
                if (!filter(item)) continue;
                string itemName = nameGetter(item);
                if (itemName.Equals(name, comp)) { matches = 1; return item; }
                if (itemName.IndexOf(name, comp) < 0) continue;
                
                match = item; matches++;
                if (matches <= limit)
                    matchNames.Append(itemName).Append(", ");
                else if (matches == limit + 1)
                    matchNames.Append("(and more)").Append(", ");
            }
            
            if (matches == 0) {
                Player.Message(pl, "No " + type + " match \"" + name + "\"."); return default(T);
            } else if (matches == 1) {
                return match;
            } else {
                string count = matches > limit ? limit + "+ " : matches + " ";
                string names = matchNames.ToString(0, matchNames.Length - 2);
                Player.Message(pl, count + type + " match \"" + name + "\":");
                Player.Message(pl, names); return default(T);
            }
        }
    }
}
