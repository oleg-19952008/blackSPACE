using System.Linq.Expressions;
using System.Threading;
using Do.Assembly;
using Do.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Do.game.usersClass;
using System.Diagnostics;

namespace Do.game
{

/*
 * Partial-class User: PvE-логика: опыт/уровень (CheckLevel), получение урона, запись в БД (Updat).
 */
partial class User
{
        public static int exp_;
        public void CheckLevel()
        {
      
            var level = Ship.level;
            var exp = Ship.exp;   exp_ = exp;
            var newLevel = level;
            var level1 = 10000;
            var level2 = level1 * 2;
            var level3 = level2 * 2;
            var level4 = level3 * 2;
            var level5 = level4 * 2;
            var level6 = level5 * 2;
            var level7 = level6 * 2;
            var level8 = level7 * 2;
            var level9 = level8 * 2;
            var level10 = level9 * 2;
            var level11 = level10 * 2;
            var level12 = level11 * 2;
            var level13 = level12 * 2;
            var level14 = level13 * 2;
            var level15 = level14 * 2;
            var level16 = level15 * 2;
            var level17 = level16 * 2;
            var level18 = level17 * 2;
            var level19 = level18 * 2;
            var level20 = level19 * 2;
            if (level == 1 && exp > level1)
            {
                newLevel = 2;
            }
            else if (level == 2 && exp > level2)
            {
                newLevel = 3;
            }
            else if (level == 3 && exp > level3)
            {
                newLevel = 4;
            }

            else if (level == 4 && exp > level4)
            {
                newLevel = 5;
            }

            else if (level == 5 && exp > level5)
            {
                newLevel = 6;
            }

            else if (level == 6 && exp > level6)
            {
                newLevel = 7;
            }

            else if (level == 7 && exp > level7)
            {
                newLevel = 8;
            }

            else if (level == 8 && exp > level8)
            {
                newLevel = 9;
            }
            else if (level == 9 && exp > level9)
            {
                newLevel = 10;
            }
            else if (level == 10 && exp > level10)
            {
                newLevel = 11;
            }
            else if (level == 11 && exp > level11)
            {
                newLevel = 12;
            }
            else if (level == 12 && exp > level12)
            {
                newLevel = 13;
            }
            else if (level == 13 && exp > level13)
            {
                newLevel = 14;
            }
            else if (level == 14 && exp > level14)
            {
                newLevel = 15;
            }
            else if (level == 15 && exp > level15)
            {
                newLevel = 16;
            }
            else if (level == 16 && exp > level16)
            {
                newLevel = 17;
            }
            else if (level == 17 && exp > level17)
            {
                newLevel = 18;
            }
            else if (level == 18 && exp > level18)
            {
                newLevel = 19;
            }
            else if (level == 19 && exp > level19)
            {
                newLevel = 20;
            }
            if (level == newLevel)
            {
                return;
            }
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_players SET level = '" + Ship.level + "' WHERE playerID = '" + UserId + "'");
            }
            Ship.level = newLevel;
            Send("0|A|LUP|" + newLevel);

        }
        public void TakeDamage(int damage)
        {
            _damageTaken += damage;
        }
        public void DropDamage(int damage)
        {
            _damageTaken -= damage;
        }
        private async void SendDamage()
        {
            try
            {
                while (!DestroyedClass)
                {
                    if (_damageTaken < 0)
                    {
                        _damageTaken = 0;
                    }
                    else if (_damageTaken > 0)
                    {
                        var packet = "0|Y|" + UserId + "|" + UserId + "|L|" + Ship.HP + "|" + Ship.shield + "|" + _damageTaken + "|100|1";
                        Send(packet);
                        DropDamage(_damageTaken);
                    }
                    await Task.Delay(990);
                }
            }
            catch (Exception) { }

        }
        public async void UpdateUser(int num, uint id)
        {
            var updateDatabase = new Thread(Updat);
            updateDatabase.IsBackground = true;
            updateDatabase.Start(new object[] { num, id });
            await Task.Delay(20);
        }
        public void Updat(object Details)
        {
            try
            {

                object[] detailArray = (object[])Details;
                int num = Convert.ToInt32(detailArray[0]);
                uint id = uint.Parse(detailArray[1].ToString());
                Ship.isAttacking = false;

                switch (num)
                {
                    case 3:
                        // ГИБЕЛЬ ИГРОКА ОТ NPC: игрок должен возрождаться на
                        // СТАРТОВОЙ карте своей фракции, а не там, где умер.
                        // Раньше в БД писались координаты места гибели (GetXY),
                        // и при следующем входе игрока выбрасывало ровно на то
                        // же место, где он помер.
                        Program.Maps[MapId].RemoveUser(id);
                        Program.DisconectUser2(id);
                        Ship.selectedUserId = 0;
                        Ship.isAttacking = false;
                        Ship.destroyed = false;
                        Ship.HP = Ship.maxHP;
                        Ship.shield = Ship.maxShield;

                        ushort homeMap; int homeX, homeY;
                        if (!TryGetHomeSpawn(out homeMap, out homeX, out homeY))
                        {
                            // Страховка: если стартовая карта неизвестна — пишем
                            // текущие координаты (прежнее поведение).
                            var cur = GetXY();
                            homeMap = (ushort)cur[2]; homeX = cur[0]; homeY = cur[1];
                        }
                        Ship.x = homeX;
                        Ship.y = homeY;
                        MapId = homeMap;

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_players SET x = '" + homeX + "',y = '" + homeY + "',mapId = '" + homeMap + "' WHERE playerID = '" +
                                           UserId + "'");
                        }
                        // Фоновый SavePositionToDatabase() не должен перезатереть
                        // точку респауна координатами места гибели.
                        _lastSavedMapId = homeMap;
                        _lastSavedX = homeX;
                        _lastSavedY = homeY;
                        foreach (var pair in Program.Users[id].PlayersInRange)
                        {
                            try
                            {
                                Program.Users[pair].Send("0|K|" + id);
                            }
                            catch (Exception) { }
                        }

                        break;
                    case 1:
                        foreach (var pair in Program.Users[id].PlayersInRange)
                        {
                            try
                            {
                                Program.Users[pair].Send("0|K|" + id);
                            }
                            catch (Exception e) { }
                        }
                        update = Program.Users[id].GetXY();

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            var data = (DataTable)dbClient.query("SELECT uri,credits,exp,honor FROM server_1_players WHERE playerID=" + UserId);
                            if (data.Rows.Count == 1)
                            {
                                var exp = Convert.ToInt32(data.Rows[0]["exp"]);

                                var honor = Convert.ToInt32(data.Rows[0]["honor"]);

                                exp += Program.Npcs[Convert.ToUInt16(Program.Users[id].Ship.Id)].Exp;
                                if (Program.Maps[MapId].IsPvp)
                                    exp += Program.Npcs[Convert.ToUInt16(Program.Users[id].Ship.Id)].Exp / 2;
                                if (Program.Users[id].Ship.GetFactionID() == Ship.GetFactionID())
                                {
                                    honor -= Program.Npcs[Convert.ToUInt16(Program.Users[id].Ship.Id)].Hon;

                                    Send("0|LM|ST|HON|-" + Program.Npcs[Convert.ToUInt16(Program.Users[id].Ship.Id)].Hon +
                                         "|" + honor);
                                }
                                else
                                {
                                    honor += Program.Npcs[Convert.ToUInt16(Program.Users[id].Ship.Id)].Hon;

                                    if (Program.Maps[MapId].IsPvp)
                                        honor += Program.Npcs[Convert.ToUInt16(Program.Users[id].Ship.Id)].Hon / 2;

                                    Send("0|LM|ST|HON|" + Program.Npcs[Convert.ToUInt16(Program.Users[id].Ship.Id)].Hon +
                                         "|" + honor);
                                }
                                dbClient.query("UPDATE server_1_players SET honor = '" + honor + "',exp = '" + exp + "' WHERE playerID = '" + UserId + "'");

                                Send("0|LM|ST|EP|" + Program.Npcs[Convert.ToUInt16(Program.Users[id].Ship.Id)].Exp + "|" + exp + "|" + Ship.level);

                                Ship.honor = honor;
                                Ship.exp = exp;
                                CheckLevel();
                                var typeId = Convert.ToUInt16(Program.Users[id].Ship.Id);
                                if (typeId == 16 || typeId == 17 || typeId == 18 || typeId == 60 || typeId == 58)
                                    typeId = 8;
                                if (typeId == 19 || typeId == 52 || typeId == 53 || typeId == 54 || typeId == 56 || typeId == 59 || typeId == 61 || typeId == 62 || typeId == 63 || typeId == 64 || typeId == 65 || typeId == 66 || typeId == 67 || typeId == 68 || typeId == 86 || typeId == 87 || typeId == 88 || typeId == 109 || typeId == 110)
                                    typeId = 10;

                                dbClient.query("UPDATE player_kills_count SET ship" + typeId + " = ship" + typeId + "+1 WHERE playerID = '" + UserId + "'");

                            }
                        }
                        //Program.UpdatePlayer(id, Convert.ToUInt16(Program.Users[id].Ship.Id));
                        Program.Maps[MapId].RemoveUser(Ship.selectedUserId);
                        Program.DisconectUser2(Ship.selectedUserId);
                        Ship.selectedUserId = 0;
                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_players SET x = '" + update[0] + "',y = '" + update[1] + "',mapId = '" + update[2] + "' WHERE playerID = '" +
                                           id + "'");
                        }

                        break;
                    case 2:
                        {
                            foreach (var pair in Program.Maps[MapId].Aliens[id].Move.PlayersInRange)
                            {
                                try
                                {
                                    Program.Users[pair].Send("0|K|" + id);
                                }
                                catch (Exception e) { }
                            }

                            var alienId = id;
                            Ship.selectedUserId = 0;
                            Program.Maps[MapId].Aliens[alienId].Destroyed = true;
                            Program.Maps[MapId].Aliens[alienId].Regenerate();

                            foreach (var player in Program.Maps[MapId].Users.Where(player => player.Value.Ship.selectedUserId == alienId))
                            {
                                player.Value.Ship.selectedUserId = 0;
                                player.Value.Ship.isAttacking = false;
                                player.Value.Send("0|N|-1");
                                player.Value.Ship.selected1 = false;
                            }
                        }
                        break;
                }
            }
            catch
            { }
        }
        // changed_ объявлен в Users.Database.cs (тот же partial-класс User)

}
}
