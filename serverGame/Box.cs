using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Do.serverGame
{
    class Box
    {
        private enum Rewards
        {
            Uridium = 75,
            Ammunition = 50,
            //Energy = 49,
            //Jackpott = 40,
            Credits = 30
        }
        public uint Id { get; set; }

        public bool Collected { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public ushort MapId { get; set; }
        private const string Uridium = "uridium";
        private const string Credits = "credits";
        private const string Energy = "energy";
        private const string Jackpott = "jackpott";
        private const string Ammo = "ammo";
        private const string lf = "lf";
        private readonly int[] _boxUri = { new Random().Next(20, 30000) };
        private readonly int[] _boxCre = { new Random().Next(2, 12200) };
        private readonly int[] _boxAmmo = { new Random().Next(1, 21000) };
        private List<uint> _playersInRange = new List<uint>();
        public Box(uint id, ushort mapid)
        {
            Id = id;
            MapId = mapid;
            X = Convert.ToInt32(Program.Random.Next(5, 180) + "00");
            Y = Convert.ToInt32(Program.Random.Next(5, 128) + "00");
         Task.Factory.StartNew(CheckUsers);
        }

        private async void CheckUsers()
        {
            try
            {
                while (true)
                {
                    foreach (var user in _playersInRange)
                    {
                        if (!Program.Users.ContainsKey(user) || Program.Users[user].MapId != MapId || Program.Users[user].DestroyedClass)
                            _playersInRange.Remove(user);
                    }
                    var rnd = new Random();
                    foreach (var user in Program.Maps[MapId].Users.Values)
                    {
                        var x = user.Ship.GetPosX();
                        var y = user.Ship.GetPosY();
                        var dist = Distance.Get(x, X, y, Y);
                        if (dist < 2000)
                        {
                            if (!_playersInRange.Contains(user.UserId) && !Collected)
                            {

                                _playersInRange.Add(user.UserId);
                                user.Send("0|c|" + Id + "|" + /*rnd.Next(3, 6) */2+ "|" + X + "|" + Y);
                            }
                        }
                        else
                        {
                            if (_playersInRange.Contains(user.UserId))
                            {
                                _playersInRange.Remove(user.UserId);
                                user.Send("0|2|" + Id);
                            }
                        }
                    }
                    await Task.Delay(2000);
                }
            }
            catch (Exception) { }
        }
        async void Recreate()
        {
            try
            {
                foreach (var pair in _playersInRange)
                {
                    if (!Program.Users[pair].DestroyedClass)
                        Program.Users[pair].Send("0|2|" + Id);
                }
            }
            catch (Exception) { }
            _playersInRange = new List<uint>();
            await Task.Delay(500);

            X = Program.Random.Next(5, 180) * 100;
            Y = Program.Random.Next(5, 128) * 100;
            Collected = false;
        }

        public void Collect(uint userId)
        {
            if (!_playersInRange.Contains(userId) || Collected) return;
            var x = Program.Users[userId].Ship.GetPosX();
            var y = Program.Users[userId].Ship.GetPosY();
            var dist = Distance.Get(X, x, Y, y);
            if (dist > 200) return;
            Collected = true;
            Recreate();
            var rnd = new Random().Next(1, 10000);
            if (rnd >= 1 && rnd <= 2000)
            {
                UpdatePlayer(Uridium, userId);
            }
            else if (rnd >= 2001 && rnd <= 5800)
            {
                UpdatePlayer(Credits, userId);
            }
            else if (rnd >= 5801 && rnd <= 9999)
            {
                UpdatePlayer(lf, userId);
            }   /* 
            var rando = new Random();
            var luck = rando.Next(0, 93);
            if (luck >= (int)Rewards.Uridium)
            {
                UpdatePlayer(Uridium, userId);
            }
            else if (luck >= (int)Rewards.Ammunition)
            {
                UpdatePlayer(Ammo, userId);

            }
            else if (luck <= (int)Rewards.Credits)
            {
                UpdatePlayer(Credits, userId);
            } */
        }
        private void UpdatePlayer(string type, uint userId)
        {
            Thread updateDatabase = new Thread(this.UpdateDatabase);
            updateDatabase.IsBackground = true;
            updateDatabase.Start(new object[] { type, userId });
        }
        private void UpdateDatabase(object oDetails)
        {
            object[] detailArray = (object[])oDetails;
            string type = detailArray[0].ToString();
            uint userId = uint.Parse(detailArray[1].ToString());

            try
            {

                if (type == Uridium)
                {
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        var data = (DataTable)dbClient.query("SELECT uri FROM server_1_players WHERE playerID=" + userId);
                        if (data.Rows.Count != 1) return;
                        var uri = Convert.ToInt32(data.Rows[0]["uri"]);
                        var reward = new Random().Next(1, 12500);
                        uri += reward;

                        Program.Users[userId].Ship.uri = uri;
                        Program.Users[userId].Send("0|LM|ST|URI|" + reward + "|" + uri);
                        dbClient.query("UPDATE server_1_players SET uri = '" + uri + "' WHERE playerID = '" + userId +
                                       "'");
                    }
                }
                else if (type == Credits)
                {
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        var data =
                            (DataTable)dbClient.query("SELECT credits FROM server_1_players WHERE playerID=" + userId);
                        if (data.Rows.Count != 1) return;
                        var cre = Convert.ToInt32(data.Rows[0]["credits"]);
                        var reward = new Random().Next(1, 12500);
                        cre += reward;
                        Program.Users[userId].Ship.credits = cre;
                        Program.Users[userId].Send("0|LM|ST|CRE|" + reward + "|" + cre);
                        dbClient.query("UPDATE server_1_players SET credits = '" + cre + "' WHERE playerID = '" + userId +
                                       "'");
                    }
                }
                else if (type == lf)
                {
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        var rnd = new Random().Next(1, 10000);
                        if (rnd >= 1 && rnd <= 5000)
                        {
                            dbClient.query("INSERT INTO `server_1_player_all_items` (`id`, `lootid`, `Q`, `LV`, `properties`, `interactive`, `charge`, `playerID`, `userID`, `offitime`) VALUES (NULL, 'equipment_weapon_laser_lf-2', '1', '', '', '', '', '" + userId + "', '" + userId + "', '" + DateTime.Now + "');");
                            Program.Users[userId].Send("0|A|STD|Получен лазер LF-2");
                        }
                        else if (rnd >= 5001 && rnd <= 7000)
                        {
                            dbClient.query("INSERT INTO `server_1_player_all_items` (`id`, `lootid`, `Q`, `LV`, `properties`, `interactive`, `charge`, `playerID`, `userID`, `offitime`) VALUES (NULL, 'equipment_generator_shield_sg3n-b01', '1', '', '', '', '', '" + userId + "', '" + userId + "', '" + DateTime.Now + "');");
                            Program.Users[userId].Send("0|A|STD|Получен щит SG3N-B01");
                        }
                        else if (rnd >= 7001 && rnd <= 7700)
                        {
                            dbClient.query("INSERT INTO `server_1_player_all_items` (`id`, `lootid`, `Q`, `LV`, `properties`, `interactive`, `charge`, `playerID`, `userID`, `offitime`) VALUES (NULL, 'equipment_generator_shield_sg3n-b02', '1', '', '', '', '', '" + userId + "', '" + userId + "', '" + DateTime.Now + "');");
                            Program.Users[userId].Send("0|A|STD|Получен щит SG3N-B02");
                        }
                        else if (rnd >= 7701 && rnd <= 9800)
                        {
                            dbClient.query("INSERT INTO `server_1_player_all_items` (`id`, `lootid`, `Q`, `LV`, `properties`, `interactive`, `charge`, `playerID`, `userID`, `offitime`) VALUES (NULL, 'equipment_weapon_laser_lf-3', '1', '', '', '', '', '" + userId + "', '" + userId + "', '" + DateTime.Now + "');");
                            Program.Users[userId].Send("0|A|STD|Получен лазер LF-3");
                        }                   
                        else if (rnd >= 9801 && rnd <= 9900)
                        {
                            dbClient.query("INSERT INTO `server_1_player_all_items` (`id`, `lootid`, `Q`, `LV`, `properties`, `interactive`, `charge`, `playerID`, `userID`, `offitime`) VALUES (NULL, 'equipment_weapon_laser_lf-4', '1', '', '', '', '', '" + userId + "', '" + userId + "', '" + DateTime.Now + "');");
                            Program.Users[userId].Send("0|A|STD|Получен лазер LF-4");
                        }

                        

                    }
                }
                else if (type == Ammo)
                {
                    {
                        var ammo = Program.loadAmmos(userId, "ammunition_laser_lcb-10");
                        var luck = Program.Random.Next(1, 4);
                        try
                        {
                            switch (luck)
                            {
                                case 1:
                                    break;
                                case 2:
                                    ammo = Program.loadAmmos(userId, "ammunition_laser_mcb-25");
                                    break;
                                case 3:
                                    ammo = Program.loadAmmos(userId, "ammunition_laser_mcb-50");
                                    break;
                                case 4:
                                    ammo = Program.loadAmmos(userId, "ammunition_laser_ucb-100");
                                    break;
                            }

                        }
                        catch (Exception) { }
                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            var reward = _boxAmmo[0];
                            ammo += reward;
                            switch (luck)
                            {
                                case 1:
                                    Program.Users[userId].Ship.x1 = ammo;
                                    dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Program.Users[userId].Ship.x1 +
                                                   "' WHERE playerID = '" + userId +
                                                   "' AND lootid = 'ammunition_laser_lcb-10'");
                                    break;
                                case 2:
                                    Program.Users[userId].Ship.x2 = ammo;
                                    dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Program.Users[userId].Ship.x2 +
                                                   "' WHERE playerID = '" + userId +
                                                   "' AND lootid = 'ammunition_laser_mcb-25'");
                                    break;
                                case 3:
                                    Program.Users[userId].Ship.x3 = ammo;
                                    dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Program.Users[userId].Ship.x3 +
                                                   "' WHERE playerID = '" + userId +
                                                   "' AND lootid = 'ammunition_laser_mcb-50'");
                                    break;
                                case 4:
                                    Program.Users[userId].Ship.UCB100 = ammo;
                                    dbClient.query("UPDATE server_1_player_all_items SET Q = '" +
                                                   Program.Users[userId].Ship.UCB100 + "' WHERE playerID = '" + userId +
                                                   "' AND lootid = 'ammunition_laser_ucb-100'");
                                    break;
                            }
                            Program.Users[userId].Send("0|LM|ST|BAT|" + luck + "|" + reward);
                            Program.Users[userId].Send("0|B|" + Program.Users[userId].Ship.x1 + "|" + Program.Users[userId].Ship.x2 + "|" +
                                               Program.Users[userId].Ship.x3 + "|" + Program.Users[userId].Ship.UCB100 + "|" +
                                               Program.Users[userId].Ship.sab + "|" + Program.Users[userId].Ship.rsb );
                        }
                    }
                }
            }
            catch (Exception) { }
        }
    }
}

