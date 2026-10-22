using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Do.Core;
using Do.game;
using Do.game.usersClass;
using Do.serverGame;
using MySQLManager.Database;
using System.Runtime.InteropServices;
using System.Threading;
namespace Do
{
    class Program
    {
        public class Ships
        {
            public ushort Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public uint Hp { get; set; }
            public uint Shield { get; set; }
            public ushort ShieldAbsorb { get; set; }
            public uint Damage { get; set; }
            public ushort Speed { get; set; }
            public int Uri { get; set; }
            public int Cre { get; set; }
            public int Exp { get; set; }
            public int Hon { get; set; }
            public bool IsNeutral { get; set; }
            public ushort LaserId { get; set; }
            public ushort Ai { get; set; }
        }

        public static DateTime Time = DateTime.Now;
        private static bool _loop;
        public static readonly Random BoxX = new Random();
        public static readonly Random BoxY = new Random();
        public static Dictionary<ushort, Map> Maps;
        public static Dictionary<uint, User> Users;
        public static readonly Random Random = new Random();
        public static Dictionary<ushort, Ships> Npcs;
        //  public string Versionfile = "0.1.0.0";
        public static Dictionary<string, string> Setting;
        public static DatabaseManager Manager;
        private static readonly DateTime UnixDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private static ConsoleEventDelegate _handler;
        private delegate bool ConsoleEventDelegate(int eventType);
        public static string VERSION = "v0.1.6.2 \n JPA v0.3.1   ";

        // Точка входа консольного приложения
        public static void Main(string[] a)
        {
            // Запуск сервера: загрузка настроек/БД + слушатель сокетов (как в server.starter.ui)
            new Thread(Do.Core.AsynchronousSocketListener.StartListening).Start();
            // Не даём процессу завершиться, ждём нажатия Enter для корректной остановки
            Console.ReadLine();
            try
            {
                if (Users != null)
                {
                    foreach (var user in Users)
                    {
                        user.Value.discconectUser(-1);
                    }
                }
            }
            catch
            {
            }
        }

        public void RUN()
        {

            //       SetConsoleCtrlHandler(_handler, true);

            //#if !DEBUG
            //            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            //#endif
            //   Console.SetWindowSize(70, 30);
            _handler = ConsoleEventCallback;
            Out.WriteLine("¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦");
            Out.WriteLine("Starting Dark SPACE/Black SPACE server core");
            Out.WriteLine("Devoloped by Cryz35");
            Out.WriteLine("NavigatoR & SeVeN edition");
            Out.WriteLine("¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦");
            // Сокет-слушатель запускается из Main (AsynchronousSocketListener.StartListening),
            // здесь только инициализация ядра сервера.
            new Program().Server();
            new Program().CountUser();
            //    Console.ReadLine();
        }




        public async void CountUser()
        {
            while (true)
            {
                await Task.Delay(2000);
                var a = DateTime.Now - Time;
                var users = Users.Count;

                //      Console.Title = users + " game clients connected! - " + Process.GetCurrentProcess().PeakPagedMemorySize/1024 + " KB - Running for " + a.Hours + ":" + a.Minutes + ":" + a.Seconds;
            }
        }


        public static bool ConsoleEventCallback(int eventType)
        {
            try
            {
                if (eventType != 2) return false;
                foreach (var user in Users)
                {
                    user.Value.discconectUser(-1);
                }
                return false;
            }
            catch
            {
                Environment.Exit(0);
            }
            return false;
        }
        public static int GetUnixTimestamp
        {
            get { return (int)(DateTime.UtcNow - UnixDateTime).TotalSeconds; }
        }

        public static void RemoveUser(uint userId)
        {
            Do.game.User.online--;

            try
            {
                Users[userId] = null;
                Users.Remove(userId);
                GC.Collect();
            }
            catch (Exception)
            {

            }
        }
        public static void DisconectUser2(uint userId)
        {
            Users[userId].discconectUser(1);
        }

