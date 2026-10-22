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
    class User
    {
        Socket _handler;
        private readonly byte[] _buffer = new byte[2048];
        private bool _userAuth;
        private bool _socketClosed;
        private bool _isboss;
        private bool _canLoadHellstorm = true;
        public bool DestroyedClass = false;
        // Spaceball (стационарные боевые станции MMO/EIC/VRU).
        // Координаты НИЖЕ — это НЕ поле spaceball и НЕ ворота: это позиции самих
        // станций на картах 1-1 / 5-1 / 9-9 (фракционные NPC-станции). Они взяты
        // один-в-один из пакета "0|s|0|1|...|<x>|<y>", который сервер рассылает
        // клиенту в sendStations(), и точно такие же значения ({1000,1000,1},
        // {19800,1000,5}, {19600,11900,9}) лежали в _mmoXy/_eicXy/_vruXy в
        // исходном коде (до правок). Поле самого spaceball (карта 4-4, ворота
        // сверху/снизу/слева у края карты и мяч в центре) сервером не моделируется
        // — оно целиком на стороне клиента/движка, менять там нечего.
        // factionId 1 -> MMO (1-1), 2 -> EIC (5-1), 3 -> VRU (9-9).
        public static readonly int[][] SpaceballStations =
        {
            new int[] { 1, 1000, 1000, 1 },     // MMO  (1-1)
            new int[] { 2, 19800, 1000, 5 },    // EIC  (5-1)
            new int[] { 3, 19600, 11900, 9 },   // VRU  (9-9)
        };

        public static int[] FindSpaceballStation(int factionOrMap, bool byFaction)
        {
            foreach (var st in SpaceballStations)
                if (byFaction ? st[0] == factionOrMap : st[3] == factionOrMap)
                    return st;
            return null;
        }

        public bool GodMode = false;
        public int LastPing = 0;
        public int Blk = 0;
        public int GodModeDelay = 300;
        private bool _repairRequest;
        private bool _isRepairing;
        private bool _cancelARocket = true;
        public uint UserId, AttackId = 0;
        public ushort MapId = 1;
        public ushort LaserId = 1;
        public ushort RocketId = 1;
        public ushort Precision = 0;
        public ushort LoadedHellstorm = 0;
        public ushort HellstormId = 8;
        public string Ip;
        public int CurrentConfig = 1;
        public DateTime Config = new DateTime(2013, 3, 1, 7, 0, 0);
        public bool EnergyLeechActive = false;
        private DateTime _sbu = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _brb = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _ela = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _rpm = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _eci = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _ish = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _emp = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _smb = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _mine = new DateTime(2013, 3, 1, 7, 0, 0);
        /*
                private DateTime Fwx = new DateTime(2013, 3, 1, 7, 0, 0);
        */
        public uint CreatedFireworks = 0;
        public List<uint> InvitationNames = new List<uint>();
        public List<uint> GroupNames = new List<uint>();
        public List<uint> PlayersInRange = new List<uint>();
        public List<uint> LaserCooldownList = new List<uint>();
        private int _damageTaken;
        public static Dictionary<uint, User> Users;
        // Счетчик онлайна изменяется из потоков обработки сокетов и фоновых циклов,
        // поэтому операции инкремента/декремента должны быть атомарными (Interlocked).
        private static int _online = 0;
        public static int online
        {
            get { return System.Threading.Interlocked.CompareExchange(ref _online, 0, 0); }
            set { System.Threading.Interlocked.Exchange(ref _online, value); }
        }
        public static void IncrementOnline()
        {
            System.Threading.Interlocked.Increment(ref _online);
        }
        // Флаг "этот пользователь учтён в счётчике онлайна" (1 — учтён, 0 — снят).
        // Гарантирует, что декремент выполняется ровно один раз на сессию.
        private int _onlineCounted = 0;
        public void MarkOnlineCounted()
        {
            System.Threading.Interlocked.Exchange(ref _onlineCounted, 1);
        }
        // Потокобезопасный декремент: никогда не уходит в отрицательные значения.
        public static void DecrementOnline()
        {
            int current;
            do
            {
                current = System.Threading.Interlocked.CompareExchange(ref _online, 0, 0);
                if (current <= 0) return;
            }
            while (System.Threading.Interlocked.CompareExchange(ref _online, current - 1, current) != current);
        }
        #region usersClass
        public Clan Clan;
        public Ship Ship;
        public Setting Settings;
        public SettingsAssembly Setting;
        public Pet Pet;
        #endregion
    public   static Socket __handler;
        public User(Socket handler)
        {
            __handler = handler;
            _handler = handler;

            Ip = handler.RemoteEndPoint.ToString().Split(':')[0];

            handler.BeginReceive(_buffer, 0, _buffer.Length, 0, ReadCallback, this);
        }
        public static int User_id___ = 0;
        public async void UpdateShip_()
        {
            // БЫЛО: while(true) без задержки и без проверки выхода — бесконечный
            // "fire-and-forget" цикл в async void, который продолжал слать пакеты
            // уже отключённому игроку (спам в лог/сокет после дисконнекта).
            // СТАЛО: цикл останавливается при закрытии сокета/уничтожении класса.
            // ВАЖНО: здесь отправляются только speed/бустеры/снаряжение — НИКАКИХ
            // пакетов HP (HPT/HL/N/RDY|I), чтобы не перетирать актуальное HP корабля
            // на клиенте (см. регрессию "корабль без хп").
            Send("0|A|BS|0/0/" + Ship.dmgbooster + "/" + Ship.shieldbooster + "/0/0/0/" + Ship.hpbooster);
            try
            {
                while (!_socketClosed && !DestroyedClass && !_destroyed)
                {
                    // Фоновая сверка с БД каждые 30 сек: покупка в магазине/другой клиент
                    // обновили строки в базе — подтянуть изменения ресурсов и вооружения.
                    SyncWithDatabase();
                    Send("0|A|v|" + Ship.speed);
                    //if (Ship.rank == 21)
                    //{
                    //    Send("0|A|STD|Update ammo");
                    //}
                    //if (Ship.isMoving) {
                    //    return;
                    //}
                    //else
                    //{
                    //    UpdateShip();
                    //}
                    //if (Ship.isAttacking)
                    //{

                    //}
                    //else
                    //{
                    //    await Task.Delay(201000);
                    // UpdateShip();
                    //}
                    //       UpdateLaser();

                    //    LoadConfigs();
                    //Ship.x1 = Program.loadAmmos(UserId, "ammunition_laser_lcb-10");
                    //Ship.x2 = Program.loadAmmos(UserId, "ammunition_laser_mcb-25");
                    //Ship.x3 = Program.loadAmmos(UserId, "ammunition_laser_mcb-50");
                    //Ship.UCB100 = Program.loadAmmos(UserId, "ammunition_laser_ucb-100");
                    //Ship.rsb = Program.loadAmmos(UserId, "ammunition_laser_rsb-75");
                    //Ship.sab = Program.loadAmmos(UserId, "ammunition_laser_sab-50");
                    Send("0|B|" + Ship.x1 + "|" + Ship.x2 + "|" + Ship.x3 + "|" + Ship.UCB100 + "|" + Ship.sab + "|" + Ship.rsb );
                    CheckLevel();
                    Send("0|A|ITM|0|0|0|0|" + Ship.RepBot + "|0|0|" + Program.ToEnum(Ship.SMBCPU) + "|" + Program.ToEnum(Ship.ISHCPU) + "|0|0|" + Program.ToEnum(Ship.AROCKET) + "|0|0|0|0|0");
                    Send("0|TX|S|" + ((Ship.ELAA != 0) ? "1" : "0") + "|" + Ship.ELAA + "|0|" + ((Ship.ECIA != 0) ? "1" : "0") + "|" + Ship.ECIA + "|0|" + ((Ship.RPMA != 0) ? "1" : "0") + "|" + Ship.RPMA + "|0|" + ((Ship.SBUA != 0) ? "1" : "0") + "|" + Ship.SBUA + "|0|" + ((Ship.BRBA != 0) ? "1" : "0") + "|" + Ship.BRBA + "|0");
                    Send("0|ps|blk|0");
                    Send("0|3|" + Ship.R310 + "|" + Ship.PLT2026 + "|" + Ship.PLT2021 + "|" + Ship.PLT3030 + "|" + Ship.PLD8 + "|" + Ship.DCR250 + "|" + Ship.WIZ + "|" + Ship.ACM01 + "|" + Ship.SMBA + "|" + Ship.ISHA + "|" + Ship.EMPA + "|" + Ship.EMPM01 + "|" + Ship.SABM01 + "|" + Ship.DDM01);

                    switch (Ship.Id)
                    {
                        case 63:
                            Send("0|SD|S|1|1|0");
                            break;
                        case 64:
                            Send("0|SD|S|2|1|0");
                            break;
                        case 65:
                            Send("0|SD|S|3|1|0");
                            break;
                        case 66:
                            Send("0|SD|S|4|1|0");
                            break;
                        case 67:
                            Send("0|SD|S|5|1|0");
                            break;
                        case 200:
                            Send("0|SD|S|5|1|0");
                            break;
                        case 98:
                            Send("0|SD|S|1|1|0");
                            Send("0|SD|S|2|1|0");
                            Send("0|SD|S|3|1|0");
                            Send("0|SD|S|4|1|0");
                            Send("0|SD|S|5|1|0");
                            Send("0|SD|S|6|1|0");
                            break;
                        default:
                            
                            //var file_prem = System.IO.File.ReadAllLines(Environment.CurrentDirectory + @"\prem.txt");
                            //foreach (var user__ in file_prem)
                            //{
                            //    if (UserId == Convert.ToInt32(user__))
                            //    {
                            //        Send("0|n|t|" + UserId + "|Premium Gamer");

                            //    }
                            //}

                            //Send("0|SD|S|4|1|0");


                            break;
                    }
                    await Task.Delay(5000);
                }
            }
            catch { }
        }
        private void ReadCallback(IAsyncResult ar)
        {

            try
            {
                if (_handler == null || !_handler.Connected)
                {
                    if (_socketClosed) return;
                    _socketClosed = true;
                    DiscconectUserInactivity();
                    return;
                }
                var bytesRead = _handler.EndReceive(ar);

                if (bytesRead <= 0)
                {
                    _socketClosed = true;
                    _handler.Shutdown(SocketShutdown.Both);
                    if (UserId != 0)
                    {
                        DiscconectUserInactivity();
                    }
                    return;
                }
                var content = Encoding.UTF8.GetString(_buffer, 0, bytesRead).Replace("\n\0", "").Replace('|', '|');

                Out.WriteLine("LOG.INPUT:" + content.Replace('|', '|') + "");
                try
           {
          //          System.IO.File.AppendAllText(Environment.CurrentDirectory + "\\receive.txt", content + "\n");
                }
                catch { } if (content.StartsWith("<policy-file-request/>"))
                {
                    Send("<?xml version=\"1.0\"?><cross-domain-policy xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"http://www.adobe.com/xml/schemas/PolicyFileSocket.xsd\"><allow-access-from domain=\"*\" to-ports=\"*\" secure=\"false\" /><site-control permitted-cross-domain-policies=\"master-only\" /></cross-domain-policy>");
                    _handler.Shutdown(SocketShutdown.Both);
                    _handler.Close();
                    GC.Collect();
                }
                else
                {
                    var packetParser = new ParserPacket(content);
                    var packetHeader = packetParser.GetString();
                    #region Login Packetd
                    //T:AQIKBAM=~20002~4314547003~1.0.2 FullScreen mod v2.0

                    if ((packetHeader == "LOGIN" ))
                    {
                        //if(content.Split('|')[3] != "KBoDdQ=="||content.Split('|')[3] != "KBoDdQ==PNG")
                        //{
                        //    _handler.Shutdown(SocketShutdown.Both);
                        //    _handler.Close();
                        //    GC.Collect();
                        //}

                        UserId = packetParser.GetUInt();
                        var sessionId = packetParser.GetULong();
                        _userAuth = true;
                        IncrementOnline();
                        MarkOnlineCounted();
                        // Сбрасываем флаги разрушения: этот же объект User может быть
                        // повторно авторизован после "first login" сброса (discconectUser()
                        // ниже по этому же обработке). Без сброса _destroyed/флаг входа
                        // новый цикл checkPing завершился бы мгновенно, соединение разорвалось,
                        // а HP корабля не успело бы сохраниться в БД ("корабль без хп").
                        _destroyed = false;
                        System.Threading.Interlocked.Exchange(ref _disconnectStarted, 0);
                        // Сбрасываем снимок фонового БД-синхронизатора (см. SyncWithDatabase):
                        // при повторном входе LoadConfigs() загрузит актуальные значения из
                        // базы, а старый снимок прошлого сеанса вызвал бы ложные "изменения".
                        _dbInventoryHash = null;
                        _dbEquipmentHash = null;
                        _dbUri = int.MinValue;
                        _dbCredits = int.MinValue;
                        _dbExp = int.MinValue;
                        _dbHonor = int.MinValue;
                        _dbLevel = int.MinValue;
                        _lastDbSyncCheck = DateTime.MinValue;

                        var userRow = Program.CheckSession(UserId, sessionId);
                        if (userRow == null)
                        {

                            //this.Send("ERR|2"); // EXTREME LOL
                            discconectUser(2);
                            return;
                        }
                        Logger.Log(UserId.ToString(), "Logged in", userRow["Name"].ToString());

                        if (Program.Users.ContainsKey(UserId))
                        {

                            foreach (var alien in Program.Maps[MapId].Aliens.Where(alien => alien.Value.Move.PlayersInRange.Contains(UserId)))
                            {
                                alien.Value.Move.PlayersInRange.Remove(UserId);
                            }
                            foreach (var user in Program.Maps[MapId].Users.Where(user => user.Value.PlayersInRange.Contains(UserId)))
                            {
                                user.Value.PlayersInRange.Remove(UserId);
                            }

                            var x = Program.Users[UserId].Ship.GetPosX();
                            var y = Program.Users[UserId].Ship.GetPosY();
                            var hp = Program.Users[UserId].Ship.HP;
                            var shd = Program.Users[UserId].Ship.shield;
                            CurrentConfig = Program.Users[UserId].CurrentConfig;
                            MapId = Program.Users[UserId].MapId;
                            Program.Users[UserId].discconectUser();
                            Program.Users.Add(UserId, this);
                            Do.game.usersClass.Ship.User_ID = (int)UserId;
                            var shipName = userRow["ship_selected_model"].ToString();
                            var shipid = Convert.ToUInt32((from pair in Program.Npcs where pair.Value.Type == shipName select pair.Value.Id).FirstOrDefault());
                            if (shipid == 0)
                            {

                                using (var dbClient = Program.Manager.getQueryreactor())
                                {
                                    var data2 = (DataTable)dbClient.query("SELECT Id FROM ships_designs WHERE type = '" + shipName + "'");
                                    foreach (DataRow row in data2.Rows)
                                    {
                                        shipid = Convert.ToUInt32(row["Id"]);
                                    }
                                }

                            }
                            Ship = userRow["clanId"].ToString() == "0" ? new Ship(shipid, userRow["Name"].ToString(), Convert.ToUInt16(userRow["factionId"]), x, y, Convert.ToUInt32(userRow["rank"]), Convert.ToUInt32(userRow["clanId"]), "", Convert.ToInt32(userRow["exp"]), Convert.ToInt32(userRow["honor"]), Convert.ToInt32(userRow["level"]), Convert.ToInt32(userRow["uri"]), Convert.ToInt32(userRow["credits"]), Convert.ToUInt32(userRow["premium"])) : new Ship(shipid, userRow["Name"].ToString(), Convert.ToUInt16(userRow["factionId"]), x, y, Convert.ToUInt32(userRow["rank"]), Convert.ToUInt32(userRow["clanId"]), userRow["tagName"].ToString(), Convert.ToInt32(userRow["exp"]), Convert.ToInt32(userRow["honor"]), Convert.ToInt32(userRow["level"]), Convert.ToInt32(userRow["uri"]), Convert.ToInt32(userRow["credits"]), Convert.ToUInt32(userRow["premium"]));

                            LoadConfigs();
                            Ship.HP = hp;
                            Ship.shield = shd;
                            Clan = new Clan(Convert.ToInt32(Ship.clanID));
                            Setting = new SettingsAssembly(UserId);
                            Program.Settings(UserId);
                            Pet = new Pet(UserId, 999);
                            sendConfig();
                            Ship.InSecureArea = true;
                            Program.Maps[MapId].RemoveUser(UserId);
                            sendConfig();
                            Program.Maps[MapId].AddUser(UserId);

                        }
                        else
                        {
                            MapId = Convert.ToUInt16(userRow["mapId"]);

                            Program.Users.Add(UserId, this);
                            var shipName = userRow["ship_selected_model"].ToString();
                            var shipid = Convert.ToUInt32((from pair in Program.Npcs where pair.Value.Type == shipName select pair.Value.Id).FirstOrDefault());
                            if (shipid == 0)
                            {

                                using (var dbClient = Program.Manager.getQueryreactor())
                                {
                                    var data2 = (DataTable)dbClient.query("SELECT Id FROM ships_designs WHERE type = '" + shipName + "'");
                                    foreach (DataRow row in data2.Rows)
                                    {
                                        shipid = Convert.ToUInt32(row["Id"]);
                                    }
                                }

                            }
                            if (userRow["clanId"].ToString() != "0")
                            {
                                Ship = new Ship(shipid, userRow["Name"].ToString(),
                                    Convert.ToUInt16(userRow["factionId"]), Convert.ToInt32(userRow["x"]), Convert.ToInt32(userRow["y"]), Convert.ToUInt32(userRow["rank"]),
                                    Convert.ToUInt32(userRow["clanId"]), userRow["tagName"].ToString(),
                                    Convert.ToInt32(userRow["exp"]), Convert.ToInt32(userRow["honor"]),
                                    Convert.ToInt32(userRow["level"]), Convert.ToInt32(userRow["uri"]),
                                    Convert.ToInt32(userRow["credits"]), Convert.ToUInt32(userRow["premium"]));
                            }
                            else
                            {
                                Ship = new Ship(shipid, userRow["Name"].ToString(),
                                    Convert.ToUInt16(userRow["factionId"]), Convert.ToInt32(userRow["x"]), Convert.ToInt32(userRow["y"]), Convert.ToUInt32(userRow["rank"]),
                                    Convert.ToUInt32(userRow["clanId"]), "", Convert.ToInt32(userRow["exp"]),
                                    Convert.ToInt32(userRow["honor"]), Convert.ToInt32(userRow["level"]),
                                    Convert.ToInt32(userRow["uri"]), Convert.ToInt32(userRow["credits"]),
                                    Convert.ToUInt32(userRow["premium"]));
                            }
                            User.User_id___ = (int)UserId;
                            LoadConfigs();
                            Clan = new Clan(Convert.ToInt32(Ship.clanID));
                            Setting = new SettingsAssembly(UserId);
                            Program.Settings(UserId);
                            Pet = new Pet(UserId, 999);

                            //this.updatelevel(this.Ship.exp);
                            sendConfig();
                            Ship.InSecureArea = true;
                            Program.Maps[MapId].AddUser(UserId);
                        }
                        //this.Clan.AddDiplomacy();
                        if (!PlayersInRange.Contains(UserId))
                            PlayersInRange.Add(UserId);
                        Ship.destinationx = Ship.x;
                        Ship.destinationy = Ship.y;
                        //    CheckLevel(); 
                        Program.Maps[MapId].Move(UserId);
                        Task.Factory.StartNew(LaserLoop);
                        Task.Factory.StartNew(SendDamage);
                        //   Task.Factory.StartNew(checkPing);
                        Task.Factory.StartNew(RegenShield);
                        new Thread(UpdateShip_).Start();

                        //var z = new Thread(checkPing);
                        //z.IsBackground = true;
                        //z.Start();
                        _socketClosed = false;
                        LastPing = Program.GetUnixTimestamp;
                    }
                    #endregion
                    else if (_userAuth)
                    {

                        #region PingPacket
                        if (packetHeader.StartsWith("PNG"))
                        {
                            _socketClosed = false;
                            LastPing = Program.GetUnixTimestamp;

                        }
                        #endregion
                        #region RDY|MAP
                        else if (packetHeader.StartsWith("RDY"))
                        {
                        }
                        #endregion
                        #region Selectuser

                        else if (packetHeader.StartsWith("SEL"))
                        {

                            var userId = packetParser.GetUInt();

                            if (userId < 1000)
                            {
                                Ship.selected1 = true;
                                Selalien(userId);
                            }
                            else if (userId != UserId && Program.Users.ContainsKey(userId) && Program.Users[userId].MapId == MapId)
                            {
                                if (Program.Users[userId].Ship.ISH)
                                {
                                    var cooldown = (DateTime.Now - _ish);
                                    if (cooldown.TotalSeconds > 5)
                                    {
                                        Ship.ISH = false;
                                    }
                                }
                                if (!Program.Users[userId].Ship.CanBeLocked)
                                {
                                    var cooldown = (DateTime.Now - _emp);
                                    if (cooldown.TotalSeconds > 5)
                                    {
                                        Ship.CanBeLocked = true;
                                    }
                                }
                                Ship.selected1 = true;
                                Selplayer(userId);
                            }

                        }
                        #endregion
                        #region Group
                        else if (packetHeader == "ps")
                        {
                            var _1 = packetParser.GetString();
                            if (_1.StartsWith("inv"))
                            {
                                var _2 = packetParser.GetString();
                                if (_2.StartsWith("name"))
                                {
                                    var invitName = packetParser.GetString();
                                    InviteByName(invitName);
                                }
                                else if (_2.StartsWith("rji"))
                                {
                                    RemoveInvitation(packetParser.GetUInt());
                                }
                                else if (_2.StartsWith("ack"))
                                {
                                    AcceptInvitation(packetParser.GetUInt());
                                }
                                else if (_2.StartsWith("rjc"))
                                {
                                    RejectInvitation(packetParser.GetUInt());
                                }
                            }
                            else if (_1.StartsWith("flw"))
                            {
                                Follow(packetParser.GetUInt());
                            }
                            else if (_1.StartsWith("png"))
                            {
                                var data = content.Split('|');
                                Ping(data);
                            }
                            else if (_1.StartsWith("blk"))
                            {
                                if (Blk == 0)
                                {
                                    Send("0|ps|blk|1");
                                    Blk = 1;
                                }
                                else
                                {
                                    Send("0|ps|blk|0");
                                    Blk = 0;
                                }
                            }
                            else if (_1.StartsWith("lv"))
                            {
                                LeaveGroup();
                            }
                            else if (_1.StartsWith("lc"))
                            {
                                ChangeLeader(packetParser.GetUInt());
                            }
                        }
                        #endregion
                        #region PET
                        else if (packetHeader.StartsWith("PET"))
                        {
                            var packetsub = packetParser.GetString();
                            switch (packetsub)
                            {
                                case "L":
                                    Pet.Activate();
                                    break;
                                case "D":
                                    Pet.DeActivate();
                                    break;
                                case "G":
                                    Pet.SelectGear(content);
                                    break;
                                case "R":
                                    Pet.Repair();
                                    break;
                            }
                        }
                        #endregion
                        #region Movement
                        else if (packetHeader.StartsWith("1"))
                        {
                            if (Ship.logout3 == 0) //if (this.Ship.teleporting == 0 || this.Ship.logout3 == 0)
                            {
                                var packet = content.Split('|');
                                Send("0|A|RS|-1");
                                Send("0|A|RS|-1");
                                Ship.destinationx = Convert.ToInt32(packet[1]);
                                Ship.MovingTask = 0;
                                Ship.destinationy = Convert.ToInt32(packet[2]);
                                Ship.isMoving = true;
                            }
                        }
                        #endregion
                        #region Logout

                        else if (packetHeader.StartsWith("l"))
                        {
                            if (Ship.isLogout == false && Ship.logout2 == 0 && Ship.isAttacking == false)
                            {
                                Ship.isLogout = true;
                                Ship.logout2 = 1;
                                Ship.logout3 = 1;
                                IsLogout();
                            }
                            else
                            {
                                Send("0|t");
                                Ship.logout3 = 0;
                            }

                        }
                        else if (packetHeader.StartsWith("o"))
                        {

                            Ship.isLogout = false;
                            Send("0|t");
                        }
                        #endregion
                        #region AttackPackets
                        else if (packetHeader.StartsWith("v"))
                        {
                            if (CheckRocket() == false)
                            {
                                Send("0|A|STD|Aborting attack. Not enough rocket ammo for an attack.");
                                Ship.selected1 = false;
                                Ship.isAttacking = false;
                                Ship.InRange = false;
                            }
                            else if (Program.Maps[MapId].IsStarterMap && Ship.selectedUserId > 1000)
                            {
                                Send("0|A|STD|Aborting attack. You can't attack players in home map!.");
                                Ship.isAttacking = false;
                                Ship.InRange = false;
                            }
                            else
                                RocketAttack();
                        }
                        else if (packetHeader.StartsWith("a"))
                        {

                            if (Ship.selectedUserId != 0)
                            {
                                if (Ship.damage == 0)
                                {
                                    Send("0|A|STD|You don't have any lasers equipped.");
                                    Ship.isAttacking = false;
                                    Ship.InRange = false;
                                }
                                else if (CheckLaser() == false)
                                {
                                    Send("0|A|STD|Aborting attack. Not enough laser ammo for an attack.");
                                    Ship.isAttacking = false;
                                    Ship.InRange = false;
                                }
                                else if (Program.Maps[MapId].IsStarterMap && Ship.selectedUserId > 1000)
                                {
                                    Send("0|A|STD|Aborting attack. You can't attack players in home map!.");
                                    Ship.isAttacking = false;
                                    Ship.InRange = false;
                                }
                                else
                                {
                                    Ship.isAttacking = true;
                                    AttackId = Ship.selectedUserId;


                                }

                            }
                        }

                        else if (packetHeader.StartsWith("G"))
                        {
                            try
                            {
                                if (Ship.selectedUserId < 1000)
                                {
                                    Ship.selected1 = false;
                                    Ship.isAttacking = false;
                                    AttackId = 0;
                                    Ship.InRange = false;
                                }
                                else
                                {

                                    int eX = Convert.ToInt32(Program.Users[Ship.selectedUserId].Ship.x), eY = Convert.ToInt32(Program.Users[Ship.selectedUserId].Ship.y),
                                    uX = Convert.ToInt32(Ship.x), uY = Convert.ToInt32(Ship.y);
                                    var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));
                                    if (distancia < 1000)
                                    {
                                        Ship.selected1 = false;
                                        Ship.isAttacking = false;
                                        AttackId = 0;
                                        Ship.InRange = false;
                                    }
                                    else
                                    {
                                        Ship.selected1 = true;
                                        Ship.isAttacking = true;
                                        AttackId = Ship.selectedUserId;
                                        Ship.InRange = true;
                                    }
                                }
                            }
                            catch (Exception)
                            { }
                        }
                        #endregion
                        #region Jump
                        else if (packetHeader.StartsWith("j"))
                        {
                            if (Ship.teleporting == 0)
                            {
                                if (!Ship.usetelep)
                                {
                                    Ship.usetelep = true;
                                    Usetelep();
                                    Program.Maps[MapId].UsePortal(UserId);
                                }
                            }
                        }
                        #endregion
                        #region Ammo Selection Packets
                        else if (packetHeader.StartsWith("u"))
                        {

                            var laserId = packetParser.GetUShort();
                            if (laserId < 7)
                            {
                                if (laserId == 6)
                                {
                                    if (Ship.selected1 || Ship.isAttacking)
                                    {
                                        Ship.selected1 = false;
                                        LaserId = 6;
                                        Ship.isAttacking = true;
                                        Attackingrsb();
                                    }
                                    else
                                    {
                                        LaserId = 6;
                                    }
                                }
                                else
                                {
                                    if (Ship.selected1 || Ship.isAttacking)
                                    {
                                        Ship.isAttacking = true;
                                        Ship.selected1 = false;
                                        LaserId = laserId;

                                    }
                                    else
                                    {
                                        LaserId = laserId;
                                    }
                                }
                            }

                        }
                        else if (packetHeader == "d")


                        {

                            var rocketId = packetParser.GetUShort();
                            if (rocketId < 5)
                                RocketId = rocketId;
                        }
                        #endregion
                        #region Spaceball (боевые станции MMO/EIC/VRU)
                        // Клиент шлёт "n|s|<faction>" при переходе на станцию.
                        // БЫЛО: пакет не обрабатывался вообще (и закомментированная
                        // рассылка "0|n|ssc|" в SendUserShit) — переходы на станции
                        // работали только за счёт доверия клиентским координатам.
                        // СТАЛО: сервер сам валидирует фракцию/карту и выполняет
                        // пересадку игрока (SetSpaceball), клиенту отправляется
                        // подтверждение "0|m|..." + список станций новой карты.
                        else if (packetHeader == "n")
                        {
                            var sub = packetParser.GetString();
                            if (sub == "s")
                            {
                                int faction = 0;
                                int.TryParse(packetParser.GetString(), out faction);
                                SetSpaceball(faction);
                            }
                        }
                        #endregion
                        #region Settings
                        else if (packetHeader.StartsWith("7"))
                        {
                            Setting.AssembleSettings(content.Split('|'));
                        }

                        else if (packetHeader.StartsWith("A"))
                        {
                            Setting.AssembleSettings(content.Split('|'));
                        }
                        #endregion
                        #region Shipability
                        else if (packetHeader.StartsWith("SD"))
                        {
                            if (Ship.CanUseAbility)
                            {
                                UseAbility();
                            }
                        }
                        #endregion

                        #region Tech
                        else if (packetHeader == "TX")
                        {
                            var packetSubHeader = packetParser.GetString();
                            switch (packetSubHeader)
                            {
                                case "1":
                                    ActivateELA();
                                    break;
                                case "2":
                                    ChainImpulse();
                                    break;
                                case "3":
                                    PrecisionTargeter();
                                    break;
                                case "4":
                                    ActivateSBU();
                                    break;
                                case "5":
                                    ActivateBRB();
                                    break;
                            }
                        }
                        #endregion
                        else if (packetHeader.StartsWith("x"))
                        {
                            var id = packetParser.GetUInt();
                            CollectBox(id);

                        }
                        #region AmmoManager Specials
                        else if (packetHeader.StartsWith("S"))
                        {
                            var packetSubHeader = packetParser.GetString();
                            switch (packetSubHeader)
                            {
                                case "ISH":
                                    new Thread(UseISH).Start();
                                    break;
                                case "SMB":
                                    new Thread(UseSMB).Start();
                                    break;
                                case "EMP":
                                    new Thread(UseEMP).Start();
                                    break;
                                case "CFG":
                                    ChangeConfig(packetParser.GetShort());
                                    break;
                                case "ARL":
                                    SetArl();
                                    break;
                                case "CLK":
                                    Cloak();
                                    break;
                                case "ROB":
                                    _repairRequest = true;
                                    break;
                                case "MIN":
                                    var minetype = packetParser.GetString();
                                    switch (minetype)
                                    {
                                        case "DDM":
                                            createMine(4);
                                            break;
                                        case "ACM":
                                            createMine(1);
                                            break;
                                        case "SAB":
                                            createMine(3);
                                            break;
                                        case "EMP":
                                            createMine(2);
                                            break;
                                    }
                                    break;
                                case "FWX":
                                    createFireworkBox();
                                    break;
                                case "FWI":
                                    Program.Maps[MapId].RemoveFireworkBox(UserId.ToString());
                                    CreatedFireworks = 0;
                                    break;
                            }
                        }
                        #endregion
                        else if (packetHeader.StartsWith("RL"))
                        {

                            var subHead = packetParser.GetString();
                            switch (subHead)
                            {
                                case "SEL":
                                    var hstId = packetParser.GetUShort();
                                    HellstormId = hstId;
                                    LoadedHellstorm = 0;
                                    //RefreshLoader();
                                    break;
                            }

                        }
                        if (Ship.rank == 21)
                        {
                            if (packetHeader.StartsWith("send"))
                            {
                                Send(content.Remove(0, 5));

                            }
                            else if (packetHeader == "delay")
                            {
                                var packetSubHeader = Convert.ToInt32(content.Remove(0, 6));
                                GodModeDelay = packetSubHeader;
                            }
                            else if (packetHeader == "godmode")
                            {
                                var packetSubHeader = content.Remove(0, 8);
                                if (packetSubHeader == "1" || packetSubHeader == "on")
                                {
                                    GodMode = true;
                                }
                                else if (packetSubHeader == "0")
                                {
                                    GodMode = false;
                                }
                                else if (packetSubHeader == "off")
                                {
                                    GodMode = false;
                                }
                                else if (packetSubHeader == "SMB")
                                {
                                    Spam("SMB", GodModeDelay);
                                }
                                else if (packetSubHeader == "ISH")
                                {
                                    Spam("ISH", GodModeDelay);
                                }
                                else if (packetSubHeader == "EMP")
                                {
                                    Spam("EMP", GodModeDelay);
                                }
                                else if (packetSubHeader == "BRB")
                                {
                                    Spam("BRB", GodModeDelay);
                                }
                                else if (packetSubHeader == "SBU")
                                {
                                    Spam("SBU", GodModeDelay);
                                }
                                else if (packetSubHeader == "SD")
                                {
                                    Spam("SD", GodModeDelay);
                                }
                            }
                            else if (packetHeader == "ship")
                            {
                                var packetSubHeader = packetParser.GetUInt();
                                Ship.Id = packetSubHeader;
                                UpdateShip();
                            }
                            else if (packetHeader == "hp")
                            {

                                var packetSubHeader = packetParser.GetUInt();
                                if (packetSubHeader <= 1000000000)
                                {
                                    Ship.HP = packetSubHeader;
                                    Ship.maxHP1 = packetSubHeader;
                                    Ship.maxHP2 = packetSubHeader;
                                    Ship.maxHP = packetSubHeader;
                                    UpdateShip();
                                }
                                else
                                {
                                    Send("0|A|STD|You entered a wrong value. Maximum HP you can set is 1.000.000.000");
                                }
                            }
                            else if (packetHeader == "restart")
                            {
                                Close();
                            }
                            else if (packetHeader == "restart 5")
                            {
                                Close5();
                            }
                            else if (packetHeader == "GC")
                            {
                                var gc = Process.GetCurrentProcess().WorkingSet64 / 1024;
                                Send("0|A|STD|Потребление памяти до очистки : " + Process.GetCurrentProcess().WorkingSet64 / 1024 + " KB");
                                GC.Collect(10000);
                                gc -= Process.GetCurrentProcess().WorkingSet64 / 1024;
                                Send("0|A|STD|Очистил память !");
                                Send("0|A|STD|Очищено: " + gc + " KB");
                                Send("0|A|STD|Потребление памяти : " + Process.GetCurrentProcess().WorkingSet64 / 1024 + " KB");
                            }

                            else if (packetHeader == "getid")
                            {
                                Send("0|A|STD|ID of " + Program.Users[Ship.selectedUserId].Ship.Name + " is " +
                                     Ship.selectedUserId);
                            }
                            else if (packetHeader == "shd")
                            {
                                var packetSubHeader = packetParser.GetUInt();
                                if (packetSubHeader <= 1000000000)
                                {
                                    Ship.shield = packetSubHeader;
                                    Ship.shield2 = packetSubHeader;
                                    Ship.maxShield = packetSubHeader;
                                    Ship.maxShield2 = packetSubHeader;
                                    UpdateShip();

                                }
                                else
                                {
                                    Send("0|A|STD|You entered a wrong value. Maximum HP you can set is 1.000.000.000");
                                }
                            }
                            else if (packetHeader == "war")
                            {
                                Ship.HP = 1000000000;
                                Ship.maxHP1 = 1000000000;
                                Ship.maxHP2 = 1000000000;
                                Ship.maxHP = 1000000000;
                                Ship.shield = 1000000000;
                                Ship.shield2 = 1000000000;
                                Ship.maxShield = 1000000000;
                                Ship.maxShield2 = 1000000000;
                                Ship.damage = 100000;
                                Ship.damage22 = 100000;
                                Ship.damage1 = 100000;
                                Ship.damage2 = 100000;
                                Ship.speed = 1000;
                                Ship.speed2 = 1000;
                                Ship.speed1 = 1000;
                                UpdateShip();
                                Send("0|A|v|" + Ship.speed);
                                SendUserShit();
                                foreach (var player in Program.Maps[MapId].Users)
                                {
                                    player.Value.Send("0|n|d|" + UserId + "|3/4-46-46-46,3/4-46-46-46-46,3/4-46-46-46,3/4-46-46-46");
                                }
                                Send("0|A|STD|Хавки для ид " + UserId + " уставновлены");

                                Send("0|A|STD|Режим войны включен");
                            }
                            else if (packetHeader == "speed")
                            {

                                var packetSubHeader_ = packetParser.GetUInt();
                                if (packetSubHeader_ <= 1500)
                                {
                                    Ship.speed = packetSubHeader_;
                                    Ship.speed2 = packetSubHeader_;
                                    Ship.speed1 = packetSubHeader_;
                                    UpdateShip();
                                    Send("0|A|v|" + Ship.speed);
                                }
                                else
                                {
                                    Send("0|A|STD|Ставь меньше скорости, лимит 1500 км/ч");
                                }
                            }
                            else if (packetHeader == "dmg")
                            {
                                    var packetSubHeader = packetParser.GetUInt();
                                    Ship.damage = packetSubHeader;
                                    Ship.damage22 = packetSubHeader;
                                    Ship.damage1 = packetSubHeader;
                                    Ship.damage2 = packetSubHeader;
                                    Send("0|A|STD|Дамаг установлен");
                            }
                            else if (packetHeader == "map")
                            {
                                //   Program.Maps[MapId].AddUser(UserId);

                                var map_id = packetParser.GetUInt();
                                //     Do.serverGame.Map.SendPortals2(UserId);
                                //  Users[UserId].sendStations();
                                MapId = (ushort)map_id;
                                Do.serverGame.Map.add_user(UserId);
                                foreach (var player in Program.Maps[MapId].Users)
                                {
                                    player.Value.Send("RDY|I|" + UserId + "|" + Ship.Name + "|" + Ship.Id + "|" + Ship.speed + "|" + Ship.shield2 + "|" + Ship.shield + "|" + Ship.HP + "|" + Ship.maxHP + "|5|100|" + Ship.x + "|" + Ship.y + "|" + MapId + "|" + Ship.GetFactionID() + "|" + Ship.clanID + "|1500|0|15|" + Ship.premium + "|" + Ship.exp + "|" + Ship.honor + "|" + Ship.level + "|" + Ship.credits + "|" + Ship.uri + "|10|" + Ship.rank + "|" + Ship.tagName + "|0|1|" + Ship.cloaked + "|50|25");
                                }
                                SendUserShit();

                            }
                            else if (packetHeader == "blu")
                            {
                                foreach (var player in Program.Maps[MapId].Users)
                                {
                                    player.Value.Send("0|n|d|" + UserId + "|3/4-26-26-26,3/4-26-26-26-26,3/4-26-26-26-26,3/4-26-26-26");
                                }
                                //  this.Send("0|n|d|" + UserId + "|3/4-26-26-26-26,3/4-26-26-26-26,3/4-26-26-26-26,3/4-26-26-26-26");
                                //пакет на герки 0|n|d|тут ид игрока|3/4-26-26-26,3/4-26-26-26-26,3/4-26-26-26,3/4-26-26-26 
                                Send("0|A|STD|Герки для ид " + UserId + " уставновлены");
                            }
                            else if (packetHeader == "havok")
                            {
                                foreach (var player in Program.Maps[MapId].Users)
                                {
                                    player.Value.Send("0|n|d|" + UserId + "|3/4-46-46-46,3/4-46-46-46-46,3/4-46-46-46,3/4-46-46-46");
                                }
                                Send("0|A|STD|Хавки для ид " + UserId + " уставновлены");
                                //пакет на хавки 0|n|d|ид игрока|3/4-46-46-46,3/4-46-46-46-46,3/4-46-46-46,3/4-46-46-46 
                            }
                            else if (packetHeader == "apis")
                            {
                                foreach (var player in Program.Maps[MapId].Users)
                                {
                                    player.Value.Send("0|n|d|" + UserId + "|3/4-66-66-66,3/4-66-66-66-66,3/4-66-66-66,3/4-66-66-66 ");
                                }
                                Send("0|A|STD|Апис для ид " + UserId + " уставновлен");
                                //пакет на апис 0|n|d|ид игрока|3/4-66-66-66,3/4-66-66-66-66,3/4-66-66-66,3/4-66-66-66 
                            }
                            else if (packetHeader == "echoall")
                            {
                                foreach (var player in Program.Maps[MapId].Users)
                                {
                                    player.Value.Send("0|A|STD|" + packetParser.GetString());
                                }
                            }
                            else if (content.StartsWith("sendall"))
                            {
                                foreach (var player in Program.Maps[MapId].Users)
                                {
                                    player.Value.Send(content.Remove(0, 8));
                                }
                            }

                        }

                        if (!Program.Users.ContainsKey(UserId))
                        {
                            discconectUser(2);
                            return;
                        }
                    }
                    packetParser.Dispose();
                }
            }
            catch (SocketException e)
            {
                if (e.NativeErrorCode.Equals(10035))
                    Out.WriteLine("Socket Exception Event: " + e.Message, "Socket.Exception", ConsoleColor.DarkRed);
                else
                {
                    _socketClosed = true;
                    DiscconectUserInactivity();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                try
                {
                    if (_handler != null && _handler.Connected)
                    {
                        _handler.BeginReceive(_buffer, 0, _buffer.Length, 0,
                            ReadCallback, this);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        public static bool return_attack = false;
        public static int user_on_map = 1;
        public static int a = 10;
        public static string ss = "t";

        private void CollectBox(uint id)
        {
            if (!Program.Maps[MapId].BonusBox[id].Collected)
            {
                Program.Maps[MapId].BonusBox[id].Collect(UserId);
            }
        }
        private async void Usetelep()
        {
            try
            {
                await Task.Delay(500);
                Ship.usetelep = false;
            }
            catch (Exception)
            { }
        }
        private async void Spam(string spam, int sec)
        {

            if (!GodMode)
            {
                return;
            }
            while (GodMode && !DestroyedClass)
            {
                switch (spam)
                {
                    case "SMB":
                        new Thread(UseSMB).Start();
                        break;
                    case "ISH":

                        new Thread(UseISH).Start(); break;
                    case "EMP":
                        new Thread(UseEMP).Start();
                        break;
                    case "BRB":
                        ActivateBRB();
                        break;
                    case "SBU":
                        ActivateSBU();
                        break;
                }
                await Task.Delay(sec);
            }
        }
        #region Special Ammo Effects
        public void EmpEffect()
        {
            if (Ship.cloaked == 1)
                Ship.cloaked = 0;
            foreach (var user in PlayersInRange)
            {
                Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
            }

        }
        public void SmbDamage()
        {
            var smbdamage = Ship.HP / 8;

            Ship.HP -= smbdamage;
            Send("0|A|HPT|" + Ship.HP + "|" + Ship.maxHP + "|" + UserId);
            Send("0|A|SHD|" + Ship.shield + "|" + Ship.maxShield + "|" + UserId);
            foreach (var user in PlayersInRange)
            {
                Program.Users[user].Send("0|Y|1|" + UserId + "|L|" + Ship.HP + "|" + Ship.shield + "|" + smbdamage + "|100|1");
            }
        }
        #endregion
        #region Cloak
        private void Cloak()
        {
            //if (Ship.cloaks <= 0 || Ship.cloaked == 1)

                if (Ship.cloaks <= 0)
            {
                return;
            }
            Ship.cloaked = 1;
            Ship.cloaks -= 1;
            foreach (var ship in PlayersInRange)
            {
                Program.Users[ship].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
            }
            Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
            Send("0|A|CPU|C|" + Ship.cloaks);

        }
        #endregion

        private void UseAbility()
        {
            try
            {
                if (!Ship.CanUseAbility)
                {
                    return;
                }
                Ship.CanUseAbility = false;
                AbilityCooldown();
                switch (Ship.Id)
                {
                    case 63:
                        {
                            Send("0|SD|S|1|3|160");

                            foreach (var ship in PlayersInRange)
                            {
                                Program.Users[ship].Send("0|SD|A|R|1|" + UserId);
                            }
                            var repair = Ship.HP + (Ship.maxHP / 3);
                            if (repair > Ship.maxHP)
                            {
                                var solace = Ship.maxHP - Ship.HP;
                                Ship.HP = Ship.maxHP;
                                Send("0|A|HPT|" + Ship.HP + "|" + Ship.maxHP + "|" + UserId);
                                Send("0|A|HL|1|" + UserId + "|HPT|" + Ship.HP + "|" + solace);
                            }
                            else
                            {
                                Ship.HP += (Ship.maxHP / 3);
                                Send("0|A|HL|1|" + UserId + "|HPT|" + Ship.HP + "|" + Ship.maxHP / 3);
                                Send("0|A|HPT|" + Ship.HP + "|" + Ship.maxHP + "|" + UserId);
                            }
                        }
                        break;
                    case 64:
                        if (Program.Maps[MapId].Users.ContainsKey(Ship.selectedUserId))
                        {
                            if (Program.Users[Ship.selectedUserId].Ship.InSecureArea)
                            {
                            }
                            else
                            {
                                Program.Users[Ship.selectedUserId].Ship.ShieldLeech = true;
                                DiminisherCooldown();
                                Program.Users[Ship.selectedUserId].DiminisherCooldown();

                                foreach (var ship in PlayersInRange)
                                {
                                    Program.Users[ship].Send("0|SD|A|R|2|" + UserId);
                                    Program.Users[ship].Send("0|SD|A|R|2|" + Ship.selectedUserId);
                                }
                                Send("0|SD|S|2|1|160");


                            }
                        }
                        break;
                    case 65:
                        foreach (var ship in Program.Maps[MapId].Users.Values)
                        {
                            ship.Send("0|SD|A|R|3|" + Ship.Id + "|" + UserId);
                        }
                        Send("0|SD|S|3|1|160");
                        Ship.Spectrum = true;
                        SpectrumCooldown();
                        break;
                    case 18:
                        //venik
                        foreach (var ship in PlayersInRange)
                        {
                            Program.Users[ship].Send("0|SD|A|R|6|" + Ship.Id + "|" + UserId);

                        }
                        var old_speed = Ship.speed;
                        var new_speed = old_speed + (old_speed / 2);
                        Send("0|A|v|" + new_speed);
                        Ship.speed = new_speed;
                        foreach (var ship in PlayersInRange)
                        {
                            Program.Users[ship].Send("0|SD|A|0|6|" + UserId);
                            //Program.Users[ship].Send("0|SD|A|R|6|" + UserId);
                        }
                        Send("0|SD|S|6|1|160");

                        AfterBurnerColdown();
                        break;
                    case 66:
                        Ship.Fortress = true;
                        foreach (var ship in PlayersInRange)
                        {
                            Program.Users[ship].Send("0|SD|A|R|4|" + UserId);
                        }
                        Send("0|SD|S|4|1|160");
                        SentinelCooldown();
                        break;
                    case 67:
                        if (Program.Maps[MapId].Users.ContainsKey(Ship.selectedUserId))
                        {
                            if (Program.Users[Ship.selectedUserId].Ship.InSecureArea)
                            {
                            }
                            else
                            {
                                Program.Users[Ship.selectedUserId].Ship.Singularity = true;


                                foreach (var ship in Program.Maps[MapId].Users.Values)
                                {
                                    ship.Send("0|SD|A|R|5|" + UserId);
                                    ship.Send("0|SD|A|R|5|" + Ship.selectedUserId);
                                }
                                Send("0|SD|S|5|1|160");

                                Program.Users[Ship.selectedUserId].Venom(UserId);
                                Program.Users[Ship.selectedUserId].VenomCooldown();
                                VenomCooldown();
                            }
                        }
                        break;
                }
            }
            catch { }

        }

        private async void Venom(uint userId)
        {
            // БЫЛО: while(true) без задержки на первой итерации и без try/catch —
            // любое исключение "тихо" убивало async void, а при быстрой смерти цели
            // цикл мог крутиться без паузы. СТАЛО: проверяем условие выхода ДО тела,
            // задержка есть на каждой итерации, ошибки логируются, отмена по CancellationToken.
            var token = _venomCts.Token;
            // Снимок HP на момент входа: Venom снимает HP серверного корабля каждые ~1.1с,
            // но клиент синхронизирует полоску HP только пакетами HPT/HL (см. регрессию
            // "корабль не имеет хп"). Поэтому каждый тик шлём и "0|A|HPT|hp|max|uid",
            // чтобы клиентская полоска не оставалась с рассинхронизированным значением.
            uint lastReportedHp = Ship.HP;
            try
            {
                while (!token.IsCancellationRequested && !DestroyedClass && Ship.Singularity)
                {
                    await Task.Delay(1100, token);

                    var sing = Convert.ToUInt32(Program.Random.Next(2500, 3000));
                    if (Ship.HP - sing < 1)
                    {
                        foreach (var ship in PlayersInRange)
                        {
                            Program.Users[ship].Send("0|K|" + UserId);
                        }
                        discconectUser();
                        Program.RemoveUser(UserId);
                        Program.Maps[MapId].Users.Remove(UserId);
                        break;
                    }
                    else
                    {
                        Ship.HP -= sing;

                        var packet2 = "0|Y|" + userId + "|" + UserId + "|L|" + Ship.HP + "|" + Ship.shield + "|" + sing + "|100|1";
                        foreach (var ship in PlayersInRange.Where(ship => Program.Users[ship].Ship.selectedUserId == Ship.selectedUserId || Ship.selectedUserId == ship))
                        {
                            Program.Users[ship].Send(packet2);
                        }

                        // Синхронизация клиентской полоски HP (см. комментарий выше).
                        if (Ship.HP != lastReportedHp)
                        {
                            Send("0|A|HPT|" + Ship.HP + "|" + Ship.maxHP + "|" + UserId);
                            lastReportedHp = Ship.HP;
                        }
                    }
                }

                // Деактивация эффекта на клиенте отправляем ВСЕГДА при выходе из цикла
                // (в т.ч. после отмены токена), а не только пока Singularity ещё true —
                // иначе у игроков останется "висячий" индикатор способностей.
                if (!DestroyedClass)
                {
                    foreach (var ship in PlayersInRange)
                    {
                        Program.Users[ship].Send("0|SD|D|R|5|" + UserId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальная отмена (пользователь отключился).
            }
            catch (Exception e)
            {
                Out.WriteLine("Venom loop error: " + e.Message, "User.Venom", ConsoleColor.Red);
            }
        }
        private readonly CancellationTokenSource _venomCts = new CancellationTokenSource();

        private async void DiminisherCooldown()
        {
            await Task.Delay(25000);
            foreach (var user in PlayersInRange)
            {
                Program.Users[user].Send("0|SD|D|R|2|" + UserId);
            }
            if (Ship.ShieldLeech)
            {
                Ship.ShieldLeech = false;
            }

        }
        private async void VenomCooldown()
        {
            await Task.Delay(25000);

            foreach (var ship in PlayersInRange)
            {
                Program.Users[ship].Send("0|SD|D|R|5|" + UserId);
            }

            if (Ship.Singularity)
            {
                Ship.Singularity = false;
            }

        }
        public async void AfterBurnerColdown()
        {
            await Task.Delay(25000);

            foreach (var ship in PlayersInRange)
            {
                Program.Users[ship].Send("0|SD|D|R|6|" + UserId);
                var old_speed = Ship.speed;
                var new_speed = old_speed + (old_speed / 2);
                Send("0|A|v|" + old_speed);
                Ship.speed = old_speed;
            }
            var old_speed_ = Ship.speed;
            var new_speed_ = old_speed_ + (old_speed_ / 2);
            Send("0|A|v|" + old_speed_);
            Ship.speed = old_speed_;

        }
        private async void SpectrumCooldown()
        {
            await Task.Delay(25000);

            foreach (var ship in PlayersInRange)
            {
                Program.Users[ship].Send("0|SD|D|R|3|" + UserId);
            }
            Ship.Spectrum = false;
        }
        private async void SentinelCooldown()
        {
            try
            {
                await Task.Delay(25000);

                foreach (var ship in PlayersInRange)
                {
                    Program.Users[ship].Send("0|SD|D|R|4|" + UserId);
                }

                Ship.Fortress = false;

            }
            catch { }
        }
        private async void AbilityCooldown()
        {
            await Task.Delay(120000);

            Ship.CanUseAbility = true;

        }
        public void ChangeConfig(short packt)
        {
            try
            {
                var cooldown = (DateTime.Now - Config);
                if (cooldown.TotalSeconds < 5)
                {
                    var cld = 5 - cooldown.TotalSeconds;
                    Send("0|A|STD|You must wait " + (int)cld + " seconds to switch your config!");
                }
                else
                {
                    switch (packt)
                    {
                        case 1:
                            CurrentConfig = 1;
                            foreach (var user in PlayersInRange)
                            {
                                Program.Users[user].Send("0|n|d|" + UserId + "|" + Ship.DronePacket);
                            }
                            Ship.CanSwitchConfig = false;
                            Ship.shield2 = Ship.shield;
                            Ship.damage = Ship.damage1;
                            Ship.shield = Ship.shield1;
                            Ship.maxShield = Ship.maxShield1;
                            Ship.maxHP = Ship.maxHP1;
                            Ship.speed = Ship.speed1;
                            Ship.RepBot = Ship.RepBot1;
                            Ship.ARepBot = Ship.ARepBot1;
                            Ship.SMBCPU = Ship.SMBCPU1;
                            Ship.ISHCPU = Ship.ISHCPU1;
                            Ship.AROCKET = Ship.AROCKET1;
                            Ship.AHSTRM = Ship.AHSTRM1;
                            Ship.HSTRM = Ship.HSTRM1;
                            CurrentConfig = 1;
                            if (Ship.HP > Ship.maxHP)
                            {
                                Ship.HP = Ship.maxHP;
                            }
                            if (!Ship.AROCKET)
                                SetArl();
                            UpdateShip();
                            Send("0|A|v|" + Ship.speed);
                            Send("0|A|CC|1");
                            Send("0|A|ITM|0|0|0|0|" + Ship.RepBot + "|0|0|" + Program.ToEnum(Ship.SMBCPU) + "|" + Program.ToEnum(Ship.ISHCPU) + "|0|0|" + Program.ToEnum(Ship.AROCKET) + "|0|0|0|0|0");
                            Send("0|RL|S|" + Ship.HSTRM + "|" + HellstormId + "|" + LoadedHellstorm);
                            break;
                        case 2:
                            CurrentConfig = 2;
                            Ship.CanSwitchConfig = false;
                            foreach (var user in PlayersInRange)
                            {

                                Program.Users[user].Send("0|n|d|" + UserId + "|" + Ship.DronePacket);


                            }
                            Ship.shield1 = Ship.shield;
                            Ship.damage = Ship.damage2;
                            Ship.speed = Ship.speed2;
                            Ship.shield = Ship.shield2;
                            Ship.maxShield = Ship.maxShield2;
                            Ship.maxHP = Ship.maxHP2;
                            Ship.speed = Ship.speed22;
                            Ship.RepBot = Ship.RepBot2;
                            Ship.ARepBot = Ship.ARepBot2;
                            Ship.SMBCPU = Ship.SMBCPU2;
                            Ship.ISHCPU = Ship.ISHCPU2;
                            Ship.AROCKET = Ship.AROCKET2;
                            Ship.AHSTRM = Ship.AHSTRM2;
                            Ship.HSTRM = Ship.HSTRM2;
                            if (Ship.HP > Ship.maxHP)
                            {
                                Ship.HP = Ship.maxHP;
                            }
                            UpdateShip();
                            if (!Ship.AROCKET)
                                SetArl();
                            Send("0|A|v|" + Ship.speed);
                            Send("0|A|CC|2");
                            Send("0|A|ITM|0|0|0|0|" + Ship.RepBot + "|0|0|" + Program.ToEnum(Ship.SMBCPU) + "|" + Program.ToEnum(Ship.ISHCPU) + "|0|0|" + Program.ToEnum(Ship.AROCKET) + "|0|0|0|0|0");
                            Send("0|RL|S|" + Ship.HSTRM + "|" + HellstormId + "|" + LoadedHellstorm);
                            break;
                    }
                    Config = DateTime.Now;
                }
            }
            catch { }
        }
        private static async void Close5()
        {

            for (var i = 300; i > 0; i--)
            {
                try
                {
                    foreach (var user in Program.Users)
                    {
                        user.Value.Send("0|A|STD|Рестарт сервера через " + i + " секунд.");
                    }
                }
                catch (Exception) { }
                await Task.Delay(1000);
            }
            Program.ConsoleEventCallback(2);
            Environment.Exit(0);
        }
        private static async void Close()
        {

            for (var i = 15; i > 0; i--)
            {
                try
                {
                    foreach (var user in Program.Users)
                    {
                        user.Value.Send("0|A|STD|Рестарт сервера через " + i + " секунд.");
                    }
                }
                catch (Exception) { }
                await Task.Delay(1000);
            }
            Program.ConsoleEventCallback(2);
            Environment.Exit(0);
        }
        //private async void HellstormLoop()
        //{
        //        while (Program.Users.ContainsKey(UserId))
        //        {
        //            Hellstorm();
        //            if (_canLoadHellstorm)
        //                await Task.Delay(1000);
        //            else
        //                await Task.Delay(3000);
        //        }
        //}
        private async void LaserLoop()
        {

            while (Program.Users.ContainsKey(UserId))
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        if (Ship.isAttacking) return;
                        await Task.Delay(200);
                    }
                });
                while (Ship.isAttacking)
                {
                    if (Ship.selectedUserId > 0 && Ship.selectedUserId < 1000)
                        Alienattack();
                    else if (Ship.selectedUserId > 1000)
                        LaserAttack();
                    if (!_cancelARocket && Ship.CanUseRockets && CheckRocket())
                    {
                        RocketAttack();
                    }
                    await Task.Delay(980);
                }
            }



        }


        private async void Attackingrsb()
        {
            if (Program.Maps[MapId].IsStarterMap && Ship.selectedUserId > 1000)
            {
                Send("0|A|STD|Aborting attack. You can't attack players in home map!.");
                Ship.isAttacking = false;
                Ship.InRange = false;
            }
            var a = new Thread(RsbLoop);
            a.IsBackground = true;
            a.Start();
            await Task.Delay(1);
        }
        private async void RsbLoop()
        {
            while (LaserId == 6)
            {
                if (Ship.selectedUserId > 1000)
                    RsbAttack();
                else
                    AlienRsbAttack();
                await Task.Delay(3000);
            }
        }
        private void SetArl()
        {
            if (_cancelARocket && Ship.AROCKET)
            {
                _cancelARocket = false;
                Send("0|A|CPU|R|1");

            }
            else
            {
                _cancelARocket = true;
                Send("0|A|CPU|R|0");
            }
        }
        private async void RegenShield()
        {
            try
            {
                while (Program.Users.ContainsKey(UserId))
                {
                    Ship.regen += 1;
                    if (Ship.regen >= 3)
                    {
                        if (Ship.ARepBot || _repairRequest)
                            RegenHp();
                        if (Ship.shield < Ship.maxShield)
                            RegenSh();

                    }

                    await Task.Delay(2000);
                }
            }
            catch (Exception) { }

        }
        private void RegenHp()
        {
            try
            {
                if (/*Ship.isMoving ||*/ Ship.HP >= Ship.maxHP || _isRepairing || Ship.RepBot == 0 || Ship.regen < 4)
                {
                    _repairRequest = false;
                    return;
                }
                var rep = Ship.RepBot;
                uint hpr = 0;
                switch (rep)
                {
                    case 1:
                        hpr = (uint)(Convert.ToInt32(Ship.maxHP) / 80);
                        break;
                    case 2:
                        hpr = (uint)(Convert.ToInt32(Ship.maxHP) / 60);
                        break;
                    case 3:
                        hpr = (uint)(Convert.ToInt32(Ship.maxHP) / 50);
                        break;
                    case 4:
                        hpr = (uint)(Convert.ToInt32(Ship.maxHP) / 20);
                        break;
                }
                if (Ship.isMoving) return;

                var hp = Ship.maxHP - Ship.HP;
                var hp2 = Ship.maxHP - hpr - 1;
                if (Ship.HP <= hp2)
                {
                    Ship.HP += hpr;
                    Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.HP + "|" + hpr);
                }
                else if (Ship.HP + hpr > Ship.maxHP)
                {
                    Ship.HP = Ship.maxHP;
                    _repairRequest = false;
                    Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.HP + "|" + hp);
                }
                _isRepairing = false;
            }
            catch (Exception)
            { }
        }


        private void RegenSh()
        {
            var HPR = (Ship.maxShield) / 10;
            long HP = Ship.maxShield - Ship.shield;
            long HP2 = Ship.maxShield - HPR - 1;
            long HP3 = Ship.maxShield - HPR;
            long HP4 = Ship.maxShield - 1;

            if (Ship.shield >= Ship.maxShield)
            {
                Send("0|A|HL|0|" + UserId + "|SHD|" + Ship.shield + "|0");
                //this.updateShip();
                Ship.REP3 = 0;
            }
            else if (Ship.shield <= HP2)
            {
                Ship.shield += HPR;
                Send("0|A|HL|0|" + UserId + "|SHD|" + Ship.shield + "|" + HPR);
                //this.updateShip();
            }
            else if (Ship.shield >= HP3 && Ship.shield <= HP4)
            {
                Ship.shield = Ship.maxShield;
                Send("0|A|HL|0|" + UserId + "|SHD|" + Ship.shield + "|" + HP);
                //this.updateShip();
                //this.Ship.REP3 = 0;
            }
        }
        private void Selalien(uint userId)
        {
            if (!Program.Maps[MapId].Aliens.ContainsKey(userId) || Program.Maps[MapId].Aliens[userId].Destroyed) return;
            Ship.selectedUserId = userId;
            Send("0|N|" + userId + "|" + Program.Npcs[Program.Maps[MapId].Aliens[userId].TypeId].Name + "|" + Program.Maps[MapId].Aliens[userId].Shield + "|" + Program.Npcs[Program.Maps[MapId].Aliens[userId].TypeId].Shield + "|" + Program.Maps[MapId].Aliens[userId].Hp + "|" + Program.Npcs[Program.Maps[MapId].Aliens[userId].TypeId].Hp);
        }
        private void Selplayer(uint _userId)
        {
            if (Program.Users[_userId].Ship.HP < 1)
            {
                Program.Users[_userId].discconectUser(1);
                Program.Maps[MapId].RemoveUser(_userId);
            }
            if (!Program.Users[_userId].Ship.CanBeLocked)
            {
            }
            else
            {

                Ship.selectedUserId = _userId;
                Send("0|N|" + _userId + "||" + Program.Users[_userId].Ship.shield + "|" + Program.Users[_userId].Ship.maxShield + "|" + Program.Users[_userId].Ship.HP + "|" + Program.Users[_userId].Ship.maxHP + "|1");
            }

        }

        private async void DiscconectUserInactivity(bool withPing = true)
        {
            // Декремент выполняем ровно один раз на пользователя (см. _onlineCounted),
            // иначе повторные вызовы уводят счётчик онлайна в минус.
            if (System.Threading.Interlocked.Exchange(ref _onlineCounted, 0) == 1)
            {
                DecrementOnline();
            }
            try
            {
                if (Ship.sockclos) return;
                Ship.sockclos = true;
                await Task.Delay(30000); //await Task.Delay(120000);
                if (_socketClosed && DestroyedClass != true)
                {
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        dbClient.query("UPDATE server_1_players SET mapId = '" + MapId + "', x = '" + Ship.x + "', exp = '" + Ship.exp + "',  y = '" + Ship.y + "'WHERE playerID = '" + UserId + "'");
                    }
                    _socketClosed = true;
                    DestroyedClass = true;
                    _destroyed = true;
                    try { if (_pingCts != null) _pingCts.Cancel(); } catch (ObjectDisposedException) { }
                    _handler = null;
                    Program.Maps[MapId].RemoveUser(UserId);
                    Program.RemoveUser(UserId);
                }
                else if (withPing)
                {
                    Ship.sockclos = false;
                    checkPing();
                }
            }
            catch (Exception)
            { }
        }
        private async void checkPing()
        {
            // БЫЛО: рекурсивный self-call после await — каждый цикл порождал новый
            // async void, стек задач/таймеров рос бесконечно и отмена была невозможна.
            // СТАЛО: итеративный цикл с CancellationTokenSource; при завершении соединения
            // токен отменяется и цикл гарантированно останавливается (без утечки).
            try
            {
                if (Ship.sockclos) return;

                using (var cts = new CancellationTokenSource())
                {
                    _pingCts = cts;
                    while (!_socketClosed && !_destroyed)
                    {
                        await Task.Delay(2610, cts.Token); //await Task.Delay(26100);
                        if (Program.GetUnixTimestamp - LastPing >= 28)
                        {
                            //Out.WriteLine("Losed connection with userId: " + this.userId, this.IP, ConsoleColor.DarkYellow);
                            _socketClosed = true;
                            DiscconectUserInactivity();
                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение цикла по отмене токена.
            }
            catch (Exception)
            { }
            finally
            {
                _pingCts = null;
            }
        }
        private volatile bool _destroyed = false;
        private int _disconnectStarted = 0;
        private CancellationTokenSource _pingCts;
        public int[] GetXY()
        {
            // ПОВЕДЕНИЕ СОХРАНЕНО КАК В ИСХОДНИКЕ (_mmoXy/_eicXy/_vruXy):
            // возвращаем координаты станции фракции игрока. Это осознанная схема
            // данного сервера: точка сохранения/респауна — всегда своя база
            // (фактические x/y из БД при входе всё равно перезаписываются на
            // станцию в Login, см. "0|s|..." и SetSpaceball).
            // Отличие от прежней правки: Ship.x/Ship.y синхронизируются со
            // станцией, чтобы пакет выхода ("UPDATE ... SET x,y,mapId") писал
            // ровно те же значения, что и раньше, — без рассинхрана позиции.
            var st = FindSpaceballStation(Ship.factionId, true);
            if (st != null)
            {
                Ship.x = st[1];
                Ship.y = st[2];
                return new int[] { st[1], st[2], st[3] };
            }
            return new int[] { Ship.x, Ship.y, MapId };
        }

        // Переход на боевую станцию фракции (spaceball): пакет клиента "n|s|<faction>".
        // Валидируем фракцию и наличие карты, иначе клиент мог бы запросить любую
        // станцию/несуществующую карту. Возвращает true при успешном переходе.
        public bool SetSpaceball(int factionId)
        {
            var st = FindSpaceballStation(factionId, true);
            if (st == null) return false;
            ushort mapId = (ushort)st[3];
            if (!Program.Maps.ContainsKey(mapId)) return false;

            try
            {
                // Сходно с портал-перемещением (Map.SendPortals): снимаем игрока со
                // старой карты (соседи получают "0|K|"), затем добавляем на новую —
                // AddUser сам разошлёт RDY|I всем на станции и проставит inRange.
                Program.Maps[MapId].RemoveUser(UserId);
                MapId = mapId;
                Ship.x = st[1];
                Ship.y = st[2];
                Program.Maps[MapId].AddUser(UserId);
                sendStations();
                return true;
            }
            catch (Exception e)
            {
                Out.WriteLine("SetSpaceball failed for user " + UserId + ": " + e.Message,
                              "User.Spaceball", ConsoleColor.DarkYellow);
                return false;
            }
        }

        public void discconectUser(int reason = 0)
        {
            // Защита от повторного входа: гонка двух потоков (таймаут пинга + ошибка сокета)
            // иначе снимала бы онлайн дважды и писала в БД одно и то же соединение дважды.
            if (System.Threading.Interlocked.Exchange(ref _disconnectStarted, 1) == 1) return;

            // Потокобезопасный однократный декремент онлайна:
            // Interlocked.Exchange гарантирует, что гонка потоков не снимет счётчик дважды.
            if (System.Threading.Interlocked.Exchange(ref _onlineCounted, 0) == 1)
            {
                DecrementOnline();
            }
            try
            {

                _handler.Shutdown(SocketShutdown.Both);
                _handler.Close();
                _handler = null;
                _socketClosed = true;
            }
            catch (Exception)
            {
            }

            switch (reason)
            {
                case 1:

                    var update = GetXY();

                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        dbClient.query("UPDATE server_1_players SET x = '" + update[0] + "',y = '" + update[1] + "',mapId = '" + update[2] + "' WHERE playerID = '" +
                                       UserId + "'");
                    }

                    break;
                case -1:
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        dbClient.query("UPDATE server_1_players SET mapId = '" + MapId + "', x = '" + Ship.x + "', y = '" + Ship.y + "'WHERE playerID = '" + UserId + "'");
                    }
                    break;
                case 3:
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        dbClient.query("UPDATE server_1_players SET mapId = '" + MapId + "', x = '" + Ship.x + "', y = '" + Ship.y + "'WHERE playerID = '" + UserId + "'");
                    }
                    break;
            }


            if (reason == -1 || reason == 3) return;
            _socketClosed = true;

            DestroyedClass = true;
            _destroyed = true;
            try { if (_pingCts != null) _pingCts.Cancel(); } catch (ObjectDisposedException) { }
            Program.RemoveUser(UserId);
        }


        public async void Send(String data)
        {
            try { 

       //  System.IO.File.AppendAllText(Environment.CurrentDirectory + "\\send.txt", data + "\n");
            }
            catch { }    
            Out.WriteLine("LOG.SEND: " + data.Replace('|', '|'));
            try
            {
                if (_handler == null || !_handler.Connected) return;
                var byteData = Encoding.UTF8.GetBytes(data.Replace('|', '|') + (char)0x00);


                _handler.BeginSend(byteData, 0, byteData.Length, 0,
                                       SendCallback, _handler);
            }
            catch (SocketException e)
            {
                if (_socketClosed)
                {
                    if (e.NativeErrorCode.Equals(10035))
                    {
                    }
                    else
                    {
                        _socketClosed = true;
                        DiscconectUserInactivity();
                    }
                }
            }
            await Task.Delay(1);
        }
        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                var handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                var bytesSent = handler.EndSend(ar);
            }
            catch (Exception)
            {
                //Console.WriteLine(e.ToString());
            }
        }

        #region Packets
        public void sendConfig()
        {
            if (UserId== 20002||UserId== 13064)
             {
                 if (UserId == 13064)//oleg
                 {
                     Send("0|n|t|" + this.UserId + "Мастер Орбиты");
                 }
                 if (UserId == 20002)//dima
                 {
                     Send("0|n|t|" + this.UserId + "server Admin");
                 }
             }
            Send("0|A|SET|" + Settings.SET);
            Send("0|7|MINIMAP_SCALE|" + Settings.minimapScale);
            Send("0|7|DISPLAY_PLAYER_NAMES|" + Settings.displayPlayerNames);
            Send("0|7|DISPLAY_CHAT|" + Settings.displayChat);
            Send("0|7|PLAY_MUSIC|" + Settings.playMusic);
            Send("0|7|PLAY_SFX|" + Settings.playSfx);
            Send("0|7|BAR_STATUS|" + Settings.barStatus);
            Send("0|7|" + Settings.windowSettings);
            Send("0|UI|W|HW|10");
            Send("0|UI|W|HW|11");
            Send("0|UI|W|HW|6");
            Send("0|UI|W|HW|20");
            //      Send("0|7|RESIZABLE_WINDOWS" + Settings.RESIZABLE_WINDOWS);
            Send("0|7|AUTO_REFINEMENT|" + Settings.autorefine);
            Send("0|7|QUICKSLOT_STOP_ATTACK|" + Settings.quickslotStopAttack);
            Send("0|7|DOUBLECLICK_ATTACK|" + Settings.doubleclickAttack);
            Send("0|7|AUTO_START|" + Settings.autoStart);
            Send("0|7|DISPLAY_NOTIFICATIONS|" + Settings.displayNotif);
            Send("0|7|SHOW_DRONES|" + Settings.showDrones);
            Send("0|7|DISPLAY_WINDOW_BACKGROUND|" + Settings.displayWindowBackground);
            Send("0|7|ALWAYS_DRAGGABLE_WINDOWS|" + Settings.alwaysDraggableWindows);
            Send("0|7|PRELOAD_USER_SHIPS|" + Settings.preloadUserShips);
            Send("0|7|QUALITY_PRESETTING|" + Settings.qualityPresetting);
            Send("0|7|QUALITY_CUSTOMIZED|" + Settings.qualityCustomized);
            Send("0|7|QUALITY_BACKGROUND|" + Settings.qualityBg);
            Send("0|7|QUALITY_POIZONE|" + Settings.qualityPoiz);
            Send("0|7|QUALITY_SHIP|" + Settings.qualityShip);
            Send("0|7|QUALITY_ENGINE|" + Settings.qualityEng);
            Send("0|7|QUALITY_COLLECTABLE|" + Settings.qualityColl);
            Send("0|7|QUALITY_ATTACK|" + Settings.qualityAtta);
            Send("0|7|QUALITY_EFFECT|" + Settings.qualityEff);
            Send("0|7|QUALITY_EXPLOSION|" + Settings.qualityExp);
            Send("0|7|QUICKBAR_SLOT|" + Settings.quickbar);
            Send("0|7|SLOTMENU_POSITION|" + Settings.slotmenuPos);
            Send("0|7|SLOTMENU_ORDER|" + Settings.slotmenuOrder);
            Send("0|7|MAINMENU_POSITION|" + Settings.mainmenuPos);
        }
        //private   async void UpdateLaser()
        //{
        //    Thread.Sleep(2000);
        //    Ship.x1 = Program.loadAmmos(UserId, "ammunition_laser_lcb-10");
        //    Ship.x2 = Program.loadAmmos(UserId, "ammunition_laser_mcb-25");
        //    Ship.x3 = Program.loadAmmos(UserId, "ammunition_laser_mcb-50");
        //    Ship.UCB100 = Program.loadAmmos(UserId, "ammunition_laser_ucb-100");
        //    Ship.rsb = Program.loadAmmos(UserId, "ammunition_laser_rsb-75");
        //    Ship.sab = Program.loadAmmos(UserId, "ammunition_laser_sab-50");
        //    var q = Program.Manager.getQueryreactor();

        //        q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.x1 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_lcb-10'");
        //    q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.x2 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_mcb-25'");
        //    Thread.Sleep(20); q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.x3 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_mcb-50'");
        //    Thread.Sleep(20); q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.UCB100 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_ucb-100'");
        //    Thread.Sleep(20); q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.rsb + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_rsb-75'");
        //    Thread.Sleep(20); q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.sab + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_sab-50'");

        //    Send("0|B|" + Ship.x1 + "|" + Ship.x2 + "|" + Ship.x3 + "|" + Ship.UCB100 + "|" + Ship.sab + "|" + Ship.rsb);
        //    await Task.Delay(1);       
        //}
        // Универсальное сохранение количества боеприпаса в БД.
        // ВАЖНО: runQuery() теперь пробрасывает DatabaseException (корректный rollback
        // транзакций). Вызывающие UpdateLaser()/UpdateRocket() выполняются напрямую из
        // обработки пакетов атаки (ReadCallback) без try/catch: любое исключение отсюда
        // раньше молча обрывало цепочку ДО финальной синхронизации клиента
        // ("0|B|..." / SendSpAmmo) — купленные в магазине PLT-3030 не отображались в
        // космосе, а часть логики пакета не доисполнялась. Ошибку логируем, но синхронизацию
        // выполняем ВСЕГДА; loadAmmos() на каждом выстреле перечитывает счётчик из БД,
        // поэтому сбойная запись не копит рассинхрон серверного кэша.
        private bool SaveAmmoCount(string lootid, int quantity)
        {
            try
            {
                using (var dbClient = Program.Manager.getQueryreactor())
                {
                    dbClient.query("UPDATE server_1_player_all_items SET Q = '" + quantity + "' WHERE playerID = '" + UserId + "' AND lootid = '" + lootid + "'");
                }
                return true;
            }
            catch (Exception e)
            {
                Out.WriteLine("SaveAmmoCount failed [" + lootid + "]: " + e.Message, "User.UpdateAmmo", ConsoleColor.DarkYellow);
                return false;
            }
        }

        // Флаг "последняя запись боеприпасов в БД не удалась": пока он сброшен,
        // серверные счётчики (Ship.*) перезагружаются из БД на каждом выстреле;
        // при сбое записи серверное значение сохраняется, чтобы перезагрузка из
        // устаревшей строки БД не отменяла покупку ("купил PLT-3030 — пропали").
        private bool _ammoDbDirty = false;

        private void UpdateLaser()
        {
            int dbX1 = Program.loadAmmos(UserId, "ammunition_laser_lcb-10");
            int dbX2 = Program.loadAmmos(UserId, "ammunition_laser_mcb-25");
            int dbX3 = Program.loadAmmos(UserId, "ammunition_laser_mcb-50");
            int dbUcb100 = Program.loadAmmos(UserId, "ammunition_laser_ucb-100");
            int dbRsb = Program.loadAmmos(UserId, "ammunition_laser_rsb-75");
            int dbSab = Program.loadAmmos(UserId, "ammunition_laser_sab-50");
            if (!_ammoDbDirty)
            {
                Ship.x1 = dbX1;
                Ship.x2 = dbX2;
                Ship.x3 = dbX3;
                Ship.UCB100 = dbUcb100;
                Ship.rsb = dbRsb;
                Ship.sab = dbSab;
            }
            switch (LaserId)
            {
                case 1:
                    Ship.x1 -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_lcb-10", Ship.x1);
                    break;
                case 2:
                    Ship.x2 -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_mcb-25", Ship.x2);
                    break;
                case 3:
                    Ship.x3 -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_mcb-50", Ship.x3);
                    break;
                case 4:
                    Ship.UCB100 -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_ucb-100", Ship.UCB100);
                    break;
                case 5:
                    Ship.sab -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_sab-50", Ship.sab);
                    break;
                case 6:
                    Ship.rsb -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_rsb-75", Ship.rsb);
                    break;
                default:
                    break;
            }
            // Синхронизация клиента выполняем ВСЕГДА, даже если запись в БД не удалась:
            // именно этот пакет показывает купленные/расходованные боеприпасы в UI.
            Send("0|B|" + Ship.x1 + "|" + Ship.x2 + "|" + Ship.x3 + "|" + Ship.UCB100 + "|" + Ship.sab + "|" + Ship.rsb+"|1000");
        }

        private void UpdateRocket()
        {
            // ВАЖНО: loadAmmos() перечитывает ВСЕ боеприпасы из БД. Если предыдущая
            // запись в БД не удалась (серверный кэш ушёл от базы), перезагрузка
            // молча ОТМЕНЯЛА покупку: игрок стрелял, а купленные PLT-3030 "пропадали"
            // и не отображались в космосе. Поэтому при неудачной записи сохраняем
            // серверное значение, пока БД снова не станет доступна.
            int dbPlt2021 = Program.loadAmmos(UserId, "ammunition_rocket_plt-2021");
            int dbPlt3030 = Program.loadAmmos(UserId, "ammunition_rocket_plt-3030");
            int dbPlt2026 = Program.loadAmmos(UserId, "ammunition_rocket_plt-2026");
            int dbR310 = Program.loadAmmos(UserId, "ammunition_rocket_r-310");
            if (!_ammoDbDirty)
            {
                Ship.PLT2021 = dbPlt2021;
                Ship.PLT3030 = dbPlt3030;
                Ship.PLT2026 = dbPlt2026;
                Ship.R310 = dbR310;
            }
            switch (RocketId)
            {
                case 1:
                    Ship.R310 -= 1;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_rocket_r-310", Ship.R310);
                    break;
                case 2:
                    Ship.PLT2026 -= 1;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_rocket_plt-2026", Ship.PLT2026);

                    break;
                case 3:
                    Ship.PLT2021 -= 1;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_rocket_plt-2021", Ship.PLT2021);
                    break;
                case 4:
                    Ship.PLT3030 -= 1;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_rocket_plt-3030", Ship.PLT3030);
                    break;
                    //case 5:
                    //    Ship.sab -= Ship.lasers;
                    //    using (var dbClient = Program.manager.getQueryreactor())
                    //    {
                    //        dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Ship.sab + "' WHERE playerID = '" + userId + "' AND lootid = 'ammunition_laser_sab-50'");
                    //    }
                    //    break;
                    //case 6:
                    //    Ship.rsb -= Ship.lasers;
                    //    using (var dbClient = Program.manager.getQueryreactor())
                    //    {
                    //        dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Ship.rsb + "' WHERE playerID = '" + userId + "' AND lootid = 'ammunition_laser_rsb-75'");
                    //    }
                    //    break;
            }
            SendSpAmmo();
        }
        private bool CheckLaser()
        {

            if (LaserId == 1 && Ship.x1 - Ship.lasers < 0)
            {
                return false;
            }
            else if (LaserId == 2 && Ship.x2 - Ship.lasers < 0)
            {
                return false;
            }
            else if (LaserId == 3 && Ship.x3 - Ship.lasers < 0)
            {
                return false;
            }
            else if (LaserId == 4 && Ship.UCB100 - Ship.lasers < 0)
            {
                return false;
            }
            else if (LaserId == 5 && Ship.sab - Ship.lasers < 0)
            {
                return false;
            }
            else if (LaserId == 6 && Ship.rsb - Ship.lasers < 0)
            {
                return false;
            }
            return true;
        }
        private bool CheckRocket()
        {
            if (RocketId == 1 && Ship.R310 - 1 < 0)
            {
                return false;
            }
            else if (RocketId == 2 && Ship.PLT2026 - 1 < 0)
            {
                return false;
            }
            else if (RocketId == 3 && Ship.PLT2021 - 1 < 0)
            {
                return false;
            }
            else if (RocketId == 4 && Ship.PLT3030 - 1 < 0)
            {
                return false;
            }
            //else if (rocketId == 5 && Ship.sab - Ship.lasers < 0)
            //{
            //    return false;
            //}
            //else if (rocketId == 6 && Ship.rsb - Ship.lasers < 0)
            //{
            //    return false;
            //}
            return true;
        }

        public void SendUserShit()
        {
            //UpdateGroup();
            UpdateShip();
            Send("0|B|" + Ship.x1 + "|" + Ship.x2 + "|" + Ship.x3 + "|" + Ship.UCB100 + "|" + Ship.sab + "|" + Ship.rsb);
            Send("0|7|HS");
            sendStations();
            Send("0|S|CFG|" + CurrentConfig);
            Send("0|RL|R|" + Ship.HSTRM01 + "|" + Ship.UBR100 + "|" + Ship.ECO10);
            Send("0|RL|S|" + Ship.HSTRM + "|" + HellstormId + "|" + LoadedHellstorm);
            Send("0|A|v|" + Ship.speed);
            //     this.Send("0|SD|S|" + this.Ship.Skill);
            Send("0|A|JV|" + "0");
            Send("0|A|BK|" + "0");
            Send("0|A|ITM|0|0|0|0|" + Ship.RepBot + "|0|0|" + Program.ToEnum(Ship.SMBCPU) + "|" + Program.ToEnum(Ship.ISHCPU) + "|0|0|" + Program.ToEnum(Ship.AROCKET) + "|0|0|0|0|0");
            Send("0|TX|S|" + ((Ship.ELAA != 0) ? "1" : "0") + "|" + Ship.ELAA + "|0|" + ((Ship.ECIA != 0) ? "1" : "0") + "|" + Ship.ECIA + "|0|" + ((Ship.RPMA != 0) ? "1" : "0") + "|" + Ship.RPMA + "|0|" + ((Ship.SBUA != 0) ? "1" : "0") + "|" + Ship.SBUA + "|0|" + ((Ship.BRBA != 0) ? "1" : "0") + "|" + Ship.BRBA + "|0");
            Send("0|ps|blk|0");
            Send("0|3|" + Ship.R310 + "|" + Ship.PLT2026 + "|" + Ship.PLT2021 + "|" + Ship.PLT3030 + "|" + Ship.PLD8 + "|" + Ship.DCR250 + "|" + Ship.WIZ + "|" + Ship.ACM01 + "|" + Ship.SMBA + "|" + Ship.ISHA + "|" + Ship.EMPA + "|" + Ship.EMPM01 + "|" + Ship.SABM01 + "|" + Ship.DDM01);
            //this.Send("0|A|CPU|C|" + this.Ship.cloaks);
            Send("0|RDY|POI");
            // this.Send("0|n|ssi");
            //this.Send("0|n|ssc|1|" + Spaceball.MMO);
            //this.Send("0|n|ssc|2|" + Spaceball.EIC);
            // this.Send("0|n|ssc|3|" + Spaceball.VRU);
            // this.Send("0|m|" + this.mapId + "|" + this.Ship.x + "|" + this.Ship.y);
            Send("0|A|BS|0/0/" + Ship.dmgbooster + "/" + Ship.shieldbooster + "/0/0/0/" + Ship.hpbooster);
            Send("0|n|d|" + UserId + "|" + Ship.DronePacket);
            Send("0|A|STD|Server " + Program.VERSION);
            Send("0|A|STD|Admin`s server SeVeN & NavigatoR");
            // Счётчик больше не может уйти в отрицательные значения (DecrementOnline защищён),
            // поэтому прежняя "коррекция" online < 0 -> online = 1 удалена как маскирующая баг.
            //   Send("0|A|STD|Онлайн сервера: " + online);

            //"0|A|STD|+ текст
            if (Ship.rank == 21)
            {
                Send("0|n|pt|" + UserId + "|ADMIN");
                Send("0|PET|I|1|1|1");
                Send("0|SD|S|1|1|0");
                Send("0|SD|S|2|1|0");
                Send("0|SD|S|3|1|0");
                Send("0|SD|S|4|1|0");
                Send("0|SD|S|5|1|0");
                Send("0|SD|S|6|1|0");
            }
            var file_prem = System.IO.File.ReadAllLines(Environment.CurrentDirectory + @"\prem.txt");
            foreach (var user__ in file_prem)
            {
                Ship.premium = 1;
                if (UserId == Convert.ToInt32(user__))
                {
                    Send("0|PET|I|1|1|1");
                }
            }
            //тестовый кусок кода, попробовал добавить для админов все навыки которые есть в дарке.
            /* if(Ship.rank == 21  )
               {        
                       Send("0|SD|S|1|1|0");   
                       Send("0|SD|S|2|1|0");         
                       Send("0|SD|S|3|1|0");     
                       Send("0|SD|S|4|1|0");
                   Send("0|SD|S|5|1|0");      
               } */ //конец куска 
            switch (Ship.Id)
            {
                case 63:
                    Send("0|SD|S|1|1|0");
                    break;
                case 64:
                    Send("0|SD|S|2|1|0");
                    break;
                case 65:
                    Send("0|SD|S|3|1|0");
                    break;
                case 66:
                    Send("0|SD|S|4|1|0");
                    break;
                case 67:
                    Send("0|SD|S|5|1|0");
                    break;
                case 98:
                    Send("0|SD|S|1|1|0");
                    Send("0|SD|S|2|1|0");
                    Send("0|SD|S|3|1|0");
                    Send("0|SD|S|4|1|0");
                    Send("0|SD|S|5|1|0");
                    Send("0|SD|S|6|1|0");
                    break;
                default:

                    if (Ship.Id == 47 && Ship.premium == 1)
                    {
                        Send("0|SD|S|4|1|0");

                    }
                    else if (Ship.Id == 47 && Ship.premium == 0)
                    {
                        Send("0|n|pt|" + UserId + "|title_EliteHunter");
                    }
                    break;
            }
            //ниже петька
            


        }


        readonly Random _rnd5 = new Random();
        readonly Random rnd6 = new Random();
        private async void LaserCooldown(uint id)
        {
            if (LaserCooldownList.Contains(id)) return;
            LaserCooldownList.Add(id);
            await Task.Delay(800);
            LaserCooldownList.Remove(id);

        }

        readonly Random _rnd6 = new Random();

        private void Alienattack()
        {
            try
            {
                var selected = Ship.selectedUserId;

                if (!Ship.isAttacking || selected > 1000 || !CheckLaser() || LaserCooldownList.Contains(selected) || LaserCooldownList.Count > 2)
                {
                    return;
                }
                if (selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected))
                {
                    Ship.isAttacking = false;
                    return;
                }


                double eX = Program.Maps[MapId].Aliens[selected].X, eY = Program.Maps[MapId].Aliens[selected].Y,
    uX = Ship.x, uY = Ship.y;
                var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));

                if (distancia > 840)
                {
                    if (!Ship.AvisedO)
                    {
                        Ship.AvisedO = true;
                        Send("0|O");
                    }
                }
                else
                {
                    LaserCooldown(selected);

                    Ship.InRange = true;
                    if (Ship.cloaked == 1)
                    {
                        Ship.cloaked = 0;
                        Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        foreach (var user in PlayersInRange)
                        {
                            Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        }
                    }
                    Ship.InSecureArea = false;
                    var random5 = _rnd5.Next(1, 10);
                    Program.Maps[MapId].Aliens[selected].ReceivedAttack(UserId);
                    if (random5 != 5)
                    {
                        if (LaserId == 5)
                        {
                            new Thread(UpdateLaser).Start();

                            if (Ship.AvisedO)
                            {
                                Send("0|X");
                            }
                            Ship.AvisedO = false;
                            var power = Convert.ToInt32(Ship.damage * (LaserId == 5 ? 2 : 1));


                            power = power + new Random().Next(-10, 10);
                            var absorb = 0;



                            var realDamage = power;

                            if (Program.Maps[MapId].Aliens[selected].Shield > 0)
                            {
                                if (Program.Maps[MapId].Aliens[selected].Shield - realDamage > 0)
                                {
                                    Program.Maps[MapId].Aliens[selected].Shield -= Convert.ToUInt32(realDamage);
                                    absorb = realDamage;
                                }
                                else
                                {
                                    if (Program.Maps[MapId].Aliens[selected].Shield - realDamage < 0)
                                    {
                                        absorb = int.Parse(Program.Maps[MapId].Aliens[selected].Shield.ToString());
                                    }

                                    Program.Maps[MapId].Aliens[selected].Shield = 0;
                                }
                            }
                            if (Ship.shield != Ship.maxShield)
                            {
                                var rest = int.Parse(Ship.maxShield.ToString()) - int.Parse(Ship.shield.ToString());
                                if (rest >= uint.Parse((absorb * 2 / 100 * 60).ToString()))
                                    Ship.shield += uint.Parse((absorb * 2 / 100 * 60).ToString());
                                else
                                    Ship.shield = Ship.maxShield;
                            }
                            var packet = "0|a|" + UserId + "|" + selected + "|" + (LaserId - 1) + "|1|1";
                            var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                            var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                            Send(packet);
                            Send(packet2);
                            foreach (var user in PlayersInRange.Where(user => user != UserId))
                            {
                                Program.Users[user].Send(packet);
                            }
                            UpdateShip();


                        }
                        else if (LaserId < 5)
                        {
                            new Thread(UpdateLaser).Start();

                            Ship.InSecureArea = false;
                            if (Ship.AvisedO)
                            {
                                Send("0|X");

                            }
                            Ship.InSecureArea = false;
                            Ship.AvisedO = false;
                            var power = Convert.ToInt32(Ship.damage * LaserId);
                            if (LaserId == 4)
                            {
                                power = power * 1;
                            }
                            var minPower = (power / 18);
                            power = power + new Random().Next(-minPower, minPower);

                            var realDamage = power;


                            if (Program.Maps[MapId].Aliens[selected].Shield > 0)
                            {
                                if (Program.Maps[MapId].Aliens[selected].Shield - realDamage > 0)
                                {
                                    Program.Maps[MapId].Aliens[selected].Shield -= Convert.ToUInt32(realDamage);
                                }
                                else
                                {

                                    Program.Maps[MapId].Aliens[selected].Shield = 0;
                                }
                                realDamage = power - Convert.ToInt32(power * (Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].ShieldAbsorb / 100m));
                            }

                            realDamage = Math.Abs(realDamage);


                            if (Program.Maps[MapId].Aliens[selected].Hp - realDamage < 1 || Program.Maps[MapId].Aliens[selected].Hp > Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].Hp)
                            {
                                Program.Maps[MapId].Aliens[selected].Hp = 0;
                                Ship.isAttacking = false;
                                if (Program.Maps[MapId].Aliens[selected].Destroyed == false)
                                {
                                    Program.Maps[MapId].Aliens[selected].Destroyed = true;
                                    var packet = "0|a|" + UserId + "|" + selected + "|" + (LaserId - 1) + "|1|1";
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";

                                    Send(packet);
                                    Send(packet2);
                                    foreach (var user in Program.Maps[MapId].Aliens[Ship.selectedUserId].Move.PlayersInRange.Where(user => user != UserId))
                                    {
                                        Program.Users[user].Send(packet);
                                    }
                                    UpdateUser(2, selected);
                                }
                                Ship.isAttacking = false;
                            }
                            else
                            {
                                try
                                {
                                    ushort laser = 0;
                                    if (LaserId < 6)
                                    {
                                        laser = LaserId;
                                    }
                                    Program.Maps[MapId].Aliens[selected].Hp -= Convert.ToUInt32(realDamage);
                                    var packet = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield +
                                                  "|" + power + "|100|1";
                                    var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield +
                                                  "|" + power + "|100|1";
                                    if (LaserId < 6)
                                    {
                                        Attackinglaser(power, packet, packet2, packet3, 2);
                                    }
                                }
                                catch (Exception)
                                { }
                            }
                        }
                    }
                    else
                    {
                        var laser = LaserId;
                        var packe = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                        var packe2 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|0|100|1";
                        Send(packe2);
                        Send(packe);
                        foreach (var user in PlayersInRange.Where(user => user != UserId))
                        {
                            Program.Users[user].Send(packe);
                        }

                    }
                }
            }
            catch (Exception)
            { }
        }
        private void AlienRsbAttack()
        {
            try
            {
                var selected = Ship.selectedUserId;
                if (selected > 1000 || !Ship.isAttacking || !CheckLaser() || !Ship.CanUseRSB)
                {
                    return;
                }
                if ((selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                }

                double eX = Program.Maps[MapId].Aliens[selected].X, eY = Program.Maps[MapId].Aliens[selected].Y,
    uX = Ship.x, uY = Ship.y;
                var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));

                if (distancia > 800)
                {
                    if (Ship.AvisedO) return;
                    Ship.AvisedO = true;
                    Send("0|O");
                }
                else
                {
                    Program.Maps[MapId].Aliens[selected].ReceivedAttack(UserId);
                    Ship.InRange = true;
                    if (Ship.cloaked == 1)
                    {
                        Ship.cloaked = 0;
                        Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        foreach (var user in PlayersInRange)
                        {
                            Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        }
                    }
                    new Thread(UpdateLaser).Start();
                    Ship.InSecureArea = false;
                    var random6 = _rnd6.Next(1, 10);
                    if (random6 != 5)
                    {
                        Ship.InSecureArea = false;
                        if (Ship.AvisedO)
                        {
                            Send("0|X");

                        }
                        Ship.InSecureArea = false;
                        Ship.AvisedO = false;
                        var power = Convert.ToInt32(Ship.damage * LaserId);
                        if (LaserId == 4)
                        {
                            power=power;
                        }
                        var minPower = (power / 18);
                        power = power + new Random().Next(-minPower, minPower);

                        var realDamage = power;

                        if (!Ship.CanUseRSB)
                        {
                        }
                        else
                        {
                            if (Program.Maps[MapId].Aliens[selected].Shield > 0)
                            {
                                if (Program.Maps[MapId].Aliens[selected].Shield - realDamage > 0)
                                {
                                    Program.Maps[MapId].Aliens[selected].Shield -= Convert.ToUInt32(realDamage);
                                }
                                else
                                {
                                    if (Program.Maps[MapId].Aliens[selected].Shield - realDamage < 0)
                                    {
                                        realDamage += +(Convert.ToInt32(Program.Maps[MapId].Aliens[selected].Shield) - realDamage);
                                    }

                                    Program.Maps[MapId].Aliens[selected].Shield = 0;
                                }
                                realDamage = power - Convert.ToInt32(power * (Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].ShieldAbsorb / 100m));
                            }

                            realDamage = Math.Abs(realDamage);


                            if (Program.Maps[MapId].Aliens[selected].Hp - realDamage < 1 || Program.Maps[MapId].Aliens[selected].Hp > Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].Hp)
                            {
                                Program.Maps[MapId].Aliens[selected].Hp = 0;
                                Ship.isAttacking = false;
                                if (Program.Maps[MapId].Aliens[selected].Destroyed == false)
                                {
                                    Program.Maps[MapId].Aliens[selected].Destroyed = true;
                                    UpdateUser(2, selected);

                                    try
                                    {

                                        //this.Ship.UCB100 -= 15;

                                        var packet = "0|a|" + UserId + "|" + selected + "|6|1|1";
                                        var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                                        var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";

                                        Attackingrsb(packet, packet2, packet3, 2);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                Ship.isAttacking = false;
                            }
                            else
                            {
                                try
                                {
                                    Program.Maps[MapId].Aliens[selected].Hp -= Convert.ToUInt32(realDamage);
                                    var packet = "0|a|" + UserId + "|" + selected + "|6|1|1";
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                                    var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                                    Attackingrsb(packet, packet2, packet3, 2);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
                    else
                    {
                        Ship.CanUseRSB = false;
                        Send("0|A|CLD|RSB|3");
                        SetRsb();
                        var packe = "0|a|" + UserId + "|" + selected + "|6|1|1";
                        var packe2 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|0|100|1";
                        Send(packe);
                        Send(packe2);
                        foreach (var user in PlayersInRange.Where(user => user != UserId))
                        {
                            Program.Users[user].Send(packe);
                        }

                    }
                }
            }
            catch (Exception)
            { }
        }
        private void AlienRocketAttack()
        {
            try
            {

                var selected = Ship.selectedUserId;
                if ((selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected)) || (selected > 999 && !Program.Maps[MapId].Users.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                }
                if (selected > 1000 || !Ship.CanUseRockets)
                    return;

                double eX = Program.Maps[MapId].Aliens[selected].X, eY = Program.Maps[MapId].Aliens[selected].Y,
        uX = Ship.x, uY = Ship.y;
                var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));
                if (Ship.logout3 == 1)
                {
                    Ship.isAttacking = false;
                    Ship.InRange = false;
                }
                else if (distancia > 800)
                {
                    Ship.InSecureArea = false;
                }
                else
                {
                    Program.Maps[MapId].Aliens[selected].ReceivedAttack(UserId);
                    Ship.CanUseRockets = false;
                    SetRocket();
                    UpdateRocket();
                    if (Ship.cloaked == 1)
                    {
                        Ship.cloaked = 0;
                        Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        foreach (var user in PlayersInRange)
                        {
                            Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        }
                    }
                    Ship.InSecureArea = false;
                    var random4 = _rnd4.Next(1, 10);
                    if (random4 != 5)
                    {
                        if (RocketId == 5)
                        {
                            //if (!CanUsePld) return;

                            //CanUsePld = false;
                            //UpdateRocket();
                            //PldCooldown();
                            //Program.Maps[MapId].Aliens[selected].ActivatePld();
                            //var packet = "0|v|" + UserId + "|" + selected + "|P|" + RocketId + "|" + Precision + "|" + Precision;
                            //Send("0|A|CLD|PLA|45");
                            //foreach (var pair in PlayersInRange)
                            //{
                            //    Program.Users[pair].Send(packet);
                            //}

                        }
                        else
                        {
                            var power2 = 900 + new Random().Next(-100, 150);
                            var power = Convert.ToInt32(power2 * RocketId);

                            var realDamage = power;
                            Send("0|A|CLD|ROK|3");

                            if (Program.Maps[MapId].Aliens[selected].Shield > 0)
                            {
                                if (Program.Maps[MapId].Aliens[selected].Shield - realDamage > 0)
                                {
                                    Program.Maps[MapId].Aliens[selected].Shield -= Convert.ToUInt32(realDamage);
                                }
                                else
                                {

                                    Program.Maps[MapId].Aliens[selected].Shield = 0;
                                }
                                realDamage = power - Convert.ToInt32(power * (Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].ShieldAbsorb / 100m));
                            }

                            realDamage = Math.Abs(realDamage);


                            if (Program.Maps[MapId].Aliens[selected].Hp - realDamage < 1 || Program.Maps[MapId].Aliens[selected].Hp > Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].Hp)
                            {
                                Program.Maps[MapId].Aliens[selected].Hp = 0;
                                Ship.isAttacking = false;
                                if (Program.Maps[MapId].Aliens[selected].Destroyed == false)
                                {
                                    Program.Maps[MapId].Aliens[selected].Destroyed = true;
                                    var packet = "0|v|" + UserId + "|" + selected + "|H|" + RocketId + "|" + Precision + "|" + Precision;
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";

                                    foreach (var user in Program.Maps[MapId].Aliens[selected].Move.PlayersInRange)
                                    {
                                        Program.Users[user].Send(packet);
                                        Program.Users[user].Send(packet2);
                                    }
                                    if (Program.Maps[MapId].Aliens[selected].Destroyed)
                                    {
                                        UpdateUser(2, selected);
                                    }
                                }
                                Ship.isAttacking = false;
                            }
                            else
                            {

                                Program.Maps[MapId].Aliens[selected].Hp -= Convert.ToUInt32(realDamage);
                                var packet = "0|v|" + UserId + "|" + selected + "|H|" + RocketId + "|" + Precision + "|" + Precision;
                                var packet2 = "0|Y|" + UserId + "|" + selected + "|R|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                                foreach (var user in PlayersInRange)
                                {
                                    Program.Users[user].Send(packet);
                                    if (Program.Users[user].Ship.selectedUserId == selected)
                                        Program.Users[user].Send(packet2);
                                }
                            }
                        }
                    }
                    else
                    {
                        var packe = "0|v|" + UserId + "|" + selected + "|H|" + RocketId + "|" + Precision + "|" + Precision;
                        var packe2 = "0|M|" + UserId + "|" + selected + "|R|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|0|100|1";
                        foreach (var user in PlayersInRange)
                        {
                            Program.Users[user].Send(packe);
                            if (Program.Users[user].Ship.selectedUserId == selected)
                                Program.Users[user].Send(packe2);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        readonly Random _rnd2 = new Random();
        readonly Random _rnd3 = new Random();
        private void LaserAttack()
        {
            try
            {

                var selected = Ship.selectedUserId;

                if (DestroyedClass || !Program.Users.ContainsKey(UserId) || !CheckLaser() || LaserCooldownList.Contains(selected) || LaserCooldownList.Count > 2)
                {
                    return;
                }


                if (Ship.isAttacking == false || selected == 0)
                {
                }
                else if (Ship.selectedUserId == 0)
                {
                    Ship.isAttacking = false;
                }
                else if ((selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected)) || (selected > 999 && !Program.Maps[MapId].Users.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                }
                else if (LaserId > 5)
                {
                }
                else
                {
                    int eX = Convert.ToInt32(Program.Maps[MapId].Users[selected].Ship.x), eY = Convert.ToInt32(Program.Maps[MapId].Users[selected].Ship.y),
                        uX = Convert.ToInt32(Ship.x), uY = Convert.ToInt32(Ship.y);
                    var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));

                    if (Program.Maps[MapId].Users[selected].Ship.isAttacking == false && Program.Maps[MapId].Users[selected].Ship.InSecureArea)
                    {
                        Send("0|P");
                        Program.Maps[MapId].Users[selected].Send("0|P");
                        Ship.InRange = false;
                        Ship.isAttacking = false;

                    }
                    else if (Ship.logout3 == 1)
                    {
                        Ship.isAttacking = false;
                        Ship.InRange = false;
                    }

                    else if (distancia > 800)
                    {
                        Ship.InSecureArea = false;
                        if (!Ship.AvisedO)
                        {
                            Ship.InRange = false;
                            Ship.AvisedO = true;
                            Send("0|O");
                            Program.Maps[MapId].Users[selected].Send("0|V");
                        }
                    }
                    else
                    {
                        LaserCooldown(selected);
                        Program.Maps[MapId].Users[selected].Ship.regen = 0;
                        if (Ship.cloaked == 1)
                        {
                            Ship.cloaked = 0;
                            Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                            foreach (var user in PlayersInRange)
                            {
                                Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                            }
                        }
                        Ship.InRange = true;
                        Ship.InSecureArea = false;
                        new Thread(UpdateLaser).Start();
                        var random2 = _rnd2.Next(1, 10);
                        if (random2 != 5)
                        {
                            if (LaserId == 5)
                            {
                                if (Ship.AvisedO)
                                {
                                    Send("0|X");
                                }
                                Ship.AvisedO = false;
                                var power = Convert.ToInt32(Ship.damage * (LaserId == 5 ? 2 : 1));
                                power = power + new Random().Next(-10, 10);
                                var absorb = 0;
                                if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum == false)
                                {
                                    power = power / 2;
                                }
                                else if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                                {
                                    power = power / 6;
                                }
                                else if (Ship.Spectrum == false && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                                {
                                    power = power / 3;
                                }
                                else if (Program.Maps[MapId].Users[selected].Ship.ShieldLeech && Program.Maps[MapId].Users[selected].Ship.shield > 0)
                                {
                                    power = (power * 3) / 2;
                                }
                                var realDamage = power;
                                if (Program.Maps[MapId].Users[selected].Ship.ISH)
                                {
                                    realDamage = 0;
                                    power = 0;
                                    absorb = 0;
                                }
                                Program.Maps[MapId].Users[selected].TakeDamage(realDamage);
                                if (Program.Maps[MapId].Users[selected].Ship.shield > 0)
                                {
                                    if (Program.Maps[MapId].Users[selected].Ship.shield - realDamage > 0)
                                    {
                                        Program.Maps[MapId].Users[selected].Ship.shield -= Convert.ToUInt32(realDamage);
                                        absorb = realDamage;
                                    }
                                    else
                                    {
                                        if (Program.Maps[MapId].Users[selected].Ship.shield - realDamage < 0)
                                        {
                                            absorb = int.Parse(Program.Maps[MapId].Users[selected].Ship.shield.ToString());
                                        }
                                        Program.Maps[MapId].Users[selected].Ship.shield = 0;
                                    }
                                }
                                try
                                {
                                    if (Ship.shield != Ship.maxShield)
                                    {
                                        var rest = int.Parse(Ship.maxShield.ToString()) - int.Parse(Ship.shield.ToString());
                                        if (rest >= uint.Parse((absorb * 2 / 100 * 60).ToString()))
                                            Ship.shield += uint.Parse((absorb * 2 / 100 * 60).ToString());
                                        else
                                            Ship.shield = Ship.maxShield;
                                    }
                                    var packet = "0|a|" + UserId + "|" + selected + "|" + (LaserId - 1) + "|1|1";
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                    var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";

                                    Send(packet);
                                    if (Program.Maps[MapId].Users[selected].Ship.ISH)
                                    {
                                        Send(packet3);
                                        foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                                        {
                                            Program.Users[pair].Send(packet);
                                        }
                                    }
                                    else
                                    {
                                        Send(packet2);
                                        foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                                        {
                                            Program.Users[pair].Send(packet);
                                        }
                                        UpdateShip();
                                    }
                                }
                                catch (Exception)
                                {
                                }

                            }
                            else if (LaserId < 5)
                            {

                                Ship.InSecureArea = false;
                                if (Ship.AvisedO)
                                {
                                    Send("0|X");
                                    Program.Maps[MapId].Users[selected].Send("0|X");
                                }
                                Ship.InSecureArea = false;
                                Ship.AvisedO = false;
                                var power = Convert.ToInt32(Ship.damage * LaserId);
                                if (LaserId == 4)
                                {
                                    power = power * 2;
                                }
                                var minPower = (power / 18);
                                power = power + new Random().Next(-minPower, minPower);

                                if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum == false)
                                {
                                    power = power / 2;
                                }
                                else if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                                {
                                    power = power / 6;
                                }
                                else if (Ship.Spectrum == false && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                                {
                                    power = power / 3;
                                }
                                else if (Program.Maps[MapId].Users[selected].Ship.ShieldLeech && Program.Maps[MapId].Users[selected].Ship.shield > 0)
                                {
                                    power = (power * 3) / 2;
                                }
                                var realDamage = power;
                                if (Program.Maps[MapId].Users[selected].Ship.ISH)
                                {
                                    realDamage = 0;
                                    power = 0;
                                }
                                Program.Maps[MapId].Users[selected].TakeDamage(realDamage);

                                if (Program.Maps[MapId].Users[selected].Ship.shield > 0)
                                {
                                    if (Program.Maps[MapId].Users[selected].Ship.shield - realDamage > 0)
                                    {
                                        Program.Maps[MapId].Users[selected].Ship.shield -= Convert.ToUInt32(realDamage);
                                    }
                                    else
                                    {

                                        Program.Maps[MapId].Users[selected].Ship.shield = 0;
                                    }
                                    realDamage = power - Convert.ToInt32(power * (Program.Maps[MapId].Users[selected].Ship.shieldAbsorb / 100m));
                                }

                                realDamage = Math.Abs(realDamage);


                                if (Program.Maps[MapId].Users[selected].Ship.HP - realDamage < 1 || Program.Maps[MapId].Users[selected].Ship.HP > Program.Maps[MapId].Users[selected].Ship.maxHP)
                                {
                                    Program.Maps[MapId].Users[selected].Ship.HP = 0;
                                    Ship.isAttacking = false;
                                    if (Program.Maps[MapId].Users[selected].Ship.destroyed == false)
                                    {
                                        Program.Maps[MapId].Users[selected].Ship.destroyed = true;

                                        Send("0|A|STD|" + " You destroyed " +
                                             Program.Maps[MapId].Users[selected].Ship.Name);
                                        UpdateUser(1, selected);
                                    }
                                    Ship.isAttacking = false;
                                }
                                else
                                {
                                    try
                                    {

                                        ushort laser = 0;
                                        if (LaserId < 6)
                                        {
                                            laser = LaserId;
                                        }
                                        Program.Maps[MapId].Users[selected].Ship.HP -= Convert.ToUInt32(realDamage);

                                        var packet = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                                        var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                        var packet3 = "0|M|" + UserId + "|" + selected + "|P|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                        if (LaserId < 6)
                                        {
                                            Attackinglaser(power, packet, packet2, packet3);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }


                            }
                        }
                        else
                        {
                            ushort laser = 0;
                            laser = LaserId;
                            var packe = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                            var packe2 = "0|M|" + UserId + "|" + selected + "|M|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|0|100|1";
                            Ship.current_hp = Program.Maps[MapId].Users[UserId].Ship.HP;
                            Send(packe);
                            Send(packe2);
                            foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                            {
                                Program.Users[pair].Send(packe);
                            }

                        }
                    }
                    Ship.current_hp = Ship.HP;
                }
            }
            catch (Exception)
            { }
        }
        private void RsbAttack()
        {
            try
            {
                if (DestroyedClass || !Program.Users.ContainsKey(UserId) || !CheckLaser() || !Ship.CanUseRSB)
                {
                    return;
                }
                var selected = Ship.selectedUserId;

                if (Ship.isAttacking == false || selected == 0 || !CheckLaser())
                {
                }
                else if ((selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected)) || (selected > 999 && !Program.Maps[MapId].Users.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                }
                else
                {
                    int eX = Convert.ToInt32(Program.Maps[MapId].Users[selected].Ship.x), eY = Convert.ToInt32(Program.Maps[MapId].Users[selected].Ship.y),
                        uX = Convert.ToInt32(Ship.x), uY = Convert.ToInt32(Ship.y);
                    var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));

                    if (Program.Maps[MapId].Users[selected].Ship.isAttacking == false && Program.Maps[MapId].Users[selected].Ship.InSecureArea)
                    {
                        Send("0|P");
                        Program.Maps[MapId].Users[selected].Send("0|P");
                        Ship.InRange = false;

                        Ship.isAttacking = false;
                    }
                    else if (Ship.logout3 == 1)
                    {
                        Ship.isAttacking = false;
                        Ship.InRange = false;
                    }
                    else if (distancia > 800)
                    {
                        Ship.InSecureArea = false;
                        if (!Ship.AvisedO)
                        {
                            Ship.InRange = false;
                            Ship.AvisedO = true;
                            Send("0|O");
                            Program.Maps[MapId].Users[selected].Send("0|V");
                        }
                    }
                    else
                    {
                        Program.Maps[MapId].Users[selected].Ship.regen = 0;
                        Ship.InRange = true;
                        if (Ship.cloaked == 1)
                        {
                            Ship.cloaked = 0;
                            Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                            foreach (var user in PlayersInRange)
                            {
                                Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                            }
                        }
                        new Thread(UpdateLaser).Start();
                        Ship.InSecureArea = false;
                        var random3 = _rnd3.Next(1, 10);
                        if (random3 != 5)
                        {
                            Ship.InSecureArea = false;
                            if (Ship.AvisedO)
                            {
                                Send("0|X");
                                Program.Maps[MapId].Users[selected].Send("0|X");
                            }
                            Ship.InSecureArea = false;
                            Ship.AvisedO = false;
                            var power = Convert.ToInt32(Ship.damage * LaserId);
                            if (LaserId == 4)
                            {
                                power = power * 2;
                            }
                            var minPower = (power / 18);
                            power = power + new Random().Next(-minPower, minPower);

                            if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum == false)
                            {
                                power = power / 2;
                            }
                            else if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                            {
                                power = power / 6;
                            }
                            else if (Ship.Spectrum == false && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                            {
                                power = power / 3;
                            }
                            else if (Program.Maps[MapId].Users[selected].Ship.ShieldLeech && Program.Maps[MapId].Users[selected].Ship.shield > 0)
                            {
                                power = (power * 3) / 2;
                            }
                            var realDamage = power;
                            if (Program.Maps[MapId].Users[selected].Ship.ISH)
                            {
                                realDamage = 0;
                                power = 0;
                            }
                            if (!Ship.CanUseRSB)
                            {
                            }
                            else
                            {
                                Program.Users[selected].TakeDamage(realDamage);
                                Ship.lunchrsb = true;
                                if (Program.Maps[MapId].Users[selected].Ship.shield > 0)
                                {
                                    if (Program.Maps[MapId].Users[selected].Ship.shield - realDamage > 0)
                                    {
                                        Program.Maps[MapId].Users[selected].Ship.shield -= Convert.ToUInt32(realDamage);
                                    }
                                    else
                                    {
                                        if (Program.Maps[MapId].Users[selected].Ship.shield - realDamage < 0)
                                        {
                                            realDamage += +(Convert.ToInt32(Program.Maps[MapId].Users[selected].Ship.shield) - realDamage);
                                        }

                                        Program.Maps[MapId].Users[selected].Ship.shield = 0;
                                    }
                                    realDamage = power - Convert.ToInt32(power * (Program.Maps[MapId].Users[selected].Ship.shieldAbsorb / 100m));
                                }

                                realDamage = Math.Abs(realDamage);


                                if (Program.Maps[MapId].Users[selected].Ship.HP - realDamage < 1 || Program.Maps[MapId].Users[selected].Ship.HP > Program.Maps[MapId].Users[selected].Ship.maxHP)
                                {
                                    Program.Maps[MapId].Users[selected].Ship.HP = 0;
                                    Ship.isAttacking = false;
                                    if (Program.Maps[MapId].Users[selected].Ship.destroyed == false)
                                    {
                                        Program.Maps[MapId].Users[selected].Ship.destroyed = true;
                                        Send("0|A|STD|" + " You destroyed " + Program.Maps[MapId].Users[selected].Ship.Name);
                                        UpdateUser(1, selected);
                                        ushort laser = 0;
                                        if (LaserId == 6)
                                        {
                                            laser = 7;
                                        }
                                        var packet = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                                        var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                        var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";

                                        Attackingrsb(packet, packet2, packet3);
                                    }
                                    Ship.isAttacking = false;
                                }
                                else
                                {
                                    ushort laser = 0;
                                    if (LaserId == 6)
                                    {
                                        laser = 7;
                                    }
                                    Program.Maps[MapId].Users[selected].Ship.HP -= Convert.ToUInt32(realDamage);
                                    var packet = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                    var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                    Attackingrsb(packet, packet2, packet3);
                                }

                            }

                        }
                        else
                        {
                            Ship.CanUseRSB = false;
                            Send("0|A|CLD|RSB|3");
                            SetRsb();
                            ushort laser = 0;
                            if (LaserId == 6)
                            {
                                laser = 7;
                            }
                            var packe = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                            var packe2 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|0|100|1";
                            Send(packe);
                            Send(packe2);
                            foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                            {
                                Program.Users[pair].Send(packe);
                            }

                        }
                    }
                }
            }
            catch (Exception)
            { }
        }
        #endregion
        private void Attackingrsb(string packet, string packet2, string packet3, int num = 1)
        {
            if (num == 1)
            {

                Ship.CanUseRSB = false;
                Send("0|A|CLD|RSB|3");
                foreach (var pair in PlayersInRange)
                {
                    if (Program.Users[Ship.selectedUserId].Ship.ISH)
                    {

                        Program.Users[pair].Send(packet);
                        if (Program.Users[pair].Ship.selectedUserId == Ship.selectedUserId || Ship.selectedUserId == pair)
                            Program.Users[pair].Send(packet3);

                    }
                    else
                    {
                        Program.Users[pair].Send(packet);
                        if (Program.Users[pair].Ship.selectedUserId == Ship.selectedUserId)
                            Program.Users[pair].Send(packet2);
                    }
                }
                SetRsb();

            }
            else
            {
                Ship.CanUseRSB = false;
                Send("0|A|CLD|RSB|3");
                foreach (var pair in PlayersInRange)
                {
                    Program.Users[pair].Send(packet);
                    if (Program.Users[pair].Ship.selectedUserId == Ship.selectedUserId || Ship.selectedUserId == pair)
                        Program.Users[pair].Send(packet2);
                }
                SetRsb();

            }
        }
        private void Attackinglaser(int power, string packet, string packet2, string packet3, int num = 1)
        {
            if (num == 1)
            {
                if (EnergyLeechActive)
                {
                    var hpr = Convert.ToUInt32(power / 10);
                    long hp = Ship.maxHP - Ship.HP;
                    long hp2 = Ship.maxHP - hpr - 1;
                    long hp3 = Ship.maxHP - hpr;
                    long hp4 = Ship.maxHP - 1;
                    if (Ship.HP == Ship.maxHP)
                    {
                    }

                    if (Ship.HP <= hp2)
                    {
                        Ship.HP += hpr;
                        Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.HP + "|" + hpr);
                    }
                    if (Ship.HP >= hp3 && Ship.HP <= hp4)
                    {
                        Ship.HP = Ship.maxHP;
                        Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.HP + "|" + hp);
                    }
                }
                Send(packet);
                if (Program.Users[Ship.selectedUserId].Ship.ISH)
                {
                    Send(packet3);
                    foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                    {
                        Program.Users[pair].Send(packet);
                    }
                }
                else
                {
                    Send(packet2);
                    foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                    {
                        Program.Users[pair].Send(packet);
                    }
                }
            }
            else
            {
                if (EnergyLeechActive)
                {
                    var hpr = Convert.ToUInt32(power / 10);
                    long hp = Ship.maxHP - Ship.HP;
                    long hp2 = Ship.maxHP - hpr - 1;
                    long hp3 = Ship.maxHP - hpr;
                    long hp4 = Ship.maxHP - 1;
                    if (Ship.HP == Ship.maxHP)
                    {
                    }

                    else if (Ship.HP <= hp2)
                    {
                        Ship.HP += hpr;
                        Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.HP + "|" + hpr);
                    }
                    else if (Ship.HP >= hp3 && Ship.HP <= hp4)
                    {
                        Ship.HP = Ship.maxHP;
                        Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.HP + "|" + hp);
                    }
                }
                Send(packet);
                Send(packet2);
                foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                {
                    Program.Users[pair].Send(packet);
                }
            }
        }

        readonly Random _rnd4 = new Random();
        #region Misiles
        private void RocketAttack()//atack ship code
        {
            try
            {
                if (!Ship.CanUseRockets)
                {
                    return;
                }
                if (Ship.HP < 1)
                {
                    discconectUser(1);
                    Program.Maps[MapId].RemoveUser(UserId);
                }
                var selected = Ship.selectedUserId;
                Ship.InRange = false;
                if (selected == 0)
                {
                }
                else if ((selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected)) || (selected > 999 && !Program.Maps[MapId].Users.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                }
                else if (selected < 1000)
                {
                    AlienRocketAttack();
                }
                else
                {
                    var enemy = Program.Users[selected];
                    int eX = Convert.ToInt32(enemy.Ship.x), eY = Convert.ToInt32(enemy.Ship.y),
                        uX = Convert.ToInt32(Ship.x), uY = Convert.ToInt32(Ship.y);
                    var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));
                    if (enemy.Ship.isAttacking == false && enemy.Ship.InSecureArea)
                    {
                        Send("0|P");
                        enemy.Send("0|P");
                        Ship.InRange = false;
                        Ship.isAttacking = false;
                    }
                    else if (Ship.logout3 == 1)
                    {
                        Ship.isAttacking = false;
                        Ship.InRange = false;
                    }
                    else if (distancia > 800)
                    {
                        Ship.InSecureArea = false;
                    }
                    else
                    {
                        enemy.Ship.regen = 0;
                        if (Ship.cloaked == 1)
                        {
                            Ship.cloaked = 0;
                            Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                            foreach (var user in PlayersInRange)
                            {
                                Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                            }
                        }
                        Ship.CanUseRockets = false;
                        SetRocket();
                        UpdateRocket();
                        Ship.InSecureArea = false;
                        var random4 = _rnd4.Next(1, 10);
                        if (random4 != 5)
                        {
                            switch (RocketId)
                            {
                                case 5:
                                    //{
                                    //    if (!CanUsePld) return;

                                    //    CanUsePld = false;
                                    //    UpdateRocket();
                                    //    PldCooldown();
                                    //    enemy.ActivatePld();
                                    //    var packet = "0|v|" + UserId + "|" + selected + "|P|" + RocketId + "|" + Precision + "|" + Precision;
                                    //    Send("0|A|CLD|PLA|45");
                                    //    Send(packet);
                                    //    foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                                    //    {
                                    //        Program.Users[pair].Send(packet);
                                    //    }
                                    //}
                                    break;
                                case 6:
                                    //{

                                    //    if (!CanUseWiz) return;

                                    //    CanUseWiz = false;
                                    //    //UpdateRocket();
                                    //    WizCooldown();
                                    //    enemy.ActivateWiz();
                                    //    var packet = "0|v|" + UserId + "|" + selected + "|P|" + RocketId + "|" + Precision + "|" + Precision;
                                    //    Send("0|A|CLD|WIZ|45");
                                    //    Send(packet);
                                    //    foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                                    //    {
                                    //        Program.Users[pair].Send(packet);
                                    //    }
                                    //}
                                    break;
                                default:
                                    if (RocketId < 5)
                                    {
                                        var power2 = 900 + new Random().Next(-100, 100);
                                        var power = Convert.ToInt32(power2 * RocketId);
                                        if (Ship.Spectrum && enemy.Ship.Spectrum == false)
                                        {
                                            power = power / 2;
                                        }
                                        else if (Ship.Spectrum && enemy.Ship.Spectrum)
                                        {
                                            power = power / 6;
                                        }
                                        else if (Ship.Spectrum == false && enemy.Ship.Spectrum)
                                        {
                                            power = power / 3;
                                        }
                                        else if (enemy.Ship.ShieldLeech && enemy.Ship.shield > 0)
                                        {
                                            power = (power * 3) / 2;
                                        }
                                        var realDamage = power;
                                        if (enemy.Ship.ISH)
                                        {
                                            realDamage = 0;
                                            power = 0;

                                        }
                                        Program.Users[selected].TakeDamage(realDamage);

                                        if (enemy.Ship.shield > 0)
                                        {
                                            if (enemy.Ship.shield - realDamage > 0)
                                            {
                                                enemy.Ship.shield -= Convert.ToUInt32(realDamage);
                                            }
                                            else
                                            {

                                                enemy.Ship.shield = 0;
                                            }
                                            realDamage = power - Convert.ToInt32(power * (enemy.Ship.shieldAbsorb / 100m));
                                        }

                                        realDamage = Math.Abs(realDamage);
                                        if (enemy.Ship.HP - realDamage < 1 || enemy.Ship.HP > enemy.Ship.maxHP)
                                        {
                                            enemy.Ship.HP = 0;
                                            Ship.isAttacking = false;
                                            if (enemy.Ship.destroyed == false)
                                            {
                                                enemy.Ship.destroyed = true;
                                                enemy.Ship.HP = 0;
                                                Send("0|A|STD|" + " You destroyed " + enemy.Ship.Name);
                                                UpdateUser(1, selected);
                                            }
                                            Ship.isAttacking = false;
                                        }
                                        else
                                        {
                                            try
                                            {
                                                enemy.Ship.HP -= Convert.ToUInt32(realDamage);
                                                var packet = "0|v|" + UserId + "|" + selected + "|H|" + RocketId + "|" + Precision + "|" + Precision;
                                                var packet2 = "0|Y|" + UserId + "|" + selected + "|R|" + enemy.Ship.HP + "|" + enemy.Ship.shield + "|" + power + "|100|1";
                                                var packet3 = "0|M|" + UserId + "|" + selected + "|R|" + enemy.Ship.HP + "|" + enemy.Ship.shield + "|" + power + "|100|1";
                                                Send("0|A|CLD|ROK|3");
                                                Send(packet);
                                                if (enemy.Ship.ISH)
                                                {
                                                    Send(packet3);
                                                    foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                                                    {
                                                        Program.Users[pair].Send(packet);
                                                    }
                                                }
                                                else
                                                {
                                                    Send(packet2);
                                                    foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                                                    {
                                                        Program.Users[pair].Send(packet);
                                                    }
                                                }
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            var packe = "0|v|" + UserId + "|" + selected + "|H|" + RocketId + "|" + Precision + "|" + Precision;
                            var packe2 = "0|M|" + UserId + "|" + selected + "|R|" + enemy.Ship.HP + "|" + enemy.Ship.shield + "|0|100|1";

                            Send(packe);
                            Send(packe2);
                            foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                            {
                                Program.Users[pair].Send(packe);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            { }
        }
        #endregion
        private async void SetRsb()
        {
            try
            {
                Out.WriteLine("userId " + UserId + "Sended packet of RSB reload", Ip, ConsoleColor.Yellow);
                await Task.Delay(3000);
                Ship.CanUseRSB = true;
                Ship.lunchrsb = false;
            }
            catch (Exception)
            { }
        }

        private async void SetRocket()

        {
            if (Ship.premium == 1)
                await Task.Delay(2000);
            else
                await Task.Delay(3000);

            Ship.CanUseRockets = true;
        }


        public async void UpdateShip()
        {

            try
            {
                string shield;


                if (Ship.maxShield == 0 || Ship.maxShield2 == 0)
                {
                    //shield = "";
                    //shield2 = "";
                }
                //else
                //{

                //}

                if (CurrentConfig == 1)
                {
                    // Out.WriteLine("я тут1");
                    Send("HwkU|I|" + UserId + "|" + Ship.Name + "|" + Ship.Id + "|" + Ship.speed + "|" + Ship.shield.ToString() + "|" + Ship.maxShield.ToString() + "|" + Ship.HP + "|" + Ship.GetShipMaxHP() + "|5|100|" + Ship.x + "|" + Ship.y + "|" + MapId + "|" + Ship.GetFactionID() + "|" + Ship.clanID + "|1500|0|15|" + Ship.premium + "|" + Ship.exp + "|" + Ship.honor + "|" + Ship.level + "|" + Ship.credits + "|" + Ship.uri + "|10|" + Ship.rank + "|" + Ship.tagName + "|0|1|" + Ship.cloaked + "|50|25");

                }
                else if (CurrentConfig == 2)
                {
                    //  Out.WriteLine("я тут2");
                    Send("HwkU|I|" + UserId + "|" + Ship.Name + "|" + Ship.Id + "|" + Ship.speed + "|" + Ship.shield.ToString() + "|" + Ship.maxShield.ToString() + "|" + Ship.HP + "|" + Ship.GetShipMaxHP() + "|5|100|" + Ship.x + "|" + Ship.y + "|" + MapId + "|" + Ship.GetFactionID() + "|" + Ship.clanID + "|1500|0|15|" + Ship.premium + "|" + Ship.exp + "|" + Ship.honor + "|" + Ship.level + "|" + Ship.credits + "|" + Ship.uri + "|10|" + Ship.rank + "|" + Ship.tagName + "|0|1|" + Ship.cloaked + "|50|25");
                }
                await Task.Delay(1);
            }
            catch { }
        }

        private async void IsLogout()
        {
            try
            {
                for (var i = 25; i > 0; i--)
                {
                    if (Ship.isLogout)
                    {
                        foreach (var player in Program.Maps[MapId].Users)
                        {
                            if (player.Value.Ship.selectedUserId == UserId && player.Value.Ship.isAttacking)
                            {
                                Send("0|t");
                                Ship.isLogout = false;
                                Ship.logout2 = 0;
                                Ship.logout3 = 0;
                                break;
                            }
                        }
                        Ship.isAttacking = false;
                    }
                    else
                    {

                        Ship.isLogout = false;
                        Ship.logout2 = 0;
                        Ship.logout3 = 0;
                        break;

                    }
                    await Task.Delay(200);
                }
                if (Ship.isLogout)
                {
                    foreach (var player in Program.Maps[MapId].Users)
                    {
                        if (player.Value.Ship.selectedUserId == UserId && player.Value.Ship.isAttacking)
                        {
                            Send("0|t");
                            Ship.isLogout = false;
                            Ship.logout2 = 0;
                            Ship.logout3 = 0;
                            break;
                        }
                    }
                    Ship.logout2 = 0;
                    Ship.logout3 = 0;
                    Send("0|l");
                    Send("0|l");
                    Program.Users[UserId].discconectUser(-1);
                    Program.Maps[MapId].RemoveUser(UserId);
                }
                else
                {
                    Ship.isLogout = false;
                    Ship.logout2 = 0;
                    Ship.logout3 = 0;
                }
            }
            catch (Exception)
            { }
        }
        public void sendStations()
        {


            if (MapId == 1)
            {
                Send("0|s|0|1|redStation|1|1500|1000|1000");
            }
            else if (MapId == 5)
            {
                Send("0|s|0|1|blueStation|2|1500|19800|1000");
            }
            else if (MapId == 9)
            {
                Send("0|s|0|1|greenStation|3|1500|19600|11900");
            }


        }
        #region GroupShits
        public void Follow(uint FollowMember)
        {
            if (!GroupNames.Contains(FollowMember))
            {
                return;
            }
            if (MapId != Program.Users[FollowMember].MapId)
            {
                Send("0|A|STD|Player is not in this map.");
                return;
            }
            var x = Program.Users[FollowMember].Ship.x;
            var y = Program.Users[FollowMember].Ship.y;
            Ship.destinationx = x;
            Ship.destinationy = y;
            Ship.isMoving = true;
            var distanta = Math.Sqrt(Math.Pow(Ship.GetPosX() - x, 2) + Math.Pow(Ship.GetPosY() - y, 2));
            var Time = Convert.ToInt32(distanta / Ship.speed) * 1000;
            Ship.MovingTask = 0;
            Send("0|A|RS|-1");
            Send("0|A|RS|-1");
            Send("0|1|" + UserId + "|" + x + "|" + y + "|" + Time);
        }
        public void Ping(string[] type)
        {
            switch (type[2])
            {
                case "pos":
                    var x = type[3];
                    var y = type[4];
                    foreach (var Pair in GroupNames)
                    {
                        Program.Users[Pair].Send("0|ps|png|" + x + "|" + y);
                    }
                    break;
                case "usr":
                    var PngUserID = Convert.ToUInt32(type[3]);
                    if (!GroupNames.Contains(PngUserID))
                    {
                        return;
                    }
                    if (MapId != Program.Users[PngUserID].MapId)
                    {
                        Send("0|A|STD|Player is not in this map.");
                        return;
                    }

                    if (Program.Users.ContainsKey(PngUserID))
                    {
                        x = Program.Users[PngUserID].Ship.GetPosX().ToString();
                        y = Program.Users[PngUserID].Ship.GetPosY().ToString();
                        foreach (var Pair in GroupNames)
                        {
                            Program.Users[Pair].Send("0|ps|png|" + x + "|" + y);
                        }

                    }
                    break;
            }
        }
        public void AcceptInvitation(uint Id)
        {
            try
            {
                _isboss = false;
                Program.Users[Id].InvitationNames.Remove(UserId);
                Send("0|ps|inv|del|none|" + Id);
                Program.Users[Id].Send("0|ps|inv|del|none|" + Id + "|" + UserId);
                if (Program.Users[Id].GroupNames.Count == 0)
                {
                    Program.Users[Id].GroupNames.Add(Id);
                    Program.Users[Id].GroupNames.Add(UserId);
                    GroupNames.Add(UserId);
                    GroupNames.Add(Id);
                }
                else
                {
                    foreach (var Users in Program.Users[Id].GroupNames)
                    {
                        GroupNames.Add(Users);
                    }
                    foreach (var Users in GroupNames)
                    {
                        Program.Users[Users].GroupNames.Add(UserId);
                    }
                    GroupNames.Add(UserId);
                }
                // ?|?|?|caninvite|?|1ºname|?|1ºhp|1ºmaxhp|1ºshield|1ºmaxshield|1ºmapId|?|?|?|?|?|underattack|?|1ºselectedId|?|shipId
                foreach (var Users in Program.Users[Id].GroupNames)
                {
                    string CreateGroup = null;
                    if (Program.Users[Users]._isboss)
                    {
                        CreateGroup += Program.Users[Users].Ship.Name + "|" + Users + "|" + Program.Users[Users].Ship.HP + "|" + Program.Users[Users].Ship.maxHP + "|" + Program.Users[Users].Ship.shield + "|" + Program.Users[Users].Ship.maxShield + "|" + Program.Users[Users].MapId + "|0|0|0|0|" + Convert.ToUInt32(Program.Users[Users].Ship.cloaked) + "|0|0|" + Program.Users[Users].Ship.selectedUserId + "|0|" + Program.Users[Users].Ship.Id + "|0|1|";
                    }
                    foreach (var Pair in Program.Users[Id].GroupNames)
                    {
                        if (Pair != Users)
                        {
                            CreateGroup += Program.Users[Pair].Ship.Name + "|" + Pair + "|" + Program.Users[Pair].Ship.HP + "|" + Program.Users[Pair].Ship.maxHP + "|" + Program.Users[Pair].Ship.shield + "|" + Program.Users[Pair].Ship.maxShield + "|" + Program.Users[Pair].MapId + "|0|0|0|0|" + Convert.ToUInt32(Program.Users[Pair].Ship.cloaked) + "|0|0|" + Program.Users[Pair].Ship.selectedUserId + "|0|" + Program.Users[Pair].Ship.Id + "|0|1|";
                        }
                    }
                    Program.Users[Users].Send("0|ps|init|grp|1|1|1|1|1|" + CreateGroup);
                }
            }
            catch
            { }
        }
        public void RejectInvitation(uint Id)
        {
            Program.Users[Id].Send("0|ps|inv|del|rj|" + Id + "|" + UserId);
            Send("0|ps|inv|del|none|" + Id);
            Program.Users[Id].InvitationNames.Remove(UserId);

        }
        public void LeaveGroup()
        {
            try
            {
                Send("0|ps|lp|lv|" + UserId);
                _isboss = false;
                var Remove = new List<uint>();
                foreach (var Pair in GroupNames)
                {
                    Program.Users[Pair].Send("0|ps|lp|lv|" + UserId);
                    Remove.Add(Pair);
                }
                foreach (var Pair in Remove)
                {
                    Program.Users[Pair].GroupNames.Remove(UserId);
                }
                GroupNames.Clear();
            }
            catch
            { }
        }

        public void ChangeLeader(uint NewLeader)
        {
            if (!GroupNames.Contains(NewLeader))
            {
                return;
            }
            _isboss = false;
            Program.Users[NewLeader]._isboss = true;
        }

        public void InviteByName(string name)
        {
            try
            {
                var Id = Program.Users.FirstOrDefault(x => x.Value.Ship.Name == name).Key;
                if (name == Ship.Name || !Program.Users.ContainsKey(Id) || Program.Users[Id].GroupNames.Count != 0)
                    return;
                if (Program.Users[Id].Blk == 0 && !InvitationNames.Contains(Id))
                {
                    Program.Users[Id].Send("0|ps|inv|new|" + UserId + "|" + Ship.Name + "|" + Ship.Id + "|" + Id);
                    Send("0|ps|inv|new|" + UserId + "|" + name + "|" + Program.Users[Id].Ship.Id + "|" + Id + "|" + name + "|" + Program.Users[Id].Ship.Id);
                }
                else
                {
                    Send("0|ps|inv|blk");
                }
                if (GroupNames.Count == 0)
                {
                    _isboss = true;
                }
            }
            catch (Exception)
            { }
        }
        public void RemoveInvitation(uint Id)
        {
            try
            {
                Program.Users[Id].Send("0|ps|inv|del|rv|" + UserId);
                Send("0|ps|inv|del|rv|" + UserId + "|" + Id);
                InvitationNames.Remove(Id);
            }
            catch
            { }
        }
        #endregion
        public async void WizzRocket(uint _selecteduid)
        {
            var _1ship = Program.Users[_selecteduid].Ship.Id;
            var NewShip = new Random();
            var Newship1 = NewShip.Next(59, 69);
            Program.Users[_selecteduid].Ship.Id = Convert.ToUInt32(Newship1);
            Program.Users[_selecteduid].UpdateShip();
            //this.Ship.CanUseWizz = false;
            await Task.Delay(8000);
            Program.Users[_selecteduid].Ship.Id = _1ship;
            Program.Users[_selecteduid].UpdateShip();
            //this.Ship.CanUseWizz = true;
        }
        public async void UseWizz(int ms)
        {
            try
            {

                await Task.Delay(30000);
                //this.Ship.CanUseWizz = true;
            }
            catch (Exception)
            {
            }
        }
        #region Tech
        private void SetCooldown(string S, int duration)
        {
            try
            {
                if (Ship.rank == 21 && GodMode)
                {
                    return;
                }
                var slotone = "none";
                var slottwo = "none";
                var slotthree = "none";

                using (var dbClient = Program.Manager.getQueryreactor())
                {
                    var row = (DataTable)dbClient.query("SELECT slot_one,slot_three,slot_two FROM server_1_player_nanotech WHERE playerId = '" + UserId + "'");
                    slotone = row.Rows[0]["slot_one"].ToString();
                    slottwo = row.Rows[0]["slot_two"].ToString();
                    slotthree = row.Rows[0]["slot_three"].ToString();
                }
                /*
    SRPM -- прицел
    SBRB -- ремонтник
    SSBU -- щит     
    SECI -- молния Хд
    SELA -- трансфер
                 */
                if (slotone != "none" && slotone != "locked")
                {
                    var techone = slotone.Split('|');

                    if (techone[0] == "SRPM")
                        Ship.RPMA = Convert.ToInt32(techone[1]);
                    if (techone[0] == "SBRB")
                        Ship.BRBA = Convert.ToInt32(techone[1]);
                    if (techone[0] == "SSBU")
                        Ship.SBUA = Convert.ToInt32(techone[1]);
                    if (techone[0] == "SECI")
                        Ship.ECIA = Convert.ToInt32(techone[1]);
                    if (techone[0] == "SELA")
                        Ship.ELAA = Convert.ToInt32(techone[1]);
                }
                string[] techtwo;
                if (slottwo != "none" && slottwo != "locked")
                {
                    techtwo = slotone.Split('|');

                    if (techtwo[0] == "SRPM")
                        Ship.RPMA = Convert.ToInt32(techtwo[1]);
                    if (techtwo[0] == "SBRB")
                        Ship.BRBA = Convert.ToInt32(techtwo[1]);
                    if (techtwo[0] == "SSBU")
                        Ship.SBUA = Convert.ToInt32(techtwo[1]);
                    if (techtwo[0] == "SECI")
                        Ship.ECIA = Convert.ToInt32(techtwo[1]);
                    if (techtwo[0] == "SELA")
                        Ship.ELAA = Convert.ToInt32(techtwo[1]);
                }
                if (slotthree != "none" && slotthree != "locked")
                {
                    var techthree = slotone.Split('|');

                    if (techthree[0] == "SRPM")
                        Ship.RPMA = Convert.ToInt32(techthree[1]);
                    if (techthree[0] == "SBRB")
                        Ship.BRBA = Convert.ToInt32(techthree[1]);
                    if (techthree[0] == "SSBU")
                        Ship.SBUA = Convert.ToInt32(techthree[1]);
                    if (techthree[0] == "SECI")
                        Ship.ECIA = Convert.ToInt32(techthree[1]);
                    if (techthree[0] == "SELA")
                        Ship.ELAA = Convert.ToInt32(techthree[1]);
                }


                if (S == "RPM")
                {
                    _rpm = DateTime.Now;
                    Ship.RPMA -= 1;

                    if (slotone.StartsWith("SRPM"))
                    {
                        var rpm = "none";
                        if (Ship.RPMA > 0)
                        {
                            var tech = slotone.Split('|');
                            rpm = tech[0] + "|" + Ship.RPMA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_one = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                    else if (slottwo.StartsWith("SRPM"))
                    {

                        var rpm = "none";
                        if (Ship.RPMA > 0)
                        {
                            var tech = slottwo.Split('|');
                            rpm = tech[0] + "|" + Ship.RPMA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_two = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }

                    }
                    else if (slotthree.StartsWith("SRPM"))
                    {
                        var rpm = "none";
                        if (Ship.RPMA > 0)
                        {
                            var tech = slotthree.Split('|');
                            rpm = tech[0] + "|" + Ship.RPMA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_three = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                }
                else if (S == "BRB")
                {
                    _brb = DateTime.Now;
                    Ship.BRBA -= 1;

                    if (slotone.StartsWith("SBRB"))
                    {
                        var rpm = "none";
                        if (Ship.BRBA > 0)
                        {
                            var tech = slotone.Split('|');
                            rpm = tech[0] + "|" + Ship.BRBA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_one = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                    else if (slottwo.StartsWith("SBRB"))
                    {

                        var rpm = "none";
                        if (Ship.BRBA > 0)
                        {
                            var tech = slottwo.Split('|');
                            rpm = tech[0] + "|" + Ship.BRBA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_two = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }

                    }
                    else if (slotthree.StartsWith("SBRB"))
                    {
                        var rpm = "none";
                        if (Ship.BRBA > 0)
                        {
                            var tech = slotthree.Split('|');
                            rpm = tech[0] + "|" + Ship.BRBA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_three = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                }
                else if (S == "SBU")
                {
                    _sbu = DateTime.Now;
                    Ship.SBUA -= 1;

                    if (slotone.StartsWith("SSBU"))
                    {
                        var rpm = "none";
                        if (Ship.SBUA > 0)
                        {
                            var tech = slotone.Split('|');
                            rpm = tech[0] + "|" + Ship.SBUA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_one = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                    else if (slottwo.StartsWith("SSBU"))
                    {

                        var rpm = "none";
                        if (Ship.SBUA > 0)
                        {
                            var tech = slottwo.Split('|');
                            rpm = tech[0] + "|" + Ship.SBUA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_two = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }

                    }
                    else if (slotthree.StartsWith("SSBU"))
                    {
                        var rpm = "none";
                        if (Ship.SBUA > 0)
                        {
                            var tech = slotthree.Split('|');
                            rpm = tech[0] + "|" + Ship.SBUA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_three = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                }
                else if (S == "ECI")
                {
                    _eci = DateTime.Now;
                    Ship.ECIA -= 1;

                    if (slotone.StartsWith("SECI"))
                    {
                        var rpm = "none";
                        if (Ship.ECIA > 0)
                        {
                            var tech = slotone.Split('|');
                            rpm = tech[0] + "|" + Ship.ECIA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_one = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                    else if (slottwo.StartsWith("SECI"))
                    {

                        var rpm = "none";
                        if (Ship.ECIA > 0)
                        {
                            var tech = slottwo.Split('|');
                            rpm = tech[0] + "|" + Ship.ECIA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_two = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }

                    }
                    else if (slotthree.StartsWith("SECI"))
                    {
                        var rpm = "none";
                        if (Ship.ECIA > 0)
                        {
                            var tech = slotthree.Split('|');
                            rpm = tech[0] + "|" + Ship.ECIA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_three = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                }
                else if (S == "ELA")
                {
                    _ela = DateTime.Now;
                    Ship.ELAA -= 1;

                    if (slotone.StartsWith("SELA"))
                    {
                        var rpm = "none";
                        if (Ship.ELAA > 0)
                        {
                            var tech = slotone.Split('|');
                            rpm = tech[0] + "|" + Ship.ELAA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_one = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                    else if (slottwo.StartsWith("SELA"))
                    {

                        var rpm = "none";
                        if (Ship.ELAA > 0)
                        {
                            var tech = slottwo.Split('|');
                            rpm = tech[0] + "|" + Ship.ELAA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_two = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }

                    }
                    else if (slotthree.StartsWith("SELA"))
                    {
                        var rpm = "none";
                        if (Ship.ELAA > 0)
                        {
                            var tech = slotthree.Split('|');
                            rpm = tech[0] + "|" + Ship.ELAA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_three = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                }
                else if (S == "SMB")
                {
                    _smb = DateTime.Now;
                }
                else if (S == "EMP")
                {
                    _emp = DateTime.Now;
                }
                else if (S == "ISH")
                {
                    _ish = DateTime.Now;
                }
                Send("0|A|CLD|" + S + "|" + duration);

            }
            catch { }
        }
        #region EnergyLeech
        public async void ActivateELA()
        {
            var Cooldown = (DateTime.Now - _ela);
            if (Cooldown.TotalSeconds < 120 || Ship.ELAA == 0)
            {
            }
            else
            {
                EnergyLeechActive = true;
                Send("0|TX|A|0|ELA|" + UserId);
                SetCooldown("ELA", 120);
                await Task.Delay(60000);

                EnergyLeechActive = false;
                Send("0|TX|D|0|ELA|" + UserId);
            }
        }
        #endregion
        #region BattleRepairBot
        public async void ActivateBRB()
        {
            try
            {
                var Cooldown = (DateTime.Now - _brb);
                if (Cooldown.TotalSeconds < 38 || Ship.BRBA == 0)
                {
                }
                else
                {
                    SetCooldown("BRB", 45);
                    foreach (var user in PlayersInRange)
                    {
                        Program.Users[user].Send("0|TX|A|R|BRB|" + UserId);
                    }
                    var count = 1;
                    while (count <= 10)
                    {
                        BRBRepair();

                        await Task.Delay(1000);

                        count++;
                    }
                    foreach (var user in PlayersInRange)
                    {
                        Program.Users[user].Send("0|TX|D|R|BRB|" + UserId);
                    }
                }
            }
            catch { }
        }
        private void BRBRepair()
        {
            try
            {
                if (!Program.Users.ContainsKey(UserId))
                {
                    return;
                }
                if (Ship.GetShipHP() == Ship.GetShipMaxHP())
                {
                    Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.GetShipHP() + "|0");

                }
                else if (Ship.GetShipHP() + 10000 > Ship.GetShipMaxHP())
                {
                    long hp = Ship.GetShipMaxHP() - Ship.GetShipHP();
                    Ship.HP = Ship.GetShipMaxHP();
                    Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.GetShipHP() + "|" + hp);
                }
                else
                {
                    Ship.HP += 10000;
                    Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.GetShipHP() + "|10000");
                }
            }
            catch { }
        }
        #endregion
        #region ShieldBackup
        public void ActivateSBU()
        {
            var Cooldown = (DateTime.Now - _sbu);
            if (Cooldown.TotalSeconds < 26 || Ship.SBUA == 0)
            {
            }
            else
            {
                SetCooldown("SBU", 30);
                if (Ship.shield + 75000 > Ship.maxShield)
                {
                    long shield = Ship.maxShield - Ship.shield;
                    Ship.shield = Ship.maxShield;
                    Send("0|A|HL|0|" + UserId + "|SHD|" + Ship.shield + "|" + shield);
                    Send("0|A|CLD|SBU|30");
                    foreach (var user in PlayersInRange)
                    {
                        Program.Users[user].Send("0|TX|A|0|SBU|" + UserId);
                    }
                }
                else
                {
                    Ship.shield += 75000;
                    Send("0|A|HL|0|" + UserId + "|SHD|" + Ship.shield + "|75000");
                    foreach (var user in PlayersInRange)
                    {
                        Program.Users[user].Send("0|TX|A|0|SBU|" + UserId);
                    }
                }
            }
        }
        #endregion
        #region PrecisionTargeter
        public async void PrecisionTargeter()
        {
            var Cooldown = (DateTime.Now - _rpm);
            if (Cooldown.TotalSeconds < 150 || Ship.RPMA == 0)
            {
            }
            else
            {
                SetCooldown("RPM", 200);
                Precision = 2;
                await Task.Delay(60000);
                Precision = 0;
            }
        }
        #endregion
        #region ChainImpulse
        public void ChainImpulse()
        {
            var Cooldown = (DateTime.Now - _eci);
            if (Cooldown.TotalSeconds < 60 || Ship.selectedUserId == 0 || Ship.ECIA == 0)
            {
            }
            else
            {
                SetCooldown("ECI", 60);
                var target = Ship.selectedUserId.ToString();
                var nX = Program.Users[Ship.selectedUserId].Ship.GetPosX();
                var nY = Program.Users[Ship.selectedUserId].Ship.GetPosY();
                var MaxTargets = 1;

                foreach (var user in PlayersInRange)
                {
                    var pX = Program.Users[user].Ship.GetPosX();
                    var pY = Program.Users[user].Ship.GetPosY();
                    var distance = Convert.ToInt32(Distance.Get(pX, nX, pY, nY));
                    if (distance < 800 && UserId != user && Ship.selectedUserId != user)
                    {
                        MaxTargets++;
                        target += "|" + user;
                    }
                    if (MaxTargets == 7)
                    {
                        break;
                    }
                }
                foreach (var user in PlayersInRange)
                {
                    Program.Users[user].Send("0|TX|ECI|R|" + UserId + "|" + target);
                }
                var Targets = target.Split('|');
                foreach (var userid in Targets)
                {
                    var _userID = UInt32.Parse(userid);
                    if (Program.Users[_userID].Ship.HP - 9970 < 1)
                    {
                        //foreach (var pir in Program.Maps[mapId].Users)
                        //{);
                    }
                    else
                    {
                        Program.Users[_userID].Ship.HP -= 9970;
                    }
                    Program.Users[UserId].Send("0|Y|1|" + _userID + "|L|" + Program.Users[_userID].Ship.HP + "|" + Program.Users[_userID].Ship.shield + "|9970|100|1");
                    Program.Users[_userID].Send("0|Y|1|" + _userID + "|L|" + Program.Users[_userID].Ship.HP + "|" + Program.Users[_userID].Ship.shield + "|9970|100|1");
                    Program.Users[_userID].Send("0|A|HPT|" + Program.Users[_userID].Ship.HP + "|" + Program.Users[_userID].Ship.maxHP + "|" + _userID);
                    Program.Users[_userID].Send("0|A|SHD|" + Program.Users[_userID].Ship.shield + "|" + Program.Users[_userID].Ship.maxShield + "|" + _userID);
                }
            }
        }
        #endregion
        #endregion
        #region Special Ammo
        #region EMP
        private async void SetLock()
        {
            await Task.Delay(2900);
            Ship.CanBeLocked = true;
        }
        public async void UseEMP()
        {
            var Cooldown = (DateTime.Now - _emp);
            if (Cooldown.TotalSeconds < 40 || Ship.EMPA < 1)
            {
            }
            else
            {
                Send("0|n|EMP|" + UserId); SetCooldown("EMP", 40);
                SetLock();
                updateMines(5);
                Ship.CanBeLocked = false;
                if (Ship.Singularity)
                {
                    Ship.Singularity = false;
                }


                foreach (var user in Program.Maps[MapId].Users)
                {
                    if (user.Key == UserId) continue;
                    var distance = Distance.Get(user.Value.Ship.GetPosX(), Ship.GetPosX(), user.Value.Ship.GetPosY(), Ship.GetPosY());
                    user.Value.Send("0|n|EMP|" + UserId);

                    if (user.Value.Ship.selectedUserId == UserId)
                    {
                        user.Value.Send("0|A|STM|msg_own_targeting_harmed");
                        user.Value.Send("0|N|0");
                        user.Value.Ship.isAttacking = false;
                        user.Value.Ship.selectedUserId = 0;
                    }
                    if (distance < 400 && user.Value.UserId != UserId)
                        user.Value.EmpEffect();
                }
            }
            await Task.Delay(1);
        }
        #endregion
        #region SMB
        public async void UseSMB()
        {
            var Cooldown = (DateTime.Now - _smb);
            if (Cooldown.TotalSeconds < 30 || Ship.InSecureArea || Ship.ACM01 < 10 || !Ship.SMBCPU)
            {
            }
            else
            {
                Send("0|n|SMB|" + UserId);
                updateMines(0);
                SetCooldown("SMB", 30);

                foreach (var user in PlayersInRange)
                {
                    if (user == UserId) continue;
                    Program.Users[user].Send("0|n|SMB|" + UserId);
                    var distance = Distance.Get(Program.Users[user].Ship.GetPosX(), Ship.GetPosX(), Program.Users[user].Ship.GetPosY(), Ship.GetPosY());
                    if (distance < 500 && !(!Program.Users[user].Ship.isAttacking && Program.Users[user].Ship.InSecureArea) && user != UserId)
                        Program.Users[user].SmbDamage();
                }
            }
            await Task.Delay(1);
        }
        #endregion
        #region ISH
        public async void UseISH()
        {
            var Cooldown = (DateTime.Now - _ish);
            if (Cooldown.TotalSeconds < 22 || Ship.ACM01 < 10 || !Ship.ISHCPU)
            {
            }
            else
            {
                SetCooldown("ISH", 25);
                updateMines(0);
                foreach (var user in PlayersInRange)
                {
                    Program.Users[user].Send("0|n|ISH|" + UserId);
                }
                Ship.ISH = true;
                await Task.Delay(2900);
                Ship.ISH = false;
            }
        }
        #endregion
        #region Mine
        public void createMine(int type)
        {
            if (Ship.InSecureArea)
            {
                return;
            }
            if (type == 1 && Ship.ACM01 == 0)
            {
                return;
            }
            else if (type == 2 && Ship.EMPM01 == 0)
            {
                return;
            }
            else if (type == 3 && Ship.SABM01 == 0)
            {
                return;
            }
            else if (type == 4 && Ship.DDM01 == 0)
            {
                return;
            }
            updateMines(type);
            var Cooldown = (DateTime.Now - _mine);
            if (Cooldown.TotalSeconds > 26)
            {

                _mine = DateTime.Now;
                Send("0|A|CLD|MIN|30");
                var id = Program.Random.Next(9, 100000);
                var packet = "0|L|" + id + "|" + type + "|" + Ship.GetPosX() + "|" + Ship.GetPosY();
                Program.Maps[MapId].AddMine(id, Ship.GetPosX(), Ship.GetPosY(), type);
                foreach (var user in Program.Maps[MapId].Users)
                {
                    user.Value.Send(packet);
                }
            }
        }
        private void updateMines(int type = 0)
        {

            switch (type)
            {
                case 0:
                    var a = Program.loadAmmos(UserId, "ammunition_mine_acm-01");
                    if (a != Ship.ACM01)
                        Ship.ACM01 = a;

                    Ship.ACM01 -= 10;
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Ship.ACM01 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_mine_acm-01'");
                    }
                    double Ammo = Ship.ACM01 / 10;
                    Ship.ISHA = (int)Math.Truncate(Ammo);
                    Ship.SMBA = (int)Math.Truncate(Ammo);

                    break;
                case 1:
                    var c = Program.loadAmmos(UserId, "ammunition_mine_acm-01");
                    if (c != Ship.ACM01)
                        Ship.ACM01 = c;

                    Ship.ACM01 -= 1;
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Ship.ACM01 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_mine_acm-01'");
                    }

                    break;
                case 2:
                    Ship.EMPM01 -= 1;
                    break;
                case 3:
                    Ship.SABM01 -= 1;
                    break;
                case 4:
                    Ship.DDM01 -= 1;
                    break;
                case 5:
                    var b = Program.loadAmmos(UserId, "ammunition_specialammo_emp-01");
                    if (b != Ship.EMPA)
                        Ship.EMPA = b;

                    Ship.EMPA -= 1;
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Ship.EMPA + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_specialammo_emp-01'");
                    }
                    break;
                default:
                    break;
            }
            SendSpAmmo();

        }
        private void SendSpAmmo()
        {
            Send("0|3|" + Ship.R310 + "|" + Ship.PLT2026 + "|" + Ship.PLT2021 + "|" + Ship.PLT3030 + "|" + Ship.PLD8 + "|" + Ship.DCR250 + "|" + Ship.WIZ + "|" + Ship.ACM01 + "|" + Ship.SMBA + "|" + Ship.ISHA + "|" + Ship.EMPA + "|" + Ship.EMPM01 + "|" + Ship.SABM01 + "|" + Ship.DDM01);
        }
        #endregion
        #region Fireworks
        public static string GetHash(int length)
        {
            //rastgele stringimizin oluşturulacağı karakter dizisi
            const string characters = "acbdef"
            + "01345678";
            var randomString = "";
            var rasgele = new Random((int)DateTime.Now.Ticks);
            for (var i = 0; i < length; i++)
            {
                randomString += characters[rasgele.Next(characters.Length)];
            }
            return randomString;
        }

        public void createFireworkBox()
        {
            if (CreatedFireworks == 0)
            {
                return;
            }
            CreatedFireworks++;
            var hash = GetHash(32);
            var packet = "0|L|" + hash + "|333|" + Ship.GetPosX() + "|" + Ship.GetPosY();
            foreach (var user in Program.Maps[MapId].Users)
            {
                user.Value.Send(packet);
            }
            Program.Maps[MapId].AddFireworkBox(hash, Ship.GetPosX(), Ship.GetPosY(), Convert.ToInt32(UserId));
        }

        #endregion
        #endregion
        #region PvEshits
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
                        var update = Program.Users[id].GetXY();
                        Program.Maps[MapId].RemoveUser(id);
                        Program.DisconectUser2(id);
                        Ship.selectedUserId = 0;

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_players SET x = '" + update[0] + "',y = '" + update[1] + "',mapId = '" + update[2] + "' WHERE playerID = '" +
                                           UserId + "'");
                        }
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
        public static int changed_ = 0;

        #region Background DB sync (каждые 30 сек)
        // Фоновая проверка: изменилось ли ЧТО ЛИБО в БД (покупки в магазине/ангаре,
        // замена снаряжения/дройдов с другого клиента и т.п.) и доводка состояния клиента.
        // Отслеживаются ВСЕ игровые таблицы, влияющие на бой/инвентарь:
        //   * server_1_player_all_items (все lootid: боеприпасы, лазеры, щиты, дроиды...)
        //   * server_1_hangar_config_ship (снаряжение: пушки/щиты/процессоры/хайтеки)
        //   * server_1_player_drones (дройды, например zeus)
        //   * server_1_hangar_config_drones (привязка дройдов к конфигам)
        //   * server_1_player_nanotech (нано-технологии ELAA/ECIA/RPMA/SBU/BRB)
        //   * server_1_players (ресурсы uri/credits, опыт, уровень, ранг)
        private DateTime _lastDbSyncCheck = DateTime.MinValue;
        private string _dbInventoryHash = null;      // снимок строки инвентаря (lootid:Q)
        private string _dbEquipmentHash = null;      // снимок снаряжения (конфиги + дроиды + нано)
        private int _dbUri = int.MinValue, _dbCredits = int.MinValue;
        private int _dbExp = int.MinValue, _dbHonor = int.MinValue, _dbLevel = int.MinValue;
        private const int DbSyncIntervalMs = 30000;

        private static string Md5Hex(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private void SyncWithDatabase()
        {
            // Вызывается из UpdateShip_ (фоновый поток игрока). Не чаще раза в 30 секунд.
            var now = DateTime.Now;
            if ((now - _lastDbSyncCheck).TotalMilliseconds < DbSyncIntervalMs) return;
            _lastDbSyncCheck = now;

            try
            {
                using (var dbClient = Program.Manager.getQueryreactor())
                {
                    bool inventoryChanged = false;
                    bool equipmentChanged = false;
                    bool profileChanged = false;

                    // ---- 1. Инвентарь: боеприпасы (ammunition_%) ----
                    // Сравниваем хеш ВСЕХ строк боеприпасов: покупка в магазине меняет Q,
                    // а новая покупка добавляет строку — любое изменение ловится сразу.
                    var res = (DataTable)dbClient.query("SELECT lootid,Q FROM server_1_player_all_items WHERE server_1_player_all_items.playerID=" + UserId + " AND server_1_player_all_items.lootid LIKE 'ammunition_%'");
                    var parts = new List<string>();
                    foreach (DataRow row in res.Rows)
                        parts.Add(row["lootid"].ToString() + ":" + row["Q"].ToString());
                    parts.Sort(StringComparer.Ordinal);
                    var invHash = Md5Hex(string.Join(",", parts.ToArray()));
                    if (_dbInventoryHash == null)
                    {
                        // Первый пропуск после логина: LoadConfigs()/вход уже загрузили
                        // актуальные значения — просто запоминаем снимок, ничего не трогаем.
                        _dbInventoryHash = invHash;
                    }
                    else if (_dbInventoryHash != invHash)
                    {
                        _dbInventoryHash = invHash;
                        inventoryChanged = true;
                    }

                    // ---- 2. Снаряжение: ангарные конфиги, дроиды, привязки, нанотех ----
                    var eqParts = new List<string>();
                    foreach (uint cfgNum in new uint[] { 1, 2 })
                    {
                        var eq = (DataTable)dbClient.query("SELECT lasers,generators,heavy_guns,extras FROM server_1_hangar_config_ship WHERE playerID=" + UserId + " AND configNum=" + cfgNum);
                        foreach (DataRow row in eq.Rows)
                            eqParts.Add("cfg" + cfgNum + "=" + row["lasers"] + "/" + row["generators"] + "/" + row["heavy_guns"] + "/" + row["extras"]);
                    }
                    var dr = (DataTable)dbClient.query("SELECT drone_kind,drone_level FROM server_1_player_drones WHERE playerID=" + UserId + " ORDER BY id");
                    foreach (DataRow row in dr.Rows)
                        eqParts.Add("d=" + row["drone_kind"] + ":" + row["drone_level"]);
                    var cd = (DataTable)dbClient.query("SELECT * FROM server_1_hangar_config_drones WHERE playerID=" + UserId + " ORDER BY configNum");
                    foreach (DataRow row in cd.Rows)
                    {
                        var rp = new List<string>();
                        foreach (DataColumn c in cd.Columns) rp.Add(Convert.ToString(row[c]));
                        eqParts.Add("cd=" + string.Join(":", rp.ToArray()));
                    }
                    var nt = (DataTable)dbClient.query("SELECT slot_one,slot_two,slot_three FROM server_1_player_nanotech WHERE playerId=" + UserId);
                    foreach (DataRow row in nt.Rows)
                        eqParts.Add("nt=" + row["slot_one"] + "/" + row["slot_two"] + "/" + row["slot_three"]);
                    eqParts.Sort(StringComparer.Ordinal);
                    var eqHash = Md5Hex(string.Join("|", eqParts.ToArray()));
                    if (_dbEquipmentHash == null)
                    {
                        _dbEquipmentHash = eqHash;
                    }
                    else if (_dbEquipmentHash != eqHash)
                    {
                        _dbEquipmentHash = eqHash;
                        equipmentChanged = true;
                    }

                    // ---- 3. Профиль: ресурсы/опыт/уровень (БД — источник истины) ----
                    var pdata = (DataTable)dbClient.query("SELECT uri,credits,exp,honor,level FROM server_1_players WHERE playerID=" + UserId);
                    if (pdata.Rows.Count == 1)
                    {
                        var r0 = pdata.Rows[0];
                        int dbUri = Convert.ToInt32(r0["uri"]);
                        int dbCredits = Convert.ToInt32(r0["credits"]);
                        int dbExp = Convert.ToInt32(r0["exp"]);
                        int dbHonor = Convert.ToInt32(r0["honor"]);
                        int dbLevel = Convert.ToInt32(r0["level"]);
                        if (dbUri != _dbUri || dbCredits != _dbCredits ||
                            dbExp != _dbExp || dbHonor != _dbHonor ||
                            dbLevel != _dbLevel)
                        {
                            profileChanged = true;
                            _dbUri = dbUri; _dbCredits = dbCredits;
                            _dbExp = dbExp; _dbHonor = dbHonor;
                            _dbLevel = dbLevel;
                            Ship.uri = dbUri;
                            Ship.credits = dbCredits;
                            Ship.exp = dbExp;
                            Ship.honor = dbHonor;
                            Ship.level = dbLevel;
                        }
                    }

                    // ---- 4. Применение изменений ----
                    if (equipmentChanged || inventoryChanged)
                    {
                        // Полная перезагрузка снаряжения/дроидов/боеприпасов из БД — тот же
                        // путь, что при входе (пушки, щиты, процессоры, хайтеки, дроиды).
                        LoadConfigs();
                    }

                    if (inventoryChanged || equipmentChanged || profileChanged)
                    {
                        // Рассылка клиенту тех же пакетов, что при входе (SendUserShit):
                        // UpdateShip() шлёт профиль HwkU|I (ресурсы/характеристики корабля).
                        UpdateShip();
                        // Боеприпасы: лазеры (0|B|), ракеты/шахты/мин (0|RL|R|, 0|3|)
                        Send("0|B|" + Ship.x1 + "|" + Ship.x2 + "|" + Ship.x3 + "|" + Ship.UCB100 + "|" + Ship.sab + "|" + Ship.rsb);
                        Send("0|RL|R|" + Ship.HSTRM01 + "|" + Ship.UBR100 + "|" + Ship.ECO10);
                        SendSpAmmo();
                        // Слоты REPCPU/ISICPU/AROCPU (реботы и процессоры из ангара)
                        Send("0|A|ITM|0|0|0|0|" + Ship.RepBot + "|0|0|" + Program.ToEnum(Ship.SMBCPU) + "|" + Program.ToEnum(Ship.ISHCPU) + "|0|0|" + Program.ToEnum(Ship.AROCKET) + "|0|0|0|0|0");
                        // Нанотехнологии (ELAA/ECIA/RPMA/SBUA/BRBA)
                        Send("0|TX|S|" + ((Ship.ELAA != 0) ? "1" : "0") + "|" + Ship.ELAA + "|0|" + ((Ship.ECIA != 0) ? "1" : "0") + "|" + Ship.ECIA + "|0|" + ((Ship.RPMA != 0) ? "1" : "0") + "|" + Ship.RPMA + "|0|" + ((Ship.SBUA != 0) ? "1" : "0") + "|" + Ship.SBUA + "|0|" + ((Ship.BRBA != 0) ? "1" : "0") + "|" + Ship.BRBA + "|0");
                        // Дройды (например купленный zeus): этому игроку и всем в радиусе.
                        Send("0|n|d|" + UserId + "|" + Ship.DronePacket);
                        try
                        {
                            foreach (var pair in Program.Maps[MapId].Users)
                            {
                                if (pair.Key == UserId) continue;
                                try { pair.Value.Send("0|n|d|" + UserId + "|" + Ship.DronePacket); }
                                catch { }
                            }
                        }
                        catch { }

                        Out.WriteLine("DB sync: changes detected for user " + UserId +
                                      " (inventory=" + inventoryChanged + ", equipment=" + equipmentChanged +
                                      ", profile=" + profileChanged + ")", "User.DbSync", ConsoleColor.Cyan);
                    }
                }
            }
            catch (Exception e)
            {
                // Фоновая синхронизация не должна ронять цикл UpdateShip_.
                Out.WriteLine("SyncWithDatabase failed for user " + UserId + ": " + e.Message, "User.DbSync", ConsoleColor.DarkYellow);
            }
        }
        #endregion

        public void LoadConfigs()
        {
            try
            {
                Ship.DronePacket = Program.LoadDrones(UserId);
                var eq1 = Program.LoadEq(UserId, 1);
                var eq2 = Program.LoadEq(UserId, 2);



                uint DamageC1 = 0, ShieldC1 = 0, SpeedC1 = 0, DronesC1 = 0, DamageC2 = 0, ShieldC2 = 0, SpeedC2 = 0, DronesC2 = 0;
                var C1E = "";
                var C2E = "";
                string[] C1Lasers, C2Lasers, C1Gens, C2Gens, C1Ex, C2Ex;
                if (eq1["lasers"].ToString() != "")
                {
                    var C1L = eq1["lasers"].ToString();
                    if (C1L.Contains('|'))
                    {
                        C1Lasers = C1L.Split('|');

                        foreach (var it in from item in C1Lasers where item != "" select Program.LoadItem(UserId, item).Split('|') into it where it[0] == "lsr" select it)
                        {

                            DamageC1 += Convert.ToUInt32(it[1]);
                            Ship.lasers1++;
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, C1L).Split('|');
                        if (it[0] == "lsr")
                        {
                            DamageC1 += Convert.ToUInt32(it[1]);
                            Ship.lasers1++;
                        }
                    }
                }
                if (eq1["generators"].ToString() != "")
                {
                    var C1G = eq1["generators"].ToString();
                    if (C1G.Contains('|'))
                    {
                        C1Gens = C1G.Split('|');

                        foreach (var it in from item in C1Gens where item != "" select Program.LoadItem(UserId, item).Split('|'))
                        {
                            if (it[0] == "spd")
                                SpeedC1 += Convert.ToUInt32(it[1]);
                            if (it[0] == "shd")
                                ShieldC1 += Convert.ToUInt32(it[1]);
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, C1G).Split('|');
                        if (it[0] == "spd")
                            SpeedC1 += Convert.ToUInt32(it[1]);
                        if (it[0] == "shd")
                            ShieldC1 += Convert.ToUInt32(it[1]);
                    }
                }
                if (eq2["lasers"].ToString() != "")
                {
                    var C2L = eq2["lasers"].ToString();
                    if (C2L.Contains('|'))
                    {
                        C2Lasers = C2L.Split('|');
                        foreach (var it in from item in C2Lasers where item != "" select Program.LoadItem(UserId, item).Split('|') into it where it[0] == "lsr" select it)
                        {
                            DamageC2 += Convert.ToUInt32(it[1]);
                            Ship.lasers2++;
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, C2L).Split('|');
                        if (it[0] == "lsr")
                        {
                            DamageC2 += Convert.ToUInt32(it[1]);
                            Ship.lasers2++;
                        }
                    }
                }
                if (eq2["generators"].ToString() != "")
                {
                    var c2G = eq2["generators"].ToString();

                    if (c2G.Contains('|'))
                    {
                        C2Gens = c2G.Split('|');
                        foreach (var it in from item in C2Gens where item != "" select Program.LoadItem(UserId, item).Split('|'))
                        {
                            if (it[0] == "spd")
                                SpeedC2 += Convert.ToUInt32(it[1]);
                            else if (it[0] == "shd")
                                ShieldC2 += Convert.ToUInt32(it[1]);
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, c2G).Split('|');
                        if (it[0] == "spd")
                            SpeedC1 += Convert.ToUInt32(it[1]);
                        if (it[0] == "shd")
                            ShieldC1 += Convert.ToUInt32(it[1]);
                    }
                }
                if (eq2["heavy_guns"].ToString() != "")
                {
                    var it = Program.LoadItem(UserId, eq2["heavy_guns"].ToString()).Split('|');
                    if (it[0] == "equipment_weapon_rocketlauncher_hst-2")
                    {
                        Ship.HSTRM2 = 2;
                    }
                    else if (it[0] == "equipment_weapon_rocketlauncher_hst-1")
                    {
                        Ship.HSTRM2 = 1;
                    }
                }
                if (eq1["heavy_guns"].ToString() != "")
                {
                    var it = Program.LoadItem(UserId, eq1["heavy_guns"].ToString()).Split('|');
                    if (it[0] == "equipment_weapon_rocketlauncher_hst-2")
                    {
                        Ship.HSTRM = 2;
                        Ship.HSTRM1 = 2;
                    }
                    else if (it[0] == "equipment_weapon_rocketlauncher_hst-1")
                    {
                        Ship.HSTRM = 1;
                        Ship.HSTRM1 = 1;
                    }
                }
                if (eq2["extras"].ToString() != "")
                {
                    C2E = eq2["extras"].ToString();
                    if (C2E.Contains('|'))
                    {
                        C2Ex = C2E.Split('|');
                        foreach (var it in from item in C2Ex where item != "" select Program.LoadItem(UserId, item).Split('|'))
                        {
                            if (it[0] == "equipment_extra_repbot_rep-1")
                            {
                                Ship.RepBot2 = 1;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-2")
                            {
                                Ship.RepBot2 = 2;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-3")
                            {
                                Ship.RepBot2 = 3;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-4")
                            {
                                Ship.RepBot2 = 4;
                            }
                            else if (it[0] == "equipment_extra_cpu_ish-01")
                            {
                                Ship.ISHCPU2 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_smb-01")
                            {
                                Ship.SMBCPU2 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_arol-x")
                            {
                                Ship.AROCKET2 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_nc-rrb-x")
                            {
                                Ship.ARepBot2 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_rllb-x")
                            {
                                Ship.AHSTRM2 = true;
                            }
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, C2E).Split('|');
                        if (it[0] == "equipment_extra_repbot_rep-1")
                        {
                            Ship.RepBot2 = 1;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-2")
                        {
                            Ship.RepBot2 = 2;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-3")
                        {
                            Ship.RepBot2 = 3;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-4")
                        {
                            Ship.RepBot2 = 4;
                        }
                        else if (it[0] == "equipment_extra_cpu_ish-01")
                        {
                            Ship.ISHCPU2 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_smb-01")
                        {
                            Ship.SMBCPU2 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_arol-x")
                        {
                            Ship.AROCKET2 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_nc-rrb-x")
                        {
                            Ship.ARepBot2 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_rllb-x")
                        {
                            Ship.AHSTRM2 = true;
                        }

                    }
                }
                if (eq1["extras"].ToString() != "")
                {
                    C1E = eq1["extras"].ToString();
                    if (C1E.Contains('|'))
                    {
                        C1Ex = C1E.Split('|');
                        foreach (var it in from item in C1Ex where item != "" select Program.LoadItem(UserId, item).Split('|'))
                        {
                            if (it[0] == "equipment_extra_repbot_rep-1")
                            {
                                Ship.RepBot1 = 1;
                                Ship.RepBot = 1;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-2")
                            {
                                Ship.RepBot1 = 2;
                                Ship.RepBot = 2;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-3")
                            {
                                Ship.RepBot1 = 3;
                                Ship.RepBot = 3;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-4")
                            {
                                Ship.RepBot1 = 4;
                                Ship.RepBot = 4;
                            }
                            else if (it[0] == "equipment_extra_cpu_ish-01")
                            {
                                Ship.ISHCPU = true;
                                Ship.ISHCPU1 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_smb-01")
                            {
                                Ship.SMBCPU = true;
                                Ship.SMBCPU1 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_arol-x")
                            {
                                Ship.AROCKET = true;
                                Ship.AROCKET1 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_nc-rrb-x")
                            {
                                Ship.ARepBot = true;
                                Ship.ARepBot1 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_rllb-x")
                            {
                                Ship.AHSTRM = true;
                                Ship.AHSTRM1 = true;
                            }
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, C1E).Split('|');
                        if (it[0] == "equipment_extra_repbot_rep-1")
                        {
                            Ship.RepBot1 = 1;
                            Ship.RepBot = 1;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-2")
                        {
                            Ship.RepBot1 = 2;
                            Ship.RepBot = 2;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-3")
                        {
                            Ship.RepBot1 = 3;
                            Ship.RepBot = 3;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-4")
                        {
                            Ship.RepBot1 = 4;
                            Ship.RepBot = 4;
                        }
                        else if (it[0] == "equipment_extra_cpu_ish-01")
                        {
                            Ship.ISHCPU = true;
                            Ship.ISHCPU1 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_smb-01")
                        {
                            Ship.SMBCPU = true;
                            Ship.SMBCPU1 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_arol-x")
                        {
                            Ship.AROCKET = true;
                            Ship.AROCKET1 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_nc-rrb-x")
                        {
                            Ship.ARepBot = true;
                            Ship.ARepBot1 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_rllb-x")
                        {
                            Ship.AHSTRM = true;
                            Ship.AHSTRM1 = true;
                        }
                        //else if (it[0] == "equipment_extra_cpu_nc-rrb-x")
                        //{
                        //    Ship.ARepBot = true;
                        //    Ship.ARepBot1 = true;
                        //}
                    }

                }
                var A = Program.LoadDroneEq(UserId).Split('|');
                DamageC1 += Convert.ToUInt32(A[0]);
                ShieldC1 += Convert.ToUInt32(A[1]);
                DamageC2 += Convert.ToUInt32(A[2]);
                ShieldC2 += Convert.ToUInt32(A[3]);
                Ship.lasers = Ship.lasers1;

                using (var dbClient = Program.Manager.getQueryreactor())
                {
                    var res = (DataTable)dbClient.query("SELECT lootid,Q FROM server_1_player_all_items WHERE server_1_player_all_items.playerID=" + UserId + " AND server_1_player_all_items.lootid LIKE 'ammunition_%'");
                    foreach (DataRow row in res.Rows)
                    {
                        if (row["lootid"].ToString() == "ammunition_rocket_plt-2021")
                            Ship.PLT2021 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocket_plt-3030")
                            Ship.PLT3030 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocket_plt-2026")
                            Ship.PLT2026 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocket_r-310")
                            Ship.R310 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_specialammo_emp-01")
                            Ship.EMPA = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocketlauncher_eco-10")
                            Ship.ECO10 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocketlauncher_ubr-100")
                            Ship.UBR100 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocketlauncher_hstrm-01")
                            Ship.HSTRM01 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_lcb-10")
                            Ship.x1 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_mcb-25")
                            Ship.x2 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_mcb-50")
                            Ship.x3 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_ucb-100")
                            Ship.UCB100 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_rsb-75")
                            Ship.rsb = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_sab-50")
                            Ship.sab = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_mine_acm-01")
                            Ship.ACM01 = int.Parse(row["Q"].ToString());
                    }
                    var slotone = "none";
                    var slottwo = "none";
                    var slotthree = "none";
                    res = (DataTable)dbClient.query("SELECT slot_one,slot_three,slot_two FROM server_1_player_nanotech WHERE playerId = '" + UserId + "'");
                    slotone = res.Rows[0]["slot_one"].ToString();
                    slottwo = res.Rows[0]["slot_two"].ToString();
                    slotthree = res.Rows[0]["slot_three"].ToString();

                    if (slotone != "none" && slotone != "locked")
                    {
                        var techone = slotone.Split('|');

                        if (techone[0] == "SRPM")
                            Ship.RPMA = Convert.ToInt32(techone[1]);
                        if (techone[0] == "SBRB")
                            Ship.BRBA = Convert.ToInt32(techone[1]);
                        if (techone[0] == "SSBU")
                            Ship.SBUA = Convert.ToInt32(techone[1]);
                        if (techone[0] == "SECI")
                            Ship.ECIA = Convert.ToInt32(techone[1]);
                        if (techone[0] == "SELA")
                            Ship.ELAA = Convert.ToInt32(techone[1]);
                    }
                    if (slottwo != "none" && slottwo != "locked")
                    {
                        var techtwo = slottwo.Split('|');

                        if (techtwo[0] == "SRPM")
                            Ship.RPMA = Convert.ToInt32(techtwo[1]);
                        if (techtwo[0] == "SBRB")
                            Ship.BRBA = Convert.ToInt32(techtwo[1]);
                        if (techtwo[0] == "SSBU")
                            Ship.SBUA = Convert.ToInt32(techtwo[1]);
                        if (techtwo[0] == "SECI")
                            Ship.ECIA = Convert.ToInt32(techtwo[1]);
                        if (techtwo[0] == "SELA")
                            Ship.ELAA = Convert.ToInt32(techtwo[1]);
                    }
                    if (slotthree != "none" && slotthree != "locked")
                    {
                        var techthree = slotthree.Split('|');

                        if (techthree[0] == "SRPM")
                            Ship.RPMA = Convert.ToInt32(techthree[1]);
                        if (techthree[0] == "SBRB")
                            Ship.BRBA = Convert.ToInt32(techthree[1]);
                        if (techthree[0] == "SSBU")
                            Ship.SBUA = Convert.ToInt32(techthree[1]);
                        if (techthree[0] == "SECI")
                            Ship.ECIA = Convert.ToInt32(techthree[1]);
                        if (techthree[0] == "SELA")
                            Ship.ELAA = Convert.ToInt32(techthree[1]);
                    }
                }
                if (Ship.ACM01 != 0)
                {
                    double Ammo = Ship.ACM01 / 10;
                    Ship.ISHA = (int)Math.Truncate(Ammo);
                    Ship.SMBA = (int)Math.Truncate(Ammo);
                }
                Ship.mapId = MapId;
                Ship.ParseConfigs(DamageC1, ShieldC1, SpeedC1, DronesC1, DamageC2, ShieldC2, SpeedC2, DronesC2);

            }
            catch { }
        }

        #endregion
    
    }
}