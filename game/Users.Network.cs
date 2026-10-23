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
 * Partial-class User: Приём пакетов из сокета: авторизация, разбор игровых пакетов (метод ReadCallback).
 */
partial class User
{
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

                            var x = Convert.ToInt32(userRow["x"]);
                            var y = Convert.ToInt32(userRow["y"]);
                            var hp = Program.Users[UserId].Ship.HP;
                            var shd = Program.Users[UserId].Ship.shield;
                            CurrentConfig = Program.Users[UserId].CurrentConfig;
                            // Восстанавливаем карту из БД (последнее место, где был игрок),
                            // а не с новой пустой MapId=1 этого объекта — иначе при re-login
                            // игрока выбрасывало на первую карту.
                            MapId = Convert.ToUInt16(userRow["mapId"]);
                            if (!Program.Maps.ContainsKey(MapId)) MapId = Program.Users[UserId].MapId;
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
                    else if (_userAuth)
                    {

                        if (packetHeader.StartsWith("PNG"))
                        {
                            _socketClosed = false;
                            LastPing = Program.GetUnixTimestamp;

                        }
                        else if (packetHeader.StartsWith("RDY"))
                        {
                            // Клиент присылает "RDY|MAP" когда полностью загрузил карту.
                            // Раньше пакет игнорировался; из-за этого, если на момент
                            // AddUser словарь Program.Users ещё не содержал игрока,
                            // spaceball-пакеты (ssi/sss/ssc/f|C|900) не отправлялись
                            // вообще — ни мяча, ни окна scoreboard. Повторяем выдачу
                            // события при готовности карты (метод идемпотентен).
                            if (MapId == Do.serverGame.Map.SpaceballFieldMap &&
                                Program.Maps.ContainsKey(MapId))
                            {
                                Program.Maps[MapId].SendSpaceballTo(UserId);
                            }
                        }

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

                            // Spaceball: прямое нажатие ЛКМ по мячу — включаем атаку,
                            // дальнейшую обработку ведёт Alienattack (ветка Spaceball).
                            if (Ship.selectedUserId == Do.serverGame.Map.SpaceballBallId && MapId == Do.serverGame.Map.SpaceballFieldMap)
                            {
                                if (Ship.damage == 0)
                                {
                                    Send("0|A|STD|You don't have any lasers equipped.");
                                }
                                else if (!CheckLaser())
                                {
                                    Send("0|A|STD|Aborting attack. Not enough laser ammo for an attack.");
                                }
                                else
                                {
                                    Ship.isAttacking = true;
                                    AttackId = Ship.selectedUserId;
                                }
                            }
                            else if (Ship.selectedUserId != 0)
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
                        else if (packetHeader.StartsWith("7"))
                        {
                            Setting.AssembleSettings(content.Split('|'));
                        }

                        else if (packetHeader.StartsWith("A"))
                        {
                            Setting.AssembleSettings(content.Split('|'));
                        }
                        else if (packetHeader.StartsWith("SD"))
                        {
                            if (Ship.CanUseAbility)
                            {
                                UseAbility();
                            }
                        }

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
                        else if (packetHeader.StartsWith("x"))
                        {
                            var id = packetParser.GetUInt();
                            CollectBox(id);

                        }
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
                                // Админский прыжок на любую карту: пакет вида "map|<id>".
                                var map_id = packetParser.GetUInt();

                                if (!Program.Maps.ContainsKey((ushort)map_id))
                                {
                                    Send("0|A|STD|Карта " + map_id + " не найдена. Доступные: " +
                                         string.Join(", ", Program.Maps.Keys));
                                    return;
                                }

                                var oldMapId = (uint)MapId;
                                if (oldMapId == map_id)
                                {
                                    Send("0|A|STD|Ты уже на карте " + map_id);
                                    return;
                                }

                                try
                                {
                                    // Сбрасываем выделение/атаку и убираем юзера со старой карты.
                                    Ship.selectedUserId = 0;
                                    Ship.isAttacking = false;
                                    Ship.selected1 = false;
                                    PlayersInRange = new List<uint>();
                                    PlayersInRange.Add(UserId);

                                    // Позиция после прыжка: центр новой карты. Иначе
                                    // серверные координаты оставались от старой карты,
                                    // клиент после перезагрузки рисовал корабль в центре,
                                    // и проверка дистанции до портала ("слишком далеко")
                                    // использовала именно старые координаты.
                                    // Координаты центра стандартной границы мира (20800x12800).
                                    var newX = 10400;
                                    var newY = 6400;

                                    if (Program.Maps.ContainsKey((ushort)oldMapId))
                                        Program.Maps[(ushort)oldMapId].RemoveUser(UserId);

                                    // Меняем карту и добавляем юзера на новую.
                                    MapId = (ushort)map_id;
                                    Ship.x = newX;
                                    Ship.y = newY;
                                    Ship.destinationx = newX;
                                    Ship.destinationy = newY;
                                    Ship.teleporting = 0;
                                    Ship.teleporting2 = 0;
                                    Ship.logout3 = 0;
                                    Ship.buclemover = false;
                                    Ship.teleported = false;
                                    Ship.teleport1 = false;

                                    // Команда клиенту перезагрузить игровую карту
                                    // (тот же механизм, что у порталов: "0|U|<mapId>|<id>",
                                    // где id=0 означает админский переход). Без этого
                                    // пакета клиент менял только миникарту-картинку, но
                                    // продолжал слать координаты в границах старой карты:
                                    // движение было, а по миникарте корабль "стоял",
                                    // телепорт считался недоступным.
                                    Send("0|U|" + map_id + "|0");

                                    Program.Maps[(ushort)map_id].AddUser(UserId);

                                    // Перезапуск цикла движения уже на новой карте:
                                    // старый цикл завершился при RemoveUser со старой
                                    // карты (Users.ContainsKey == false), без этого
                                    // сервер переставал интерполировать позицию.
                                    Program.Maps[(ushort)map_id].Move(UserId);

                                    Send("0|A|STD|Прыгнул на карту " + map_id);
                                    Out.WriteLine("[Admin] User " + UserId + " (" + Ship.Name +
                                                  ") jumped map " + oldMapId + " -> " + map_id,
                                                  "Admin.Map", ConsoleColor.Cyan);
                                }
                                catch (Exception ex)
                                {
                                    Send("0|A|STD|Ошибка прыжка: " + ex.Message);
                                    Out.WriteLine("[Admin] map jump FAILED user " + UserId +
                                                  " " + oldMapId + " -> " + map_id + ": " +
                                                  ex.Message + "\n" + ex.StackTrace,
                                                  "Admin.Map", ConsoleColor.Red);
                                }

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

}
}