        public async void Server()
        {
            if (!File.Exists("app.ini"))
            {
                Out.WriteLine("Error #1 loading app.ini: El archivo no existe", "Azure.Boot", ConsoleColor.Red);
                //      Console.ReadKey();
                Environment.Exit(0);
            }
            Setting = await ReadSettings(File.ReadAllText("app.ini"));
            if (!Setting.ContainsKey("server") || !Setting.ContainsKey("mport") || !Setting.ContainsKey("username") || !Setting.ContainsKey("password") || !Setting.ContainsKey("dbName") || !Setting.ContainsKey("port") || !Setting.ContainsKey("flushPackets"))
            {
                Out.WriteLine("Error #3 loading app.ini:Error la configuracion no es correcta, chequea los puertos", "Azure.Boot", ConsoleColor.Red);
                //   Console.ReadKey();
                Environment.Exit(0);
            }

            try
            {

                Manager = new DatabaseManager(30, 10, DatabaseType.MySql);
                Manager.setServerDetails(Setting["server"], uint.Parse(Setting["mport"]), Setting["username"], Setting["password"], Setting["dbName"]);
                Manager.init();
                //  new Thread   (del_scr).Start();
            }
            catch (Exception e)
            {
                Out.WriteLine("Error #4 en la coneccion con MySQL: " + e.Message, "MySQL.Connect", ConsoleColor.Red);
                //     Console.ReadKey();
                Environment.Exit(0);
            }

            Users = new Dictionary<uint, User>();
            Maps = new Dictionary<ushort, Map>();
            using (var dbClient = Manager.getQueryreactor())
            {
                Npcs = new Dictionary<ushort, Ships>();
                var data = (DataTable)dbClient.query("SELECT * FROM ships");
                foreach (var alien in from DataRow row in data.Rows
                                      select new Ships
                                      {
                                          Id = Convert.ToUInt16(row["Id"]),
                                          Name = row["Name"].ToString(),
                                          Type = row["type"].ToString(),
                                          Hp = Convert.ToUInt32(row["HP"]),
                                          Shield = Convert.ToUInt32(row["Shield"]),
                                          Damage = Convert.ToUInt32(row["Damage"]),
                                          Speed = Convert.ToUInt16(row["Speed"]),
                                          IsNeutral = ToBool(row["isNeutral"].ToString()),
                                          ShieldAbsorb = Convert.ToUInt16(row["shieldAbsorb"]),
                                          Uri = Convert.ToInt32(row["uridium"]),
                                          Cre = Convert.ToInt32(row["credits"]),
                                          Exp = Convert.ToInt32(row["experience"]),
                                          Hon = Convert.ToInt32(row["honor"]),
                                          LaserId = Convert.ToUInt16(row["LaserId"]),
                                          Ai = Convert.ToUInt16(row["AI"])
                                      })
                {
                    Npcs.Add(alien.Id, alien);
                }

                data = (DataTable)dbClient.query("SELECT * FROM maps");
                foreach (DataRow row in data.Rows)
                {
                    Maps.Add(Convert.ToUInt16(row["id"]), new Map(Convert.ToUInt16(row["id"]), row["Name"].ToString(), row["Portals"].ToString(), row["NPCS"].ToString(), ToBool(row["isStarterMap"].ToString()), Convert.ToInt16(row["factionId"]), Convert.ToUInt16(row["lvl"])));
                }
            }

            Out.WriteLine(" " + Maps.Count + " maps loaded!");
            Out.WriteLine(" " + Npcs.Count + " ships. loaded!");
            Out.WriteLine("Emulator is ready at port 2525");
        }


        public static string ToEnum(bool isTrue)
        {
            return (isTrue) ? "1" : "0";
        }

        public static bool ToBool(string isTrue)
        {
            return (isTrue == "1");
        }

        public static DataRow CheckSession(uint userId, ulong sessionId)
        {
            if (sessionId < 1 || userId < 1000)
            {
                return null;
            }
            using (var dbClient = Manager.getQueryreactor())
            {

                var data =
                    (DataTable)
                        dbClient.query(
                            "SELECT * FROM server_1_players, server_1_player_hangar_general_ship, users WHERE server_1_player_hangar_general_ship.playerID=@ID AND server_1_players.tokenId = @sessionId AND server_1_players.playerID = @ID AND users.ID = server_1_players.userID",
                            (new[] { "sessionId", "ID" }), (new[] { sessionId.ToString(), userId.ToString() }));

                if (data.Rows.Count != 1) return null;
                var row = data.Rows[0];
                if (row["clanId"].ToString() != "0")
                {
                    data =
                        (DataTable)
                            dbClient.query(
                                "SELECT * FROM server_1_players,server_1_player_hangar_general_ship, server_1_clan,users WHERE server_1_player_hangar_general_ship.playerID=@ID AND server_1_players.tokenId = @sessionId AND server_1_players.playerID = @ID AND users.ID = server_1_players.userID AND server_1_clan.clanID = server_1_players.clanId",
                                (new[] { "sessionId", "ID" }), (new[] { sessionId.ToString(), userId.ToString() }));
                }
                return data.Rows[0];

            }

        }

