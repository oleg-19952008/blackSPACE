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
 * Partial-class User: Смена конфигов корабля, регенерация HP/щитов, выбор цели, проверка пинга, Spaceball; рестарт сервера (Close/Close5).
 */
partial class User
{
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
            // Spaceball: мяч не NPC, но выделяется как ship-объект (selectSpaceball).
            if (userId == Do.serverGame.Map.SpaceballBallId && MapId == Do.serverGame.Map.SpaceballFieldMap &&
                Program.Maps.ContainsKey(MapId))
            {
                Ship.selectedUserId = userId;
                Send("0|N|" + userId + "|Spaceball|1|1|1|1");
                return;
            }
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
        // СОХРАНЕНИЕ ПОСЛЕДНЕЙ ПОЗИЦИИ (карта + координаты) во время игры.
        // Вызывается из фонового цикла UpdateShip_ (каждые 5 секунд). Без этого
        // mapId/x/y в БД обновлялись только по явным путям дисконнекта
        // (discconectUser(1)/(-1)), которые при обычном выходе клиента из игры
        // часто не срабатывали (сокет висел до пинг-таймаута, либо сервер падал) —
        // в итоге игрока при следующем входе выбрасывало на старую карту.
        private ushort _lastSavedMapId = ushort.MaxValue;
        private int _lastSavedX = int.MinValue;
        private int _lastSavedY = int.MinValue;

        public void SavePositionToDatabase()
        {
            try
            {
                ushort currentMap = MapId;
                int x = Ship.x, y = Ship.y;

                // Валидация: не пишем в БД мусор (несуществующая карта или нулевые
                // координаты до инициализации корабля) — иначе при следующем входе
                // игрока выбрасывало бы на битую позицию.
                if (!Program.Maps.ContainsKey(currentMap)) return;
                if (x <= 0 && y <= 0) return;

                // Пишем в БД только при реальном изменении позиции — иначе каждые
                // 5 секунд шли бы бесполезные UPDATE.
                if (currentMap == _lastSavedMapId && x == _lastSavedX && y == _lastSavedY) return;

                using (var dbClient = Program.Manager.getQueryreactor())
                {
                    dbClient.query("UPDATE server_1_players SET mapId = '" + currentMap + "', x = '" + x + "', y = '" + y + "' WHERE playerID = '" + UserId + "'");
                }

                _lastSavedMapId = currentMap;
                _lastSavedX = x;
                _lastSavedY = y;
            }
            catch (Exception e)
            {
                Out.WriteLine("SavePositionToDatabase failed for user " + UserId + ": " + e.Message,
                              "User.DbSync", ConsoleColor.DarkYellow);
            }
        }

        /// <summary>
        /// Стартовая точка возрождения игрока: центр безопасной зоны его фракции
        /// (MMO 1-1, EIC 5-1, VRU 9-9). Используется при гибели, чтобы игрока
        /// возвращало на стартовую карту, а не оставляло там, где он умер.
        /// </summary>
        public bool TryGetHomeSpawn(out ushort mapId, out int x, out int y)
        {
            mapId = 0; x = 0; y = 0;
            var st = FindSpaceballStation(Ship.factionId, true);
            if (st == null || !Program.Maps.ContainsKey((ushort)st[3])) return false;
            mapId = (ushort)st[3];
            // Небольшой разброс вокруг центра базы, чтобы все умершие не спавнились
            // точкой в точку (координаты валидны: база в центре карты, ±50 px).
            x = st[1] + Program.Random.Next(-50, 50);
            y = st[2] + Program.Random.Next(-50, 50);
            return true;
        }

        public int[] GetXY()
        {
            // СОХРАНЕНИЕ ПОСЛЕДНЕЙ ПОЗИЦИИ: раньше здесь координаты и карта
            // принудительно заменялись на станцию фракции (SpaceballStations),
            // поэтому в БД всегда писались {1000,1000,map=1} и игрока при следующем
            // входе выбрасывало на первую карту/базу, независимо от того, где он
            // был на самом деле. Теперь возвращаем фактические текущие координаты и
            // карту — сервер их корректно сохраняет ("UPDATE ... SET x,y,mapId"), а
            // точка респауна после гибели по-прежнему считается через
            // FindSpaceballStation в месте респауна (Users.Ship.cs / SetSpaceball).
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
                // Подтверждение перехода для клиента + актуальные счётчики
                // станций (spaceball), чтобы кнопка не «зависала» после прыжка.
                Send("0|m|" + MapId + "|" + Ship.x + "|" + Ship.y);
                // Реальные ворота — ДАЛЬНИЕ точки поля 4-4 (MMO {6035|12998},
                // EIC {28029|3034}, VRU {28006|24074}); стартовые координаты
                // станций фракций к логике гола отношения не имеют. Актуальное
                // табло (полные ssi/sss/ssc) игроку разошлёт AddUser ->
                // SendSpaceballTo выше; усечённые ssc-заглушки убраны.
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

                    // reason==1 — ГИБЕЛЬ корабля. В отличие от обычного выхода,
                    // здесь сохраняем не координаты места гибели, а СТАРТОВУЮ
                    // карту фракции: умерший игрок должен возрождаться на базе,
                    // а не там, где его убили.
                    ushort deathHomeMap; int deathHomeX, deathHomeY;
                    if (TryGetHomeSpawn(out deathHomeMap, out deathHomeX, out deathHomeY))
                    {
                        Ship.x = deathHomeX;
                        Ship.y = deathHomeY;
                        MapId = deathHomeMap;
                        Ship.destroyed = false;
                        Ship.HP = Ship.maxHP;
                        Ship.shield = Ship.maxShield;
                        _lastSavedMapId = deathHomeMap;
                        _lastSavedX = deathHomeX;
                        _lastSavedY = deathHomeY;
                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_players SET x = '" + deathHomeX + "',y = '" + deathHomeY + "',mapId = '" + deathHomeMap + "' WHERE playerID = '" +
                                           UserId + "'");
                        }
                    }
                    else
                    {
                        var update = GetXY();

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_players SET x = '" + update[0] + "',y = '" + update[1] + "',mapId = '" + update[2] + "' WHERE playerID = '" +
                                       UserId + "'");
                        }
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

}
}
