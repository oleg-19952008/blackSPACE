using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Do.game;

namespace Do.serverGame
{
    /// <summary>
    /// Серверная модель события Spaceball на карте 4-4 (mapId 16).
    ///
    /// Протокол (по клиентской реализации MapEventsAssembly):
    ///   "ssi|<mmo>|<eic>|<vru>|<speed>|<company>"  INIT_SCOREBOARD — клиент
    ///       создаёт окно WINDOW_CLASS_SPACEBALL (3 элемента MMO/EIC/VRU) и
    ///       ставит скорость мяча.
    ///   "ssc|<company>|<score>|<portalID>"         SET_SCORE — очки компании +
    ///       анимация портала (portalManager.playPortalAnimation).
    ///   "sss|<company>|<speed>"                    SET_SPEED — индикатор
    ///       spaceball_speed на scoreboard.
    ///   "f|C|<objId>|442|1||Spaceball|<x>|<y>|<company>|0|0|7||0|0" — спавн
    ///       мяча: ship-pattern id 442 имеет флаг spaceball="true", поэтому
    ///       клиент рисует его как мяч, ставит иконку mapIcon_spaceball на
    ///       миникарте и при клике вызывает ShipManager.selectSpaceball()
    ///       (без пузырей урона). Движение — обычным "0|1|<objId>|<x>|<y>|<angle>".
    /// </summary>
    partial class Map
    {
        public const uint SpaceballBallId = 900;      // id мяча (свободен: NPC < 500, игроки >= 1000)
        public const ushort SpaceballFieldMap = 16;   // карта 4-4
        public const int SpaceballPatternId = 442;    // XML-паттерн с флагом spaceball="true"
        private const int BallSpeed = 1200;           // базовая скорость мяча (для ssi/sss)

        // Ворота трёх компаний: company 1=MMO, 2=EIC, 3=VRU.
        // portalID в ssc — индекс ворот, в которые забит мяч (0..2).
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
        private int _ballSpeed = BallSpeed;
        private int _ballHolder = 0;               // компания, «ведущая» мяч (последний удар)
        // Счёт: всегда ровно 3 компании (1=MMO, 2=EIC, 3=VRU).
        public readonly Dictionary<int, int> SpaceballScore = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 } };

        /// <summary>Запуск события на поле. Идемпотентен.</summary>
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
                    Thread.Sleep(MovementTickMs);
                }
            }
            catch (Exception e)
            {
                Out.WriteLine("Spaceball loop stopped on map " + _id + ": " + e.Message, "Spaceball", ConsoleColor.DarkYellow);
            }
        }

        /// <summary>
        /// Мяч катится к ближайшему игроку; догнав — «получает удар»: летит в
        /// случайную точку поля, а ведущей компанией становится компания игрока
        /// (она же засчитывается при голе в чужие ворота).
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
            int stepPx = Math.Max(200, _ballSpeed / 3);
            if (dist <= stepPx)
            {
                BallX = _ballTargetX;
                BallY = _ballTargetY;
                if (nearest != null && best < 2500)
                {
                    // «Удар»: мяч отлетает в случайную точку поля.
                    _ballTargetX = Program.Random.Next(500, 41100);
                    _ballTargetY = Program.Random.Next(500, 25100);
                    if (nearest.Ship.factionId >= 1 && nearest.Ship.factionId <= 3)
                        _ballHolder = nearest.Ship.factionId;
                }
            }
            else
            {
                var dir = Math.Atan2(_ballTargetY - BallY, _ballTargetX - BallX);
                BallX += (int)(Math.Cos(dir) * stepPx);
                BallY += (int)(Math.Sin(dir) * stepPx);
            }
        }

        private void CheckGoal()
        {
            int goalIdx = 0;
            foreach (var goal in SpaceballGoals)
            {
                if (Distance.Get(goal.Value[0], BallX, goal.Value[1], BallY) >= 400)
                {
                    goalIdx++;
                    continue;
                }
                // Забивает компания, последняя ударившая мяч (или ведущая счёт).
                int scorer = _ballHolder != 0 ? _ballHolder : goal.Key;
                SpaceballScore[scorer] = (SpaceballScore.ContainsKey(scorer) ? SpaceballScore[scorer] : 0) + 1;
                // ssc|company|newScore|portalID — очки + анимация портала.
                Broadcast("ssc|" + scorer + "|" + SpaceballScore[scorer] + "|" + goalIdx);
                Broadcast("0|A|STD|GOAL! Company " + scorer + " scores (" + SpaceballScore[scorer] + ").");
                // Респавн мяча в центр поля.
                BallX = 20800; BallY = 12800;
                _ballTargetX = 20800; _ballTargetY = 12800;
                _ballHolder = 0;
                break;
            }
        }

        /// <summary>
        /// Инициализация события для входящего игрока: scoreboard (ssi), скорость
        /// (sss), мяч как корабль-паттерн 442 (f|C|...|442|...) и его позиция.
        /// Вызывается из AddUser/add_user для карты 16.
        /// </summary>
        public void SendSpaceballTo(uint userId)
        {
            if (_id != SpaceballFieldMap || !Users.ContainsKey(userId)) return;
            var u = Users[userId];
            // 1) INIT_SCOREBOARD: ssi|mmo|eic|vru|speed|companyWithSpeed
            u.Send("ssi|" + SpaceballScore[1] + "|" + SpaceballScore[2] + "|" + SpaceballScore[3] +
                   "|" + _ballSpeed + "|" + (_ballHolder != 0 ? _ballHolder : 1));
            // 2) SET_SPEED: текущая скорость мяча по компаниям (индикаторы).
            foreach (var c in new[] { 1, 2, 3 })
                u.Send("sss|" + c + "|" + (c == (_ballHolder != 0 ? _ballHolder : 1) ? _ballSpeed : 0));
            // 3) Текущие очки (ssc — с анимацией портала не критично на логине).
            int idx = 0;
            foreach (var goal in SpaceballGoals)
            {
                u.Send("ssc|" + goal.Key + "|" + SpaceballScore[goal.Key] + "|" + idx);
                idx++;
            }
            lock (this)
            {
                // 4) Мяч: корабль с паттерном 442 (spaceball="true") — у клиента
                //    включается иконка mapIcon_spaceball и selectSpaceball().
                u.Send("f|C|" + SpaceballBallId + "|" + SpaceballPatternId + "|1||Spaceball|" + BallX + "|" + BallY + "|" +
                       (_ballHolder != 0 ? _ballHolder : 1) + "|0|0|7||0|0");
                u.Send("0|1|" + SpaceballBallId + "|" + BallX + "|" + BallY + "|730");
            }
            StartSpaceball();
        }

        /// <summary>Смена скорости мяча (например, при усиленииleading-компании).</summary>
        public void SetSpaceballSpeed(int speed)
        {
            _ballSpeed = Math.Max(200, speed);
            int leader = _ballHolder != 0 ? _ballHolder : 1;
            Broadcast("sss|" + leader + "|" + _ballSpeed);
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
