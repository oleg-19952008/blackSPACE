using System;
using System.Collections.Generic;
using System.Threading;
using Do.game;

namespace Do.serverGame
{
    /// <summary>
    /// Серверная модель spaceball-поля на стартовой PvP-карте 4-4 (mapId 16).
    ///
    /// Что реализовано:
    ///  - мяч — отдельный объект с id 900, позицией и циклом движения. Клиент
    ///    рисует его по тем же пакетам, что и любое NPC-существо: "f|C|..."
    ///    (появление в зоне видимости), "0|1|..." (перемещение), "f|R|..."
    ///    (удаление). Своя модель физики полёта мяча на клиенте для такого
    ///    объекта отсутствует, поэтому визуально мяч будет выглядеть как существо
    ///    карты — это максимум, что может сделать сервер без правки SWF-клиента.
    ///  - ворота трёх фракций (MMO/EIC/VRU) у краёв карты.
    ///  - счётчик голов каждой фракции рассылается тем же протокольным пакетом,
    ///    что и клиентский UI spaceball ("0|n|ssc|<faction>|<goals>").
    ///
    /// Чего реализовать на сервере НЕВОЗМОЖНО (ограничение клиента):
    /// перетаскивание мяча кораблём, анимация забития ворот, «подхват» мяча. Это
    /// логика Flash-клиента (SWF), которой в этом исходнике нет.
    /// </summary>
    partial class Map
    {
        public const uint SpaceballBallId = 900;      // виртуальный id мяча (свободен: NPC < 500, игроки >= 1000)
        public const ushort SpaceballFieldMap = 16;   // карта 4-4

        private static readonly Dictionary<int, int[]> SpaceballGoals =
            new Dictionary<int, int[]>
            {
                { 1, new[] { 20800, 300 } },     // MMO  (левый край)
                { 2, new[] { 20500, 12500 } },   // EIC  (правый верх)
                { 3, new[] { 300, 12500 } },     // VRU  (левый верх)
            };

        private Thread _spaceballThread;
        private volatile bool _spaceballRunning;
        public int BallX = 20800, BallY = 12800;   // центр поля (граница карты 16 — 41600x25600)
        private int _ballTargetX = 20800, _ballTargetY = 12800;
        public readonly Dictionary<int, int> SpaceballScore = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 } };

        /// <summary>
        /// Запуск мяча на поле. Вызывается из конструктора карты 16 при старте
        /// сервера (когда коллекция Program.Users уже создана) и из обработчика
        /// RDY|MAP при заходе игрока на карту — поток идемпотентен.
        /// </summary>
        public void StartSpaceball()
        {
            if (_id != SpaceballFieldMap || _spaceballRunning) return;
            _spaceballRunning = true;
            _spaceballThread = new Thread(SpaceballLoop) { IsBackground = true };
            _spaceballThread.Start();
        }

        private void SpaceballLoop()
        {
            try
            {
                while (_spaceballRunning)
                {
                    lock (this)
                    {
                        MoveBallStep();
                        CheckGoal();
                    }
                    Broadcast("0|1|" + SpaceballBallId + "|" + BallX + "|" + BallY + "|730");
                    // Периодическая синхронизация счёта — тот же формат, что
                    // рассылается на логине (0|n|ssc|<faction>|<goals>).
                    if (Program.Random.Next(0, 10) == 0)
                    {
                        foreach (var sc in SpaceballScore)
                            Broadcast("0|n|ssc|" + sc.Key + "|" + sc.Value);
                    }
                    Thread.Sleep(MovementTickMs);
                }
            }
            catch (Exception e)
            {
                Out.WriteLine("Spaceball loop stopped on map " + _id + ": " + e.Message, "Spaceball", ConsoleColor.DarkYellow);
            }
        }

        /// <summary>
        /// Мяч «живёт» рядом с самым ближним игроком на поле: катится к нему, а
        /// когда догоняет — отскакивает в случайную точку поля (имитация удара).
        /// </summary>
        private void MoveBallStep()
        {
            User nearest = null;
            double best = double.MaxValue;
            foreach (var u in Users.Values)
            {
                var d = Distance.Get(u.Ship.x, BallX, u.Ship.y, BallY);
                if (d < best) { best = d; nearest = u; }
            }
            if (nearest != null && best < 2500)
            {
                _ballTargetX = nearest.Ship.x;
                _ballTargetY = nearest.Ship.y;
            }
            else if (Program.Random.Next(0, 12) == 0)
            {
                _ballTargetX = Program.Random.Next(500, 41100);
                _ballTargetY = Program.Random.Next(500, 25100);
            }

            var dist = Distance.Get(_ballTargetX, BallX, _ballTargetY, BallY);
            const int ballSpeedPxPerTick = 420;
            if (dist <= ballSpeedPxPerTick)
            {
                BallX = _ballTargetX;
                BallY = _ballTargetY;
                if (nearest != null && best < 2500)
                {
                    // «Удар»: мяч отлетает в случайную точку поля.
                    _ballTargetX = Program.Random.Next(500, 41100);
                    _ballTargetY = Program.Random.Next(500, 25100);
                }
            }
            else
            {
                var dir = Math.Atan2(_ballTargetY - BallY, _ballTargetX - BallX);
                BallX += (int)(Math.Cos(dir) * ballSpeedPxPerTick);
                BallY += (int)(Math.Sin(dir) * ballSpeedPxPerTick);
            }
        }

        private void CheckGoal()
        {
            foreach (var goal in SpaceballGoals)
            {
                if (Distance.Get(goal.Value[0], BallX, goal.Value[1], BallY) < 400)
                {
                    SpaceballScore[goal.Key] = SpaceballScore.ContainsKey(goal.Key) ? SpaceballScore[goal.Key] + 1 : 1;
                    Broadcast("0|A|STD|GOAL! Faction " + goal.Key + " scores (" + SpaceballScore[goal.Key] + ").");
                    foreach (var sc in SpaceballScore)
                        Broadcast("0|n|ssc|" + sc.Key + "|" + sc.Value);
                    // Респавн мяча в центр поля.
                    BallX = 20800; BallY = 12800;
                    _ballTargetX = 20800; _ballTargetY = 12800;
                    break;
                }
            }
        }

        /// <summary>
        /// Показ мяча и ворот конкретному входящему игроку (по формату пакетов
        /// NPC: f|C|<id>|<typeId>|... ). Вызывается из AddUser/add_user для карты 16.
        /// </summary>
        public void SendSpaceballTo(uint userId)
        {
            if (_id != SpaceballFieldMap || !Users.ContainsKey(userId)) return;
            lock (this)
            {
                Users[userId].Send("f|C|" + SpaceballBallId + "|80|1||Spaceball|"+ BallX + "|" + BallY + "||-1|0|0|0|0");
                Users[userId].Send("0|1|" + SpaceballBallId + "|" + BallX + "|" + BallY + "|730");
            }
            foreach (var goal in SpaceballGoals)
            {
                Users[userId].Send("f|C|" + (800 + goal.Key) + "|80|1||Gate " + goal.Key + "|" + goal.Value[0] + "|" + goal.Value[1] + "||-1|0|0|0|0");
                Users[userId].Send("0|n|ssc|" + goal.Key + "|" + (SpaceballScore.ContainsKey(goal.Key) ? SpaceballScore[goal.Key] : 0));
            }
            StartSpaceball();
        }

        private void Broadcast(string packet)
        {
            foreach (var pair in Users)
            {
                try { pair.Value.Send(packet); } catch { }
            }
        }
    }
}
