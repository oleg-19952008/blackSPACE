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
    ///  - Мяч — корабль-паттерн 442 (spaceball="true"), компания мяча = 4 (нейтральная),
    ///    виден ВСЕМ игрокам поля без радиуса видимости.
    ///  - Мяч НЕ двигается сам и НЕ притягивается к игрокам. Он стоит в центре,
    ///    пока по нему не начнут наносить урон.
    ///  - Направление полёта — К ВОРОТАМ корпорации атакующего (1=MMO, 2=EIC, 3=VRU):
    ///    игрок «бьёт» мяч в сторону своих ворот, чтобы пробить их (гол засчитывается
    ///    владельцу пробитых ворот). Вектор задаётся при ударе (прямой полёт);
    ///    удары «в бок» доворачиваются на створ — см. HitSpaceball/AimAtGoal.
    ///  - Баланс урона: 3000 — малый, 75000 — мощный. Мяч трогается при накопленном
    ///    уроне ~12000 (BallMoveThreshold) со скорости корабля (~450 px/s);
    ///    при 75000+ — максимум (~600 px/s), сопоставимо с быстро летящим
    ///    кораблём (игроки летают 500-600) — мяч можно догнать.
    ///    Если по мячу долго не бьют, урон «остывает» и мяч останавливается.
    ///  - Гол: ssc|company|newScore|portalID рассылаться должен ТАКЖЕ как "0|ssc|..."
    ///    (см. SendEvent — клиент парсит события только с префиксом owner-фрейма).
    ///
    /// Протокол (MapEventsAssembly клиента):
    ///   "ssi|<mmo>|<eic>|<vru>|<speed>|<companyWithSpeed>"  INIT_SCOREBOARD
    ///   "ssc|<company>|<newScore>|<portalID>"               SET_SCORE + анимация портала
    ///   "sss|<company>|<speed>"                             SET_SPEED (индикатор скорости)
    ///   "f|C|<objId>|442|1||Spaceball|<x>|<y>|4|0|0|7||0|0" — спавн мяча
    ///   "0|1|<objId>|<x>|<y>|<angle>"                       moveShip (обычное движение)
    /// </summary>
    partial class Map
    {
        public const uint SpaceballBallId = 900;      // id мяча (свободен: NPC < 500, игроки >= 1000)
        public const ushort SpaceballFieldMap = 16;   // карта 4-4
        public const int SpaceballPatternId = 442;    // XML-паттерн с флагом spaceball="true"
        public const int SpaceballCompanyId = 4;      // у мяча своей корпорации нет — нейтральная 4

        // Баланс: 3000 — малый урон, 75000 — мощный.
        private const double BallMoveThreshold = 12000;     // урон, при котором мяч трогается
        private const int BallMinSpeed = 450;               // скорость на пороге (~12k): как корабль
        private const int BallMaxSpeed = 600;               // потолок (на ~75k+): не быстрее 600 —
                                                            // игрок на 500-600 может догнать мяч
        private const double BallDamageDecayPerSec = 2500;  // остывание урона при простое
        private const int GoalRadius = 1200;                // радиус створа ворот

        // Центр поля; границы карты 16 — 41600 x 25600.
        private const int FieldCenterX = 20800, FieldCenterY = 12800;
        private const int FieldWidth = 41600, FieldHeight = 25600;

        // Ворота трёх компаний: company 1=MMO, 2=EIC, 3=VRU.
        // Координаты сняты с реальных порталов события на карте 4-4 (клиент
        // показывает их игроку — туда и должен катиться мяч):
        //   VRU — верх (28006|24074), MMO — лево (6035|12998), EIC — низ (28029|3034).
        // Ранее стояли произвольные точки («угловые», потом середины сторон
        // поля) — мяч катился мимо видимых клиентом ворот, игрок видел «в бок».
        // portalID в ssc — индекс ворот (0..2), соответствуют ключу.
        private static readonly Dictionary<int, int[]> SpaceballGoals =
            new Dictionary<int, int[]>
            {
                { 1, new[] { 6035, 12998 } },    // MMO  (левые ворота)
                { 2, new[] { 28029, 3034 } },    // EIC  (нижние ворота)
                { 3, new[] { 28006, 24074 } },   // VRU  (верхние ворота)
            };

        // Стабильный порядок ворот для portalID в ssc (индекс 0..2 по ключу).
        // Dictionary<int,...> перебирается в порядке вставки, но явно задаём
        // массив, чтобы portalID не «поехал» при рефакторинге словаря.
        private static readonly int[] SpaceballPortalOrder = { 1, 2, 3 };

        private Thread _spaceballThread;
        private volatile bool _spaceballRunning;
        public int BallX = FieldCenterX, BallY = FieldCenterY; // центр поля
        private int _ballVX = 0, _ballVY = 0;      // вектор скорости мяча (px/сек)
        private int _ballSpeed = 0;                // текущая скорость (0 = мяч стоит)
        private int _ballHitter = 0;               // корпорация, последним ударившая мяч
        private double _ballDamage = 0;            // накопленный урон -> скорость
        private long _lastHitMs = 0;               // время последнего удара (Environment.TickCount)
        private int _prevBallX = FieldCenterX, _prevBallY = FieldCenterY; // позиция прошлого тика
        // Игроки, которым показан мяч (f|C отправлен один раз).
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
        /// Показ мяча игроку. Spaceball — событие поля 4-4: мяч виден всем
        /// игрокам карты независимо от дистанции. Повторный вызов для уже
        /// видимого игрока ничего не делает (нет дублирующего f|C).
        /// </summary>
        public void UpdateBallVisibility(uint userId, bool show = true)
        {
            if (_id != SpaceballFieldMap) return;
            lock (this)
            {
                if (show)
                {
                    if (BallViewers.Contains(userId)) return;
                    User u;
                    if (!Program.Users.TryGetValue(userId, out u)) return;
                    BallViewers.Add(userId);
                    u.Send("f|C|" + SpaceballBallId + "|" + SpaceballPatternId + "|1||Spaceball|" + BallX + "|" + BallY + "|" +
                           SpaceballCompanyId + "|0|0|7||0|0");
                    // Пакет движения: 0|1|<objId>|<x>|<y>|<msTime> — время анимации,
                    // НЕ угол (см. InterpolatePosition / AlienMovement).
                    u.Send("0|1|" + SpaceballBallId + "|" + BallX + "|" + BallY + "|" + BallMoveTime());
                }
                else
                {
                    if (!BallViewers.Remove(userId)) return;
                    User uu;
                    if (Program.Users.TryGetValue(userId, out uu))
                        uu.Send("f|R|" + SpaceballBallId);
                }
            }
        }

        private void SpaceballLoop()
        {
            try
            {
                while (_spaceballRunning)
                {
                    bool moved = false;
                    lock (this)
                    {
                        // Остывание урона: долго не бьют — мяч останавливается.
                        if (_ballDamage > 0 && Environment.TickCount - _lastHitMs > 2000)
                        {
                            _ballDamage -= BallDamageDecayPerSec * (MovementTickMs / 1000.0);
                            ApplyBallVelocity();
                        }

                        if (_ballSpeed > 0)
                        {
                            MoveBallStep();   // может засчитать гол и сделать ResetBall
                            moved = _ballSpeed > 0;
                        }
                    }
                    // Рассылаем позицию ПОСЛЕ выхода из lock: CheckGoal -> SendEvent
                    // захватывает тот же монитор из другого потока иначе получил бы
                    // deadlock, цикл встал, табло перестало обновляться.
                    if (moved) BroadcastMove();
                    Thread.Sleep(MovementTickMs);
                }
            }
            catch (Exception e)
            {
                Out.WriteLine("Spaceball loop stopped on map " + _id + ": " + e.Message, "Spaceball", ConsoleColor.DarkYellow);
            }
        }

        /// <summary>
        /// Пересчёт скорости/вектора из накопленного урона.
        /// Ниже порога — мяч стоит; выше — линейный рост до потолка.
        /// Вектор задаётся ОДИН раз при ударе (прямой полёт, как в футболе):
        /// игрок направляет мяч позицией вокруг него и подправляет новым ударом.
        /// </summary>
        private void ApplyBallVelocity()
        {
            if (_ballDamage < BallMoveThreshold || _ballHitter == 0)
            {
                if (_ballDamage < 0) _ballDamage = 0;
                _ballSpeed = 0;
                _ballVX = 0; _ballVY = 0;
                return;
            }
            double t = (_ballDamage - BallMoveThreshold) / (75000.0 - BallMoveThreshold); // 0..1
            if (t > 1.0) t = 1.0;
            _ballSpeed = BallMinSpeed + (int)((BallMaxSpeed - BallMinSpeed) * t);

            int[] goal;
            if (!SpaceballGoals.TryGetValue(_ballHitter, out goal)) { _ballSpeed = 0; return; }
            AimAtGoal(goal);
        }

        /// <summary>
        /// Наводка вектора на ворота с дробовой (double) точностью.
        /// ВАЖНО: раньше координаты ворот и мяча были целыми, вектор усреднял
        /// направление через (int)-деление — при dy = 0 мяч «летел просто в бок»
        /// вдоль стороны поля мимо створа. Здесь расписание остаётся дробным,
        /// а округляем мы только саму скорость-вектор целиком.
        /// Дистанция до створа > GoalRadius — иначе AimAtGoal из HitSpaceball
        /// вызывается рекурсивно (ApplyBallVelocity -> AimAtGoal -> AimAtGoal),
        /// когда игрок стоит вплотную к своим воротам (мяч уже в створе).
        /// </summary>
        private void AimAtGoal(int[] goal)
        {
            double dx = goal[0] - (double)BallX, dy = goal[1] - (double)BallY;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < GoalRadius) { _ballVX = 0; _ballVY = 0; return; }
            _ballVX = (int)Math.Round(dx / len * _ballSpeed);
            _ballVY = (int)Math.Round(dy / len * _ballSpeed);
        }

        /// <summary>
        /// Время перемещения для пакета "0|1|...|<msTime>" (как в InterpolatePosition:
        /// расстояние / скорость * 1000). Клиент интерполирует мяч за это время —
        /// мяч летет ПЛАВНО по линии к воротам, а не телепортируется рывками.
        /// </summary>
        private int BallMoveTime()
        {
            double dx = BallX - _prevBallX, dy = BallY - _prevBallY;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (_ballSpeed <= 0 || dist < 1) return MovementTickMs; // стоя на месте — короткий кадр
            int t = (int)Math.Round(dist / _ballSpeed * 1000.0);
            if (t < 50) t = 50;
            if (t > 2000) t = 2000;
            return t;
        }

        /// <summary>Шаг полёта мяча к воротам.</summary>
        private void MoveBallStep()
        {
            // ВНИМАНИЕ: здесь НЕ перенаводится вектор на ворота каждый тик!
            // Раньше наводка шла с точностью до целого (int)-пикселя: когда мяч
            // поравнялся с осью ворот (например dy = 0 при полёте к правым воротам
            // EIC), вектор становился строго горизонтальным и мяч «летел просто
            // в бок», проходя вдоль стороны поля мимо GoalRadius. Теперь вектор
            // задаётся один раз при ударе (ApplyBallVelocity) — как настоящий
            // удар в футболе: мяч летит по прямой, игрок может подправить
            // направление новым ударом.
            _prevBallX = BallX; _prevBallY = BallY; // позиция до этого шага — для интерполяции клиента
            double dt = MovementTickMs / 1000.0;
            BallX += (int)Math.Round(_ballVX * dt);
            BallY += (int)Math.Round(_ballVY * dt);

            CheckGoal();

            // Мяч вылетел за пределы поля — возврат в центр.
            if (BallX < 0 || BallX > FieldWidth || BallY < 0 || BallY > FieldHeight)
            {
                ResetBall();
            }
        }

        private void CheckGoal()
        {
            // КОПИЯ словаря ворот: ниже под lock(this) вызывается ResetBall ->
            // BroadcastMove, который может выкинуть отвалившегося игрока из
            // Users; если бы в этот момент мы шли по SpaceballGoals, а другой
            // поток менял словарь — коллекция была бы «изменена» и цикл упал
            // бы с InvalidOperationException (гол не засчитан, табло молчит).
            foreach (var goal in SpaceballGoals.ToList())
            {
                int idx = Array.IndexOf(SpaceballPortalOrder, goal.Key); // стабильный portalID
                if (Distance.Get(goal.Value[0], BallX, goal.Value[1], BallY) >= GoalRadius)
                    continue;

                // Пробиты ворота компании goal.Key — очко ЕЙ.
                int scorer = goal.Key;
                SpaceballScore[scorer] = SpaceballScore.ContainsKey(scorer) ? SpaceballScore[scorer] + 1 : 1;
                int newScore = SpaceballScore[scorer];
                ResetBall(); // стоп-состояние + возврат в центр + рассылка позиции

                // Порядок пакетов важен (см. документацию MapEventsAssembly):
                //  1) ssi|<mmo>|<eic>|<vru>|<speed>|<companyWithSpeed> — это НЕ
                //     только инициализация: клиент перерисовывает цифры табло
                //     именно из него (GuiManager.createSpaceballScoreboard /
                //     updateScoreboard). Раньше мы слали «ssi|...|0|0» со
                //     СБРОСОМ скорости — и клиент reset-ил индикаторы/табло,
                //     из-за чего счёт визуально не обновлялся. Теперь speed =
                //     текущая скорость мяча (после ResetBall это 0), company =
                //     забившая корпорация — корректное полное состояние табло.
                //  2) ssc|<company>|<newScore>|<portalID> — SET_SCORE:
                //     анимация портала + обновление очков компании.
                //  3) sss — индикаторы скорости (все в 0, мяч вернулся в центр).
                SendEvent("ssi|" + SpaceballScore[1] + "|" + SpaceballScore[2] + "|" + SpaceballScore[3] +
                          "|" + _ballSpeed + "|" + scorer);
                for (int c = 1; c <= 3; c++)
                    SendEvent("sss|" + c + "|0");
                SendEvent("ssc|" + scorer + "|" + newScore + "|" + idx);
                Broadcast("0|A|STD|GOAL! Company " + scorer + " scores (" + newScore + ").");
                break;
            }
        }

        private void ResetBall()
        {
            _prevBallX = BallX; _prevBallY = BallY;
            BallX = FieldCenterX; BallY = FieldCenterY;
            _ballVX = 0; _ballVY = 0;
            _ballSpeed = 0;
            _ballHitter = 0;
            _ballDamage = 0;
            _lastHitMs = 0;
            // Индикаторы скорости — только sss (без повторного ssi: он может
            // пересоздавать окно табло на клиенте). Цифры счёта обновляет
            // вызывающий код (CheckGoal) своим ssi/ssc.
            for (int c = 1; c <= 3; c++)
                SendEvent("sss|" + c + "|0");
            // Мяч вернулся в центр — показываем новую позицию всем зрителям.
            BroadcastMove();
        }

        /// <summary>
        /// Инициализация события для входящего игрока: scoreboard (ssi), скорость
        /// (sss), очки (ssc), мяч как корабль-паттерн 442 (f|C|...|442|...) и позиция.
        /// Вызывается из AddUser/add_user/RDY|MAP для карты 16. Идемпотентен.
        /// </summary>
        public void SendSpaceballTo(uint userId)
        {
            if (_id != SpaceballFieldMap) return;
            User u;
            if (!Program.Users.TryGetValue(userId, out u)) return;
            int leader = _ballHitter != 0 ? _ballHitter : 1;
            // 1) INIT_SCOREBOARD: ssi|mmo|eic|vru|speed|companyWithSpeed
            SendEventTo(u, "ssi|" + SpaceballScore[1] + "|" + SpaceballScore[2] + "|" + SpaceballScore[3] +
                           "|" + _ballSpeed + "|" + leader);
            // 2) SET_SPEED: индикатор скорости у держателя мяча.
            for (int c = 1; c <= 3; c++)
                SendEventTo(u, "sss|" + c + "|" + (c == leader && _ballSpeed > 0 ? _ballSpeed : 0));
            // 3) Текущие очки (табло). portalID — стабильный индекс ворот.
            foreach (var c in SpaceballPortalOrder)
                SendEventTo(u, "ssc|" + c + "|" + SpaceballScore[c] + "|" + Array.IndexOf(SpaceballPortalOrder, c));
            // 4) Сам мяч.
            UpdateBallVisibility(userId);
            StartSpaceball();
        }

        /// <summary>
        /// «Удар» по мячу: вызывается из боевого кода при попадании по Spaceball.
        /// Урон накапливается; скорость полёта растёт с суммарным уроном
        /// (баланс: старт ~12k, пик ~75k). Направление — от позиции атакующего
        /// через мяч (как удар в футболе): игрок наводится, подлетая с нужной
        /// стороны, и может пробить точно в створ своих ворот. Если прямая от
        /// игрока сквозь мяч не приводит к воротам ударившей корпорации (мяч
        /// ушёл «в бок»), вектор доворачивается на ворота — гарантированный
        /// полёт в створ, а не вдоль стороны поля.
        /// СТАРТ С ЦЕНТРА: если мяч стоит ровно в центре поля (только что
        /// после гола/возврата), линия «игрок -> мяч» даёт произвольный угол и
        /// мяч улетал «просто в бок». В этом случае вектор сразу наводится на
        /// ворота ударившей корпорации.
        /// </summary>
        public void HitSpaceball(int attackerCompany, double damage, int fromX, int fromY)
        {
            if (_id != SpaceballFieldMap) return;
            if (attackerCompany < 1 || attackerCompany > 3) return;
            lock (this)
            {
                _ballHitter = attackerCompany;
                _ballDamage += Math.Max(1, damage);
                _lastHitMs = Environment.TickCount;

                bool atCenter = BallX == FieldCenterX && BallY == FieldCenterY;
                ApplyBallVelocity();   // при stand-in-center сам наводит вектор на ворота

                if (_ballSpeed > 0 && !atCenter)
                {
                    // Вектор удара: продолжение линии «игрок -> мяч».
                    double dx = BallX - (double)fromX, dy = BallY - (double)fromY;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len >= 1)
                    {
                        _ballVX = (int)Math.Round(dx / len * _ballSpeed);
                        _ballVY = (int)Math.Round(dy / len * _ballSpeed);
                        // Проверка: приведёт ли этот удар к воротам? Если прямая
                        // уходит «в сторону» — целимся прямо на ворота.
                        int[] goal;
                        if (SpaceballGoals.TryGetValue(attackerCompany, out goal))
                        {
                            double gx = goal[0] - (double)BallX, gy = goal[1] - (double)BallY;
                            double glen = Math.Sqrt(gx * gx + gy * gy);
                            double cos = glen < 1 ? 1 : (dx * gx + dy * gy) / (len * glen);
                            if (cos < 0.85) AimAtGoal(goal); // удар «в сторону» — наводим на створ
                        }
                    }
                }

                // Индикатор скорости на scoreboard — у корпорации-ударившего.
                for (int c = 1; c <= 3; c++)
                    SendEvent("sss|" + c + "|" + (c == attackerCompany ? _ballSpeed : 0));
            }
            BroadcastMove();
        }

        /// <summary>Смена скорости мяча (например, из админ-команд).</summary>
        public void SetSpaceballSpeed(int speed)
        {
            _ballSpeed = Math.Max(0, speed);
            int leader = _ballHitter != 0 ? _ballHitter : 1;
            SendEvent("sss|" + leader + "|" + _ballSpeed);
        }

        /// <summary>
        /// Рассылка событийного пакета (ssi/sss/ssc) всем на поле.
        /// ВАЖНО 1 (префикс): клиент (Flash, DarkOrbit 4.1) матчит события вида
        /// ssi/sss/ssc ТОЛЬКО в строках формата "<ownerFrame>|<cmd>|...". В
        /// оригинальном сервере ownerFrame — ИД игрока-владельца окна
        /// ("11004|ssi|..."), а не "0" — фрейм "0" клиент не ассоциирует ни с
        /// одним слушателем MapEventsAssembly. Шлём "<uid>|<cmd>|..." каждому.
        /// ВАЖНО 2 (копия словаря): Users — общий Dictionary<uint,User>, его
        /// меняют потоки сети (AddUser/Remove). Send — async void: исключение
        /// внутри него НЕ долетает до нашего catch, а вот enumerating по живому
        /// словарю бросает InvalidOperationException и обрывает рассылку посреди
        /// цикла — часть игроков (или все) не получают ssi/ssc, табло не
        /// обновляется. Поэтому снимок списка под lock(this) (все writers
        /// состояния мяча тоже под этим монитором), отправка — вне перебора.
        /// </summary>
        private void SendEvent(string packet)
        {
            List<KeyValuePair<uint, User>> snapshot;
            lock (this)
                snapshot = Users.ToList();
            foreach (var pair in snapshot)
            {
                try { pair.Value.Send(pair.Key + "|" + packet); } catch { }
            }
        }

        private void SendEventTo(User u, string packet)
        {
            try { u.Send(u.UserId + "|" + packet); } catch { }
        }

        /// <summary>Рассылка готового пакета (с префиксом) всем игрокам карты.</summary>
        private void Broadcast(string packet)
        {
            List<User> snapshot;
            lock (this)
                snapshot = Users.Values.ToList();
            foreach (var u in snapshot)
            {
                try { u.Send(packet); } catch { }
            }
        }

        private void BroadcastMove()
        {
            string mv = "0|1|" + SpaceballBallId + "|" + BallX + "|" + BallY + "|" + BallMoveTime();
            List<uint> viewers;
            lock (this)
                viewers = BallViewers.ToList();
            foreach (var uid in viewers)
            {
                User u;
                if (Program.Users.TryGetValue(uid, out u))
                    try { u.Send(mv); } catch { }
            }
        }

    }
}