        public static void UpdatePlayer(uint userId, ushort typeId, int rewexp = 0, int rewuri = 0, int rewcre = 0, int rewhonor = 0)
        {
            //if (typeId == 100)
            //{

            //    using (var dbClient = Program.Manager.getQueryreactor())
            //    {

            //        var slotone = "none";
            //        var slottwo = "none";
            //        var slotthree = "none";
            //        var res = (DataTable)dbClient.query("SELECT slot_one,slot_three,slot_two FROM server_1_player_nanotech WHERE playerId = '" + userId + "'");
            //        slotone = res.Rows[0]["slot_one"].ToString();
            //        slottwo = res.Rows[0]["slot_two"].ToString();
            //        slotthree = res.Rows[0]["slot_three"].ToString();
            //        System.Windows.Forms.MessageBox.Show(res.Rows.ToString() + "");
            //        if (/*slotone == "none" && */slotone != "locked")
            //        {

            //            var techone = slotone.Split('|');
            //            var s = Convert.ToInt32(techone[1]);
                    
            //            if (techone[0] == "SRPM" || slotone == "none")
            //            {
            //                // UPDATE `server_1_player_nanotech` SET `slot_one` = 'SBRB|2|0' WHERE `server_1_player_nanotech`.`iddd` = 1463;   
            //                dbClient.query("UPDATE `server_1_player_nanotech` SET `slot_one` = 'SRPM|" + s + "|0' WHERE `server_1_player_nanotech`.`playerID` =" + userId);
            //                System.Windows.Forms.MessageBox.Show(s+""/*"UPDATE `server_1_player_nanotech` SET `slot_one` = 'SRPM|" + s + "|0' WHERE `server_1_player_nanotech`.`playerID` = " + userId*/);
            //            }
            //            s++;
            //            //    Ship.RPMA = Convert.ToInt32(techone[1]);
            //            //if (techone[0] == "SBRB")
            //            //    Ship.BRBA = Convert.ToInt32(techone[1]);
            //            //if (techone[0] == "SSBU")
            //            //    Ship.SBUA = Convert.ToInt32(techone[1]);
            //            //if (techone[0] == "SECI")
            //            //    Ship.ECIA = Convert.ToInt32(techone[1]);
            //            //if (techone[0] == "SELA")
            //            //    Ship.ELAA = Convert.ToInt32(techone[1]);
            //        }
            //        if (/*slottwo == "none" && */slottwo != "locked")
            //        {
            //            var techtwo = slottwo.Split('|');
            //            if (techtwo[0] == "SRPM" || slottwo == "none")
            //            {
            //                var s = Convert.ToInt32(techtwo[1]);
                            
            //                s++;
            //                // UPDATE `server_1_player_nanotech` SET `slot_one` = 'SBRB|2|0' WHERE `server_1_player_nanotech`.`iddd` = 1463;   
            //                dbClient.query("UPDATE `server_1_player_nanotech` SET `slot_two` = 'SRPM|" + s + "|0' WHERE `server_1_player_nanotech`.`playerID` =" + userId);
            //                //   System.Windows.Forms.MessageBox.Show("UPDATE `server_1_player_nanotech` SET `slot_two` = 'SRPM|" + s + "|0' WHERE `server_1_player_nanotech`.`playerID` =" + userId);
            //                System.Windows.Forms.MessageBox.Show(s + ""/*"UPDATE `server_1_player_nanotech` SET `slot_one` = 'SRPM|" + s + "|0' WHERE `server_1_player_nanotech`.`playerID` = " + userId*/);
            //            }

            //            //if (techtwo[0] == "SRPM")
            //            //    Ship.RPMA = Convert.ToInt32(techtwo[1]);
            //            //if (techtwo[0] == "SBRB")
            //            //    Ship.BRBA = Convert.ToInt32(techtwo[1]);
            //            //if (techtwo[0] == "SSBU")
            //            //    Ship.SBUA = Convert.ToInt32(techtwo[1]);
            //            //if (techtwo[0] == "SECI")
            //            //    Ship.ECIA = Convert.ToInt32(techtwo[1]);
            //            //if (techtwo[0] == "SELA")
            //            //    Ship.ELAA = Convert.ToInt32(techtwo[1]);
            //        }
            //        if (/*slotthree == "none" && */slotthree != "locked")
            //        {
            //            var techthree = slotthree.Split('|');
            //            if (techthree[0] == "SRPM" || slotthree == "none")
            //            {
            //                var s = Convert.ToInt32(techthree[1]);


            //                // UPDATE `server_1_player_nanotech` SET `slot_one` = 'SBRB|2|0' WHERE `server_1_player_nanotech`.`iddd` = 1463;   
            //                dbClient.query("UPDATE `server_1_player_nanotech` SET `slot_three` = 'SRPM|" + s + "|0' WHERE `server_1_player_nanotech`.`playerID` =" + userId);
            //                //      System.Windows.Forms.MessageBox.Show("UPDATE `server_1_player_nanotech` SET `slot_three` = 'SRPM|" + s + "|0' WHERE `server_1_player_nanotech`.`playerID` =" + userId);
            //                s++;
            //                System.Windows.Forms.MessageBox.Show(s + ""/*"UPDATE `server_1_player_nanotech` SET `slot_one` = 'SRPM|" + s + "|0' WHERE `server_1_player_nanotech`.`playerID` = " + userId*/);

            //            }
            //            //var techthree = slotthree.Split('|');

            //            //if (techthree[0] == "SRPM")
            //            //    Ship.RPMA = Convert.ToInt32(techthree[1]);
            //            //if (techthree[0] == "SBRB")
            //            //    Ship.BRBA = Convert.ToInt32(techthree[1]);
            //            //if (techthree[0] == "SSBU")
            //            //    Ship.SBUA = Convert.ToInt32(techthree[1]);
            //            //if (techthree[0] == "SECI")
            //            //    Ship.ECIA = Convert.ToInt32(techthree[1]);
            //            //if (techthree[0] == "SELA")
            //            //    Ship.ELAA = Convert.ToInt32(techthree[1]);
            //        }
            //    }
            //}
            if (rewuri != 0)
            {
                using (var dbClient = Manager.getQueryreactor())
                {
                    var data = (DataTable)dbClient.query("SELECT uri,credits,exp,honor FROM server_1_players WHERE playerID=" + userId);
                    if (data.Rows.Count == 1)
                    {
                        var exp = Convert.ToInt32(data.Rows[0]["exp"]);
                        var uri = Convert.ToInt32(data.Rows[0]["uri"]);
                        var credits = Convert.ToInt32(data.Rows[0]["credits"]);
                        var honor = Convert.ToInt32(data.Rows[0]["honor"]);
                        uri += rewuri;
                        credits += rewcre;
                        exp += rewexp;
                        honor += rewhonor;
                        Users[userId].Ship.uri = uri;
                        Users[userId].Ship.credits = credits;
                        Users[userId].Ship.honor = honor;
                        Users[userId].Ship.exp = exp;
                        Users[userId].CheckLevel();
                        dbClient.query("UPDATE server_1_players SET credits = '" + credits + "',uri = '" + uri + "',honor = '" + honor + "',exp = '" + exp + "' WHERE playerID = '" + userId + "'");
                        Users[userId].Send("0|LM|ST|EP|" + rewexp + "|" + exp + "|" + Users[userId].Ship.level);
                        Users[userId].Send("0|LM|ST|CRE|" + rewcre + "|" + credits);
                        Users[userId].Send("0|LM|ST|URI|" + rewuri + "|" + uri);
                        Users[userId].Send("0|LM|ST|HON|" + rewhonor + "|" + honor);
                    }
                }
                return;
            }

            using (var dbClient = Manager.getQueryreactor())
            {
                var data = (DataTable)dbClient.query("SELECT uri,credits,exp,honor FROM server_1_players WHERE playerID=" + userId);
                if (data.Rows.Count == 1)
                {

                    var exp = Convert.ToInt32(data.Rows[0]["exp"]);
                    var uri = Convert.ToInt32(data.Rows[0]["uri"]);
                    var credits = Convert.ToInt32(data.Rows[0]["credits"]);
                    var honor = Convert.ToInt32(data.Rows[0]["honor"]);
                    rewuri = Npcs[typeId].Uri;
                    rewcre = Npcs[typeId].Cre;
                    rewexp = Npcs[typeId].Exp;
                    rewhonor = Npcs[typeId].Hon;
                    uri += rewuri;
                    credits += rewcre;
                    exp += rewexp;
                    honor += rewhonor;
                    Users[userId].Ship.uri = uri;
                    Users[userId].Ship.credits = credits;
                    Users[userId].Ship.honor = honor;
                    Users[userId].Ship.exp = exp;
                    Users[userId].CheckLevel();
                    dbClient.query("UPDATE server_1_players SET credits = '" + credits + "',uri = '" + uri + "',honor = '" + honor + "',exp = '" + exp + "' WHERE playerID = '" + userId + "'");

                    Users[userId].Send("0|LM|ST|EP|" + rewexp + "|" + exp + "|" + Users[userId].Ship.level);
                    Users[userId].Send("0|LM|ST|CRE|" + rewcre + "|" + credits);
                    Users[userId].Send("0|LM|ST|URI|" + rewuri + "|" + uri);
                    Users[userId].Send("0|LM|ST|HON|" + rewhonor + "|" + honor);
                    var result = (DataTable)dbClient.query("SELECT npc" + typeId + " FROM npc_kills WHERE playerID=" + userId);
                    if (result.Rows.Count == 1)
                    {
                        var a = "npc" + typeId.ToString();
                        var kills = int.Parse(result.Rows[0][a].ToString()) + 1;
                        dbClient.query("UPDATE npc_kills SET npc" + typeId + " = '" + kills + "' WHERE playerID = '" + userId + "'");
                    }
                }
            }

        }
        public static DataRow LoadEq(uint userId, uint eqid)
        {
            using (var dbClient = Manager.getQueryreactor())
            {

                var data = (DataTable)dbClient.query("SELECT * FROM server_1_hangar_config_ship WHERE server_1_hangar_config_ship.playerID=@ID AND server_1_hangar_config_ship.configNum=@CID", (new[] { "ID", "CID" }), (new[] { userId.ToString(), eqid.ToString() }));

                return data.Rows.Count == 1 ? data.Rows[0] : null;
            }

        }
        public static string LoadDrones(uint userId)
        {
            try
            {
                using (var dbClient = Manager.getQueryreactor())
                {

                    var data = (DataTable)dbClient.query("SELECT * FROM server_1_player_drones WHERE server_1_player_drones.playerID=@ID", (new[] { "ID" }), (new[] { userId.ToString() }));
                    var dronePacket = "";
                    var totalDrones = 0;
                    string drone1 = "", drone1Lvl = "", drone2 = "", drone2Lvl = "", drone3 = "", drone3Lvl = "", drone4 = "", drone4Lvl = "", drone5 = "", drone5Lvl = "", drone6 = "", drone6Lvl = "", drone7 = "", drone7Lvl = "", drone8 = "", drone8Lvl = "", drone9 = "", drone9Lvl = "", drone10 = "", drone10Lvl = "";
                    foreach (DataRow row in data.Rows)
                    {
                        totalDrones++;
                        switch (totalDrones)
                        {
                            case 1:
                                drone1 = row["drone_kind"].ToString() == "drone_iris" ? "6" : "6";

                                drone1Lvl = row["drone_level"].ToString();
                                break;
                            case 2:
                                drone2 = row["drone_kind"].ToString() == "drone_iris" ? "6" : "6";

                                drone2Lvl = row["drone_level"].ToString();
                                break;
                            case 3:
                                drone3 = row["drone_kind"].ToString() == "drone_iris" ? "6" : "6";

                                drone3Lvl = row["drone_level"].ToString();
                                break;
                            case 4:
                                drone4 = row["drone_kind"].ToString() == "drone_iris" ? "6" : "6";

                                drone4Lvl = row["drone_level"].ToString();
                                break;
                            case 5:
                                drone5 = row["drone_kind"].ToString() == "drone_iris" ? "6" : "4";

                                drone5Lvl = row["drone_level"].ToString();
                                break;
                            case 6:
                                drone6 = row["drone_kind"].ToString() == "drone_iris" ? "6" : "6";

                                drone6Lvl = row["drone_level"].ToString();
                                break;
                            case 7:
                                drone7 = row["drone_kind"].ToString() == "drone_iris" ? "6" : "6";

                                drone7Lvl = row["drone_level"].ToString();
                                break;
                            case 8:
                                drone8 = row["drone_kind"].ToString() == "drone_iris" ? "6" : "6";

                                drone8Lvl = row["drone_level"].ToString();
                                break;
                            case 9:
                                drone9 = row["drone_kind"].ToString() == "drone_apis" ? "6" : "6";

                                drone9Lvl = row["drone_level"].ToString();
                                break;
                            case 10:
                                drone10 = row["drone_kind"].ToString() == "drone_zeus" ? "6" : "6";

                                drone10Lvl = row["drone_level"].ToString();
                                break;
                        }

                    }
                    drone1Lvl = "6";
                    drone2Lvl = "6";
                    drone3Lvl = "6";
                    drone4Lvl = "6";
                    drone5Lvl = "6";
                    drone6Lvl = "6";
                    drone7Lvl = "6";
                    drone8Lvl = "6";
                    drone9Lvl = "6";
                    drone10Lvl = "6";
                    switch (totalDrones)
                    {
                        case 1:
                            dronePacket = "3/2,3/4-" + drone1 + "" + drone1Lvl + ",2/4";
                            break;
                        case 2:
                            dronePacket = "3/5,3/4-" + drone1 + "" + drone1Lvl + "-" + drone2 + "" + drone2Lvl + ",3/3";
                            break;
                        case 3:
                            dronePacket = "3/5,3/4-" + drone3 + "" + drone3Lvl + "-" + drone1 + "" + drone1Lvl + "-" + drone2 + "" + drone2Lvl + ",3/3";
                            break;
                        case 4:
                            dronePacket = "3/5,3/4-" + drone4 + "" + drone4Lvl + "-" + drone3 + "" + drone3Lvl + "-" + drone1 + "" + drone1Lvl + "-" + drone2 + "" + drone2Lvl + ",3/3";
                            break;
                        case 5:
                            dronePacket = "3/2-" + drone4 + "" + drone4Lvl + ",3/4-" + drone3 + "" + drone3Lvl + "-" + drone1 + "" + drone1Lvl + "-" + drone2 + "" + drone2Lvl + ",3/2-" + drone5 + "" + drone5Lvl;
                            break;
                        case 6:
                            dronePacket = "3/2-" + drone6 + "" + drone6Lvl + ",3/4-" + drone4 + "" + drone4Lvl + "-" + drone3 + "" + drone3Lvl + "-" + drone1 + "" + drone1Lvl + "-" + drone2 + "" + drone2Lvl + ",3/2-" + drone5 + "" + drone5Lvl;
                            break;
                        case 7:
                            dronePacket = "3/2-" + drone6 + "" + drone6Lvl + ",3/4-" + drone4 + "" + drone4Lvl + "-" + drone7 + "" + drone7Lvl + "-" + drone1 + "" + drone1Lvl + "-" + drone2 + "" + drone2Lvl + ",3/2-" + drone5 + "" + drone5Lvl + "-" + drone3 + "" + drone3Lvl + "";
                            break;
                        case 8:
                            dronePacket = "3/2-" + drone6 + "" + drone6Lvl + "-" + drone7 + "" + drone7Lvl + ",3/4-" + drone4 + "" + drone4Lvl + "-" + drone3 + "" + drone3Lvl + "-" + drone1 + "" + drone1Lvl + "-" + drone2 + "" + drone2Lvl + ",3/3-" + drone5 + "" + drone5Lvl + "-" + drone8 + "" + drone8Lvl + "";
                            break;
                        case 9:
                            dronePacket = "3/3-" + drone1 + "" + drone1Lvl + "-" + drone2 + "" + drone2Lvl + "-" + drone3 + "" + drone3Lvl + ",3/4-" + drone4 + "" + drone4Lvl + "-" + drone5 + "" + drone5Lvl + "-" + drone6 + "" + drone6Lvl + "-" + drone7 + "" + drone7Lvl + ",3/3-" + drone8 + "" + drone8Lvl + "-" + drone9 + "" + drone9Lvl + "-" + drone10 + "" + drone10Lvl + "";
                            break;
                        case 10:
                            dronePacket = "3/3-" + drone1 + "" + drone1Lvl + "-" + drone2 + "" + drone2Lvl + "-" + drone3 + "" + drone3Lvl + ",3/4-" + drone4 + "" + drone4Lvl + "-" + drone5 + "" + drone5Lvl + "-" + drone6 + "" + drone6Lvl + "-" + drone7 + "" + drone7Lvl + ",3/3-" + drone8 + "" + drone8Lvl + "-" + drone9 + "" + drone9Lvl + "-" + drone10 + "" + drone10Lvl + "";
                            //    dronePacket = "3/3-" + drone1 + "" + drone1Lvl + "-" + drone2 + "" + drone2Lvl + "-" + drone3 + "" + drone3Lvl + "-" + ",3/4-" + "66-66-66-66,3/3-66-66-66";
                            break;
                        case 0:
                            break;
                    }

                    return dronePacket;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static int loadAmmos(uint userId, string eqid)
        {

            using (var dbClient = Manager.getQueryreactor())
            {

                var data = (DataTable)dbClient.query("SELECT Q FROM server_1_player_all_items WHERE server_1_player_all_items.playerID=@ID AND server_1_player_all_items.lootid=@CID", (new[] { "ID", "CID" }), (new[] { userId.ToString(), eqid.ToString() }));
                return data.Rows.Count == 1 ? Convert.ToInt32(data.Rows[0]["q"]) : 0;
            }

        }
        public static string LoadDroneEq(uint userId)
        {

            using (var dbClient = Manager.getQueryreactor())
            {

                var data = (DataTable)dbClient.query("SELECT * FROM server_1_hangar_config_drones WHERE server_1_hangar_config_drones.playerID=@ID", (new[] { "ID" }), (new[] { userId.ToString() }));
                var C2L = "";
                uint damageC1 = 0;
                uint shieldC1 = 0;
                uint damageC2 = 0;
                uint shieldC2 = 0;
                foreach (DataRow row in data.Rows)
                {
                    if (row["EQ2"].ToString() != "")
                    {
                        C2L = row["EQ2"].ToString();
                        if (C2L.Contains('|'))
                        {
                            var c2Lasers = C2L.Split('|');
                            foreach (var it in from item in c2Lasers where item != "" select LoadItem(userId, item).Split('|'))
                            {
                                switch (it[0])
                                {
                                    case "lsr":
                                        damageC2 += Convert.ToUInt32(it[1]);
                                        Users[userId].Ship.lasers2++;
                                        break;
                                    case "shd":
                                        shieldC2 += Convert.ToUInt32(it[1]);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            var it = LoadItem(userId, C2L).Split('|');
                            switch (it[0])
                            {
                                case "lsr":
                                    damageC2 += Convert.ToUInt32(it[1]);
                                    Users[userId].Ship.lasers2++;
                                    break;
                                case "shd":
                                    shieldC2 += Convert.ToUInt32(it[1]);
                                    break;
                            }
                        }
                    }
                    if (row["EQ"].ToString() == "") continue;
                    var c1L = row["EQ"].ToString();
                    if (c1L.Contains('|'))
                    {
                        var c1Lasers = c1L.Split('|');
                        foreach (var it in from item in c1Lasers where item != "" select LoadItem(userId, item).Split('|'))
                        {
                            switch (it[0])
                            {
                                case "lsr":
                                    damageC1 += Convert.ToUInt32(it[1]);
                                    Users[userId].Ship.lasers1++;
                                    break;
                                case "shd":
                                    shieldC1 += Convert.ToUInt32(it[1]);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        var it = LoadItem(userId, c1L).Split('|');
                        switch (it[0])
                        {
                            case "lsr":
                                damageC1 += Convert.ToUInt32(it[1]);
                                Users[userId].Ship.lasers1++;
                                break;
                            case "shd":
                                shieldC1 += Convert.ToUInt32(it[1]);
                                break;
                        }
                    }
                }

                return damageC1 + "|" + shieldC1 + "|" + damageC2 + "|" + shieldC2;
            }

        }
        public static string LoadItem(uint userId, string itemId)
        {
            if (itemId == "")
            {
                return "";
            }

            using (var dbClient = Manager.getQueryreactor())
            {
                var data = (DataTable)dbClient.query("SELECT lootid FROM server_1_player_all_items WHERE server_1_player_all_items.playerID=@ID AND server_1_player_all_items.id=@CID", (new[] { "ID", "CID" }), (new[] { userId.ToString(), itemId.ToString() }));
                if (data.Rows.Count != 1) return "";
                if (data.Rows[0]["lootid"].ToString() == "equipment_weapon_laser_lf-1")
                    return "lsr|40";
                if (data.Rows[0]["lootid"].ToString() == "equipment_weapon_laser_lf-2")
                    return "lsr|100";
                if (data.Rows[0]["lootid"].ToString() == "equipment_weapon_laser_lf-3")
                    return "lsr|150";
                if (data.Rows[0]["lootid"].ToString() == "equipment_weapon_laser_lf-4")
                    return "lsr|250";
                if (data.Rows[0]["lootid"].ToString() == "equipment_weapon_laser_mp-1")
                    return "lsr|60";
                if (data.Rows[0]["lootid"].ToString() == "equipment_generator_shield_sg3n-b01")
                    return "shd|4000";
                if (data.Rows[0]["lootid"].ToString() == "equipment_generator_shield_sg3n-b02")
                    return "shd|15000";
                if (data.Rows[0]["lootid"].ToString() == "equipment_generator_shield_sg3n-a01")
                    return "shd|1000";
                if (data.Rows[0]["lootid"].ToString() == "equipment_generator_shield_sg3n-a02")
                    return "shd|2000";
                if (data.Rows[0]["lootid"].ToString() == "equipment_generator_shield_sg3n-a03")
                    return "shd|5000";
                if (data.Rows[0]["lootid"].ToString() == "equipment_generator_speed_g3n-1010")
                    return "spd|2";
                if (data.Rows[0]["lootid"].ToString() == "equipment_generator_speed_g3n-2010")
                    return "spd|3";
                if (data.Rows[0]["lootid"].ToString() == "equipment_generator_speed_g3n-3210")
                    return "spd|4";
                if (data.Rows[0]["lootid"].ToString() == "equipment_generator_speed_g3n-3310")
                    return "spd|5";
                if (data.Rows[0]["lootid"].ToString() == "equipment_generator_speed_g3n-6900")
                    return "spd|7";
                return data.Rows[0]["lootid"].ToString() == "equipment_generator_speed_g3n-7900" ? "spd|25" : data.Rows[0]["lootid"].ToString(); // скорость по умолчанию 10
            }

        }

        public static void Settings(uint userId)
        {
            using (var dbClient = Manager.getQueryreactor())
            {
                var data = (DataTable)dbClient.query("SELECT * FROM server_1_player_settings WHERE playerID=" + userId);
                if (data.Rows.Count != 1) return;
                var row = data.Rows[0];
                Users[userId].Settings = new Setting(Convert.ToInt32(row["playerID"]), row["SET"].ToString(), row["MINIMAP_SCALE"].ToString(), row["DISPLAY_PLAYER_NAMES"].ToString(), row["DISPLAY_CHAT"].ToString(), row["PLAY_MUSIC"].ToString(), row["PLAY_SFX"].ToString(), row["BAR_STATUS"].ToString(), row["WINDOW_SETTINGS"].ToString(), row["AUTO_REFINEMENT"].ToString(), row["QUICKSLOT_STOP_ATTACK"].ToString(), row["DOUBLECLICK_ATTACK"].ToString(), row["AUTO_START"].ToString(), row["DISPLAY_NOTIFICATIONS"].ToString(), row["SHOW_DRONES"].ToString(), row["DISPLAY_WINDOW_BACKGROUND"].ToString(), row["ALWAYS_DRAGGABLE_WINDOWS"].ToString(), row["PRELOAD_USER_SHIPS"].ToString(), row["QUALITY_PRESETTING"].ToString(), row["QUALITY_CUSTOMIZED"].ToString(), row["QUALITY_BACKGROUND"].ToString(), row["QUALITY_POIZONE"].ToString(), row["QUALITY_SHIP"].ToString(), row["QUALITY_ENGINE"].ToString(), row["QUALITY_COLLECTABLE"].ToString(), row["QUALITY_ATTACK"].ToString(), row["QUALITY_EFFECT"].ToString(), row["QUALITY_EXPLOSION"].ToString(), row["QUICKBAR_SLOT"].ToString(), row["SLOTMENU_POSITION"].ToString(), row["SLOTMENU_ORDER"].ToString(), row["MAINMENU_POSITION"].ToString());
            }
        }

        #region Azure.Boot Functions
        static async Task<Dictionary<string, string>> ReadSettings(string settingsIni)
        {
            var settings = new Dictionary<string, string>();

            try
            {
                var strReader = new StringReader(settingsIni);
                string line;

                while ((line = await strReader.ReadLineAsync()) != null)
                {
                    if (line.Length < 1 || line.StartsWith("#") || line.StartsWith("["))
                    {
                        continue;
                    }

                    var delimiterIndex = line.IndexOf("=", StringComparison.Ordinal);

                    if (delimiterIndex != -1)
                    {
                        var key = line.Substring(0, delimiterIndex);
                        var val = line.Substring(delimiterIndex + 1);

                        try
                        {
                            settings.Add(key, val);
                        }
                        catch
                        {
                        }
                    }
                }

                strReader.Close();
            }
            catch (Exception e)
            {
                Out.WriteLine("Error #2 loading app.ini: " + e.Message, "Azure.Boot", ConsoleColor.Red);
                //   Console.ReadKey();
                Environment.Exit(0);
            }

            return settings;
        }
        #endregion
    }

    public static class Out
    {

        public static string out___ = "";

        public static void WriteLine(string format, string header = "", ConsoleColor color = ConsoleColor.White)
        {

            out___ = format + header;
            // Консольный вывод сообщений сервера
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" " + DateTime.Now + " - ");
            if (header != "")
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("[" + header + "] ");
            }
            Console.ForegroundColor = color;
            Console.WriteLine(format);
            Console.ResetColor();
        }
    }
}