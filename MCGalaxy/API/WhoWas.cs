using System;

namespace MCGalaxy {
    public class WhoWas {
        public string rank { get; set; }
        public int modified_blocks { get; set; }
        public string time { get; set; }
        public string first_login { get; set; }
        public string last_login { get; set; }
        public int total_logins { get; set; }
        public bool banned { get; set; }
        public string ban_reason { get; set; }
        public string banned_by { get; set; }
        public string banned_time { get; set; }
        public int kicks { get; set; }

        public WhoWas(string p) {
            rank = Group.findPlayer(p);
            PlayerData pl = PlayerInfo.FindData(p);
            if (pl == null) return;
            
            modified_blocks = (int)pl.TotalModified;
            time = pl.TotalTime;
            first_login = pl.FirstLogin.ToString("yyyy-MM-dd HH:mm:ss");
            last_login = pl.LastLogin.ToString("yyyy-MM-dd HH:mm:ss");
            total_logins = pl.Logins;
            kicks = pl.Kicks;
        }
    }
}