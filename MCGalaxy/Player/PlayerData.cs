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
using System.Data;
using MCGalaxy.DB;
using MCGalaxy.SQL;

namespace MCGalaxy {
    public class PlayerData {
        public string Name, Color, Title, TitleColor, TotalTime, IP;
        public DateTime FirstLogin, LastLogin;
        public int UserID, Money, Deaths, Logins, Kicks;
        public long TotalModified, TotalDrawn, TotalPlaced, TotalDeleted;
        
        internal static void Create(Player p) {
            p.prefix = "";
            p.time = new TimeSpan(0, 0, 0, 1);
            p.title = "";
            p.titlecolor = "";
            p.color = p.group.color;
            p.money = 0;
            
            p.firstLogin = DateTime.Now;
            p.totalLogins = 1;
            p.totalKicked = 0;
            p.overallDeath = 0;
            p.overallBlocks = 0;
            p.TotalDrawn = 0;
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            Database.Backend.AddRow("Players", "Name, IP, FirstLogin, LastLogin, totalLogin, Title, " +
                                    "totalDeaths, Money, totalBlocks, totalKicked, TimeSpent",
                                    p.name, p.ip, now, now, 1, "", 0, 0, 0, 0, p.time.ToDBTime());
            
            using (DataTable ids = Database.Backend.GetRows("Players",
                                                            "ID", "WHERE Name = @0", p.name)) {
                if (ids.Rows.Count > 0) {
                    string id = ids.Rows[0]["ID"].ToString();
                    p.UserID = PlayerData.ParseInt(id);
                } else {
                    p.UserID = NameConverter.InvalidNameID(p.name);
                }
            }
        }
        
        internal static void Load(DataTable playerDb, Player p) {
            PlayerData data = PlayerData.Fill(playerDb.Rows[0]);
            p.totalLogins = data.Logins + 1;
            p.time = data.TotalTime.ParseDBTime();
            p.UserID = data.UserID;
            p.firstLogin = data.FirstLogin;
            p.lastLogin = data.LastLogin;
            
            p.title = data.Title;
            if (p.title != "") p.title = p.title.Replace("[", "").Replace("]", "");
            
            p.titlecolor = data.TitleColor;
            p.color = data.Color;
            if (p.color == "") p.color = p.group.color;
            
            p.overallDeath = data.Deaths;
            p.overallBlocks = data.TotalModified;
            p.TotalDrawn = data.TotalDrawn;
            p.TotalPlaced = data.TotalPlaced;
            p.TotalDeleted = data.TotalDeleted;
            
            p.money = data.Money;
            p.totalKicked = data.Kicks;
        }
        
        public static PlayerData Fill(DataRow row) {
            PlayerData data = new PlayerData();
            data.Name = row["Name"].ToString().Trim();
            data.IP = row["IP"].ToString().Trim();
            data.UserID = ParseInt(row["ID"].ToString());
            
            data.TotalTime = row["TimeSpent"].ToString();
            data.FirstLogin = DateTime.Parse(row["FirstLogin"].ToString());
            data.LastLogin = DateTime.Parse(row["LastLogin"].ToString());
            
            data.Title = row["Title"].ToString().Trim();
            data.TitleColor = ParseColor(row["title_color"]);
            data.Color = ParseColor(row["color"]);
            
            data.Money = ParseInt(row["Money"].ToString());
            data.Deaths = ParseInt(row["TotalDeaths"].ToString());
            data.Logins = ParseInt(row["totalLogin"].ToString());
            data.Kicks = ParseInt(row["totalKicked"].ToString());
            
            long blocks = ParseLong(row["totalBlocks"].ToString());
            long cuboided = ParseLong(row["totalCuboided"].ToString());
            data.TotalModified = blocks & LowerBitsMask;
            data.TotalPlaced = blocks >> LowerBits;
            data.TotalDrawn = cuboided & LowerBitsMask;
            data.TotalDeleted = cuboided >> LowerBits;
            return data;
        }
        
        
        internal static long ParseLong(string value) {
            return (value == "" || value.CaselessEq("null")) ? 0 : long.Parse(value);
        }
        
        internal static int ParseInt(string value) {
            return (value == "" || value.CaselessEq("null")) ? 0 : int.Parse(value);
        }
        
        static string ParseColor(object value) {
            string col = value.ToString().Trim();
            if (col == "") return col;
            
            // Try parse color name, then color code
            string parsed = Colors.Parse(col);
            if (parsed != "") return parsed;
            return Colors.Name(col) == "" ? "" : col;
        }
        
        
        internal static long BlocksPacked(long placed, long modified) {
            return placed << LowerBits | modified;
        }
        
        internal static long CuboidPacked(long deleted, long drawn) {
            return deleted << LowerBits | drawn;
        }

        public const int LowerBits = 38;
        public const long LowerBitsMask = (1L << LowerBits) - 1;
    }
}
