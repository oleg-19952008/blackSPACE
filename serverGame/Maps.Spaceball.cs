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
    /// Механика:
    ///  - Мяч — корабль-паттерн 442 (spaceball="true"), компания мяча = 4 (нейтральная).
    ///  - Мяч НЕ двигается сам за игроками. Он стоит в центре поля, пока его не
    ///    ударят: игрок стреляет по мячу, и в зависимости от корпорации игрока
    ///    (1=MMO, 2=EIC, 3=VRU) мяч летит В СТОРОНУ ВОРОТ этой корпорации.
    ///  - Скорость полёта зависит от суммы урона: чем больше урона нанесли,
    ///    тем быстрее летит мяч.
    ///  - Если мяч долетает до ворот корпорации X — очко получает следующая по
    ///    кругу корпорация (X%3+1): чтобы забить, нужно «пробить» ворота соперника.
    ///    После гола мяч респавнится в центре, счёт обнуляется у всех скорингов
    ///    только через ssc.
    ///
    /// Протокол (MapEventsAssembly клиента):
    ///   "ssi|<mmo>|<eic>|<vru>|<speed>|<company>"  INIT_SCOREBOARD
    ///   "ssc|<company>|<newScore>|<portalID>"      SET_SCORE + анимация портала
    ///   "sss|<company>|<speed>"                    SET_SPEED (индикатор скорости)
    ///   "f|C|<objId>|442|1||Spaceball|<x>|<y>|4|0|0|7||0|0" — спавн мяча
    ///   "0|1|<objId>|<x>|<y>|<angle>"              moveShip (обычное движение)
    /// </summary>
    partial class Map
    {
        public const uint SpaceballBallId = 900;      // id мяча (свободен: NPC < 500, игроки >= 1000)
        public const ushort SpaceballFieldMap = 16;   // карта 4-4
        public const int SpaceballPatternId = 442;    // XML-паттерн с флагом spaceball="true"
        public const int SpaceballCompanyId = 4;      // у мяча своей корпорации нет — нейтральная 4
        private const int BallBaseSpeed = 800;        // скорость при минимальном ударе
        private const int BallMaxSpeed = 6000;        // потолок скорости мяча

        // Ворота трёх компаний: company 1=MMO, 2=EIC, 3=VRU.
        // portalID в ssc — индекс ворот, в которые забит мяч (0..2).
        private static readonly Dictionary<int, int[]> SpaceballGoals =
            new Dictionary<int, int[]>
            {
                { 1, new[] { 20800, 300 } },     // MMO  (нижний край)
                { 2, new[] { 20500, 12500 } },   // EIC  (правый верх)
                { 3, new[] { 300, 12500 } },     // VRU  (левый верх)
            };

        private Thread _spaceballThread;
        private volatile bool _spaceballRunning;
        public int BallX = 20800, BallY = 12800;   // центр поля (граница карты 16 — 41600x25600)
        private int _ballVX = 0, _ballVY = 0;      // вектор скорости мяча (px/сек)
        private int _ballSpeed = 0;                // текущая скорость (0 = мяч стоит)
        private int _ballHitter = 0;               // корпорация, последним ударившая мяч
        private double _ballDamage = 0;            // накопленный урон текущего полёта -> скорость
        private int _ballAngle = 730;              // визуальный угол движения (для moveShip)
        // Игроки, которым виден мяч (аналог PlayersInRange у NPC).
        public readonly HashSet<uint> BallViewers = new HashSet<uint>();
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

        /// <summary>
        /// Динамическая видимость мяча (как у NPC): при входе игрока на карту
        /// отправляем спавн, если он в радиусе видимости, и снимаем видимость
        /// при выходе. Вызывается из UpdateAlienVisibility / RemoveUser.
        /// </summary>
        public void UpdateBallVisibility(uint userId, bool show)
        {
            if (_id != SpaceballFieldMap) return;
            lock (this)
            {
                if (show)
                {
                    if (BallViewers.Contains(userId)) return;
                    BallViewers.Add(userId);
                    if (!Users.ContainsKey(userId)) return;
                    Users[userId].Send("f|C|" + SpaceballBallId + "|" + SpaceballPatternId + "|1||Spaceball|" + BallX + "|" + BallY + "|" +
                                       SpaceballCompanyId + "|0|0|7||0|0");
                    Users[userId].Send("0|1|" + SpaceballBallId + "|" + BallX + "|" + BallY + "|" + _ballAngle);
                }
                else
                {
                    if (!BallViewers.Remove(userId)) return;
                    if (Users.ContainsKey(userId)) Users[userId].Send("f|R|" + SpaceballBallId);
                }
            }
        }

        private void SpaceballLoop()
        {
            try
            {
                while (_spaceballRunning)
                {
                    lock (this)
                    {
                        if (_ballSpeed > 0)
                        {
                            MoveBallStep();
                            CheckGoal();
                        }
                        if (_ballSpeed > 0)
                        {
                            // Шлём движение только тем, кому мяч виден.
                            foreach (var uid in BallViewers.ToList())
                                if (Users.ContainsKey(uid))
                                    try { Users[uid].Send("0|1|" + SpaceballBallId + "|" + BallX + "|" + BallY + "|" + _ballAngle); } catch { }
                        }
                    }
                    Thread.Sleep(MovementTickMs);
                }
            }
            catch (Exception e)
            {
                Out.WriteLine("Spaceball loop stopped on map " + _id + ": " + e.Message, "Spaceball", ConsoleColor.DarkYellow);
            }
        }

        /// <summary>Шаг полёта мяча по инерции (без самонаведения на игроков).</summary>
        private void MoveBallStep()
        {
            double dt = MovementTickMs / 1000.0;
            BallX += (int)(_ballVX * dt);
            BallY += (int)(_ballVY * dt);

            // Мяч вылетел за пределы поля или долетел «в никуда» — возврат в центр.
            if (BallX < 0 || BallX > 41600 || BallY < 0 || BallY > 25600)
            {
                ResetBall();
                Broadcast("0|1|" + SpaceballBallId + "|" + BallX + "|" + BallY + "|" + _ballAngle);
            }
        }

        private void CheckGoal()
        {
            int goalIdx = 0;
            foreach (var goal in SpaceballGoals)
            {
                if (Distance.Get(goal.Value[0], BallX, goal.Value[1], BallY) >= 800)
                {
                    goalIdx++;
                    continue;
                }
                // Забито в ворота компании goal.Key: очко получает следующая по
                // кругу корпорация (команда атакующего «пробивает» чужие ворота).
                int scorer = goal.Key % 3 + 1;
                SpaceballScore[scorer] = (SpaceballScore.ContainsKey(scorer) ? SpaceballScore[scorer] : 0) + 1;
                // ssc|company|newScore|portalID — очки + анимация портала.
                Broadcast("ssc|" + scorer + "|" + SpaceballScore[scorer] + "|" + goalIdx);
                Broadcast("0|A|STD|GOAL! Company " + scorer + " scores (" + SpaceballScore[scorer] + ").");
                ResetBall();
                Broadcast("0|1|" + SpaceballBallId + "|" + BallX + "|" + BallY + "|" + _ballAngle);
                break;
            }
        }

        private void ResetBall()
        {
            BallX = 20800; BallY = 12800;
            _ballVX = 0; _ballVY = 0;
            _ballSpeed = 0;
            _ballHitter = 0;
            _ballDamage = 0;
            Broadcast("sss|1|0");
            Broadcast("sss|2|0");
            Broadcast("sss|3|0");
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
            int leader = _ballHitter != 0 ? _ballHitter : 1;
            // 1) INIT_SCOREBOARD: ssi|mmo|eic|vru|speed|companyWithSpeed
            u.Send("ssi|" + SpaceballScore[1] + "|" + SpaceballScore[2] + "|" + SpaceballScore[3] +
                   "|" + _ballSpeed + "|" + leader);
            // 2) SET_SPEED: индикаторы (скорость показывает та, кто держит мяч).
            foreach (var c in new[] { 1, 2, 3 })
                u.Send("sss|" + c + "|" + (c == leader && _ballSpeed > 0 ? _ballSpeed : 0));
            // 3) Текущие очки.
            int idx = 0;
            foreach (var goal in SpaceballGoals)
            {
                u.Send("ssc|" + goal.Key + "|" + SpaceballScore[goal.Key] + "|" + idx);
                idx++;
            }
            // Спавн/видимость мяча обрабатывает UpdateBallVisibility (по радиусу).
            StartSpaceball();
        }

        /// <summary>
        /// «Удар» по мячу: вызывается из боевого кода при попадании по Spaceball.
        /// Урон накапливается; направление — к воротам корпорации атакующего;
        /// скорость полёта растёт с суммарным уроном.
        /// </summary>
        public void HitSpaceball(int attackerCompany, double damage, int fromX, int fromY)
        {
            if (_id != SpaceballFieldMap) return;
            if (attackerCompany < 1 || attackerCompany > 3) return;
            lock (this)
            {
                _ballHitter = attackerCompany;
                _ballDamage += Math.Max(1, damage);
                // Скорость: базовая + прирост от накопленного урона, с потолком.
                _ballSpeed = Math.Min(BallMaxSpeed, BallBaseSpeed + (int)_ballDamage / 2);

                int[] goal;
                if (!SpaceballGoals.TryGetValue(attackerCompany, out goal)) return;
                double dx = goal[0] - BallX, dy = goal[1] - BallY;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 1) len = 1;
                _ballVX = (int)(dx / len * _ballSpeed);
                _ballVY = (int)(dy / len * _ballSpeed);
                // Клиентский угол (градусы*10, смещённый на 180° — как в moveShip).
                _ballAngle = ((int)(Math.Atan2(-dy, dx) * 180.0 / Math.PI) + 360) % 360 + 180;

                // Индикатор скорости на scoreboard — у корпорации-ударившего.
                Broadcast("sss|" + attackerCompany + "|" + _ballSpeed);
                foreach (var c in new[] { 1, 2, 3 })
                    if (c != attackerCompany) Broadcast("sss|" + c + "|0");
            }
            // Sofort показываем новый вектор/позицию.
            Broadcast("0|1|" + SpaceballBallId + "|" + BallX + "|" + BallY + "|" + _ballAngle);
        }

        /// <summary>Смена скорости мяча (например, из админ-команд).</summary>
        public void SetSpaceballSpeed(int speed)
        {
            _ballSpeed = Math.Max(0, speed);
            int leader = _ballHitter != 0 ? _ballHitter : 1;
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
