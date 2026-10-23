using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Do.game; // User (Program.Users)
using Do.serverGame; // Map, Alien (Program.Maps)

namespace Do.Assembly
{
    class Pet
    {
        public uint PetId;
        public bool PetActive = false, Kami = false, GuardMode = false, Destroyed = false;
        // Выбранный механизм (gear): 101 — пассивный (по умолчанию), 102 — guard,
        // 1/2/6/7 — доп. механизмы. Хранится на сервере, чтобы после смены карты
        // восстанавливать в UI ровно тот механизм, что был выбран до перехода.
        public int SelectedGear = 101;
        public uint KamikazeLvl;
        public int Hp, Shield, Experience, Damage;
        public int X, Y;
        // Базовая скорость пета = скорость корабля владельца + 20%.
        // Обновляется в RecalcSpeed() при Activate и при каждом смене карты —
        // раньше скорость ставилась только при создании пета/активации и после
        // перехода на другую карту оставалась старой (или равной скорости
        // корабля без бонуса).
        public uint Speed;
        /// <summary>Пересчёт скорости пета: на 20% больше текущей скорости корабля.</summary>
        public void RecalcSpeed()
        {
            try
            {
                var owner = Program.Users[UserId];
                if (owner != null)
                    Speed = (uint)(owner.Ship.speed * 1.2);
            }
            catch { }
        }
        public DateTime KamiCld = new DateTime(2017, 3, 1, 7, 0, 0);
        public uint UserId { get; set; }
        public Pet(uint userId, int random)
        {
            try
            {
                UserId = userId;
                PetId = uint.Parse(random + "" + userId.ToString(CultureInfo.InvariantCulture));
                //    PetId = userId + 10000;
                X = Program.Users[userId].Ship.x;
                Y = Program.Users[userId].Ship.y;
                // Скорость пета всегда на 20% выше скорости корабля владельца.
                Speed = (uint)(Program.Users[userId].Ship.speed * 1.2);
                KamikazeLvl = 3;
                Hp = 10000;
                Shield = 10000;
                Damage = 3000;
            }
            catch { }
        }
        public void Activate()
        {
            try
            {
                // Повторное включение без выключения игнорируем: иначе каждый
                // "PET|L" заново шлёт весь набор G|A/G|D/G|SEL и перезапускает
                // циклы Movement/AttackLoop — клиент заливаются пакетами.
                if (PetActive)
                {
                    return;
                }
                var owner = Program.Users[UserId];
                var exp = owner.Ship.exp / 900;

                PetActive = true;
                Destroyed = false;

                // Инициализация модуля дрона на клиенте. Отправляем её ДО
                // открытия окна и до синхронизации механизмов — именно этот
                // пакет делает кнопки механизмов кликабельными с первого раза
                // (раньше он мог не доходить до UI-модуля, и выбор работал
                // только после достать/убрать/достать).
                owner.Send("0|PET|I|1|1|1");
                owner.Send("0|PET|S|1|15|" + exp + "|" + (exp + 10000) + "|999|999|999|999|1|15|1|[PET 15] " + owner.Ship.Name);

                // Синхронизация списка механизмов в UI: все доступны, выбран тот,
                // что реально активен (по умолчанию — пассивный 101).
                SendGearSync();

                RecalcSpeed(); // скорость пета = скорость корабля + 20%
                X = owner.Ship.GetPosX();
                Y = owner.Ship.GetPosY();

                for (var index = 0; index < Program.Maps[owner.MapId].Users.Count; index++)
                {
                    Program.Maps[owner.MapId].Users.ElementAt(index).Value.Send("0|PET|L|1|" + (owner.UserId + 1000) + "|15|[PET 15] " + owner.Ship.Name + " |" + owner.Ship.factionId + "|1|1|" + owner.Ship.tagName + "|0|" + X + "|" + Y + "|1|1|");
                }
                Movement();
                AttackLoop();
            }
            catch (Exception e)
            {
                Out.WriteLine("Pet.Activate failed for user " + UserId + ": " + e.Message + "\n" + e.StackTrace, "Pet", ConsoleColor.Red);
            }
        }
        /// <summary>
        /// Синхронизация списка механизмов в окне пета: все механизмы доступны,
        /// подсвечен только реально выбранный (SelectedGear, по умолчанию 101 —
        /// пассивный). Отправляется при Activate и после смены карты.
        /// Порядок важен: сначала A для всех, затем D для невыбранных, затем SEL.
        /// Вызывается только когда пет РЕАЛЬНО активен (PetActive == true),
        /// чтобы не проигрывать "звук запуска" на карте, где пета нет.
        /// </summary>
        public void SendGearSync()
        {
            if (!PetActive) return;
            var owner = Program.Users[UserId];
            owner.Send("0|PET|G|A|101"); // passive
            owner.Send("0|PET|G|A|102"); // guard
            owner.Send("0|PET|G|A|1");   // autoloot
            owner.Send("0|PET|G|A|2");   // rescollect
            owner.Send("0|PET|G|A|6");   // repair
            owner.Send("0|PET|G|A|7");   // kami
            foreach (var gear in new[] { 101, 102, 1, 2, 6, 7 })
            {
                if (gear != SelectedGear)
                    owner.Send("0|PET|G|D|" + gear);
            }
            owner.Send("0|PET|G|SEL|" + SelectedGear);
        }

        /// <summary>
        /// Восстановление пета на новой карте после перехода (портал/прыжок).
        /// ВАЖНО: при смене карты Flash-клиент пересоздаёт игровое состояние, и
        /// окно пета открывается в дефолтном ("выключенном") виде, даже если пет
        /// активен и летает за игроком. Поэтому здесь:
        ///  - обновляется скорость пета (скорость корабля +20% могла измениться
        ///    вместе с экипировкой/навыками на новой карте);
        ///  - шлётся "0|PET|I|1|1|1" — включает модуль дрона в UI без звука;
        ///  - шлётся "0|PET|S|..." — состояние окна (прочность/щиты/опыт) и
        ///    подсветка "включено";
        ///  - пересылаются механизмы (G|A/G|D/G|SEL) — восстанавливается ровно
        ///    тот механизм, что был выбран до перехода (SelectedGear);
        ///  - пересылаются только визуальные пакеты самого дрона (PET|L), без
        ///    звуковых оконных событий вроде PET|D / PET|Z.
        /// Когда пет выключен — не отправляется ничего, чтобы вход на карту не
        /// проигрывал "звук остановки".
        /// </summary>
        public void RestoreAfterMapChange()
        {
            try
            {
                if (!PetActive) return;
                var owner = Program.Users[UserId];
                if (owner == null) return;
                RecalcSpeed(); // скорость актуальна для текущего корабля
                X = owner.Ship.GetPosX();
                Y = owner.Ship.GetPosY();
                // При переходе на другую карту старый цикл Movement остаётся
                // крутиться в фоне: он видит, что владельца нет в Users старой
                // карты, и вызывает DeActivate() — пет "выключается" серверно,
                // хотя визуально летает за игроком. Останавливаем старые циклы
                // флагом (пет при этом остаётся PetActive=true), затем ниже
                // запускаем новые циклы уже для новой карты.
                PetActive = false;
                PetActive = true;
                // Инициализация модуля дрона в новом клиенте (без звука).
                owner.Send("0|PET|I|1|1|1");
                // Состояние окна пета: включён, прочность/щиты/опыт.
                var exp = owner.Ship.exp / 900;
                owner.Send("0|PET|S|1|15|" + exp + "|" + (exp + 10000) + "|999|999|999|999|1|15|1|[PET 15] " + owner.Ship.Name);
                // Механизмы: подсвечиваем тот, что был выбран до перехода.
                SendGearSync();
                for (var index = 0; index < Program.Maps[owner.MapId].Users.Count; index++)
                {
                    Program.Maps[owner.MapId].Users.ElementAt(index).Value.Send("0|PET|L|1|" + (owner.UserId + 1000) + "|15|[PET 15] " + owner.Ship.Name + " |" + owner.Ship.factionId + "|1|1|" + owner.Ship.tagName + "|0|" + X + "|" + Y + "|1|1|");
                }
                Movement();
                AttackLoop();
            }
            catch (Exception e)
            {
                Out.WriteLine("Pet.RestoreAfterMapChange failed for user " + UserId + ": " + e.Message, "Pet", ConsoleColor.Red);
            }
        }

        public void DeActivate()
        {
            try
            {
                // Игнорируем "PET|D", когда пет и так убран: иначе клиент
                // повторно проигрывает звук остановки.
                if (!PetActive)
                {
                    return;
                }
                var s = Program.Users[UserId];
                PetActive = false;
                Kami = false;
                GuardMode = false;
                for (var index = 0; index < Program.Maps[Program.Users[UserId].MapId].Users.Count; index++)
                {
                    Program.Maps[Program.Users[UserId].MapId].Users.ElementAt(index).Value.Send("f|R|" + (s.UserId + 1000));
                }
                Program.Users[UserId].Send("0|PET|D");
            }
            catch (Exception e)
            {
              //  Out.WriteLine(e.StackTrace);
            }
        }
        public void Destroy()
        {
            try
            {
                var s = Program.Users[UserId];
                PetActive = false;
                Destroyed = true;
                Kami = false;
                GuardMode = false;
                for (var index = 0; index < Program.Maps[Program.Users[UserId].MapId].Users.Count; index++)
                {
                    Program.Maps[Program.Users[UserId].MapId].Users.ElementAt(index).Value.Send("0|K|" + (s.UserId + 1000));
                }
                Program.Users[UserId].Send("0|PET|Z");
            }
            catch (Exception e)
            {
                //Out.WriteLine(e.StackTrace);
            }
        }
        public void Repair()
        {
            Program.Users[UserId].Send("0|PET|R");
        }
        #region Gears
        public void SelectGear(string content)
        {
            try
            {
                if (!PetActive)
                {
                    return;
                }
                GuardMode = false;
                Kami = false;
                var packet = content.Split('|');
                var gear = Convert.ToInt32(packet[3]);
                // Повторный клик по уже выбранному механизму игнорируем: иначе
                // на каждое "PET|G|L|<текущий>" сервер пересылает весь набор
                // G|A/G|D/G|SEL — клиент засыпается пакетами (в логах это
                // выглядело как бесконечный спам при доставании пета).
                if (gear == SelectedGear)
                {
                    return;
                }
                // Запоминаем выбранный механизм, чтобы после смены карты
                // восстановить в UI ровно его.
                SelectedGear = gear;
                SendGearSync();
                switch (gear)
                {
                    case 7:
                        Kamikaze();
                        break;
                    case 102:
                        GuardMode = true;
                        // Цикл атаки уже запущен в Activate()/RestoreAfterMapChange()
                        // и сам проверяет GuardMode на каждой итерации. Второй
                        // AttackLoop() отсюда порождает параллельные циклы — при
                        // повторном включении защиты пет начинал спамить выстрелами.
                        break;
                }
            }
            catch (Exception e)
            {
                Out.WriteLine(e.StackTrace);
            }
        }
        #region Kamikaze
        public async void Kamikaze()
        {
            try
            {
                var cooldown = (DateTime.Now - KamiCld);
                if (cooldown.TotalSeconds < 30)
                {
                    var a = 30 - Convert.ToInt32(cooldown.TotalSeconds);
                    Program.Users[UserId].Send("0|A|STD|Kamikaze is on cooldown! " + a + " seconds left!");
                    return;
                }
                var exp = Program.Users[UserId].Ship.exp; // как в Activate: реальный опыт владельца, а не статический exp_
                KamiMove();
                await Task.Delay(3500);
                var damage = (exp / 2010) * 2;
           //         (uint) 25000;// KamikazeLvl * 20000; //урон камиком
                if (!PetActive)
                {
                    return;
                }
                KamiCld = DateTime.Now;

                //foreach (var user in Program.Maps[Program.Users[userID].mapId].Users.Values)
                //{
                //    double distance = Math.Sqrt(Math.Pow(Program.Users[userID].Ship.x - x, 2) + Math.Pow(Program.Users[userID].Ship.y - y, 2));
                //    if (distance < 330 && Program.Users[userID].userId != this.userID)
                //    {
                //        Send("0|Y|1|" + Program.Users[userID].userId + "|L|" + Program.Users[userID].Ship.HP + "|" + Program.Users[userID].Ship.shield + "|" + damage + "|100|1");
                //        if (Program.Users[userID].Ship.HP - damage < 1)
                //        {
                //            Program.Users[userID].Ship.HP = 0;
                //            Program.Maps[Program.Users[userID].mapId].Users.Remove(this.userID);
                //            foreach (var ship in Program.Maps[Program.Users[userID].mapId].Users.Values)
                //            {
                //                ship.Send("0|K|" + this.userID);
                //            }
                //            Program.Users[userID].discconectUser(1);

                //        }
                //        else
                //        {
                //            Program.Users[userID].Ship.HP -= damage;
                //        }
                //        Program.Users[userID].Send("0|A|HPT|" + Program.Users[userID].Ship.HP + "|" + Program.Users[userID].Ship.maxHP + "|" + Program.Users[userID].userId);
                //        Program.Users[userID].Send("0|A|SHD|" + Program.Users[userID].Ship.shield + "|" + Program.Users[userID].Ship.maxShield + "|" + Program.Users[userID].userId);

                //    }
                //}
                foreach (var alien in from alien in Program.Maps[Program.Users[UserId].MapId].Aliens.Values let distance = Math.Sqrt(Math.Pow(alien.X - X, 2) + Math.Pow(alien.Y - Y, 2)) where distance < 330 select alien)
                {
                    Send("0|Y|1|" + alien.Id + "|L|" + alien.Hp + "|" + alien.Shield + "|" + damage + "|100|1");
                    if (alien.Hp - damage < 1)
                    {
                        alien.SelectedUserId = 0;
                        alien.Destroyed = true;
                        alien.Hp = 0;
                        foreach (var ship in Program.Maps[Program.Users[UserId].MapId].Aliens[alien.Id].Move.PlayersInRange)
                        {
                            Program.Users[ship].Send("0|K|" + alien.Id);
                        }
                        //Program.Users[userID].UpdateUser(2);
                        alien.Regenerate();
                    }
                    else
                    {
                        alien.Hp -= (uint)damage;
                    }
                }
                Destroy();
            }
            catch (Exception e)
            {
             //   Out.WriteLine(e.StackTrace);
            }
        }
        private async void KamiMove()
        {
            try
            {
                Speed = 1400;
                while (Kami && PetActive)
                {
                    Move();
                    await Task.Delay(500);
                }
            }
            catch (Exception e)
            {
           //     Out.WriteLine(e.StackTrace);
            }
        }
        #endregion
        #endregion
        #region Attack
        private async void AttackLoop()
        {
            try
            {
                while (PetActive)
                {
                    if (GuardMode)
                    {
                        Attack();
                    }
                    await Task.Delay(900);
                }
            }
            catch (Exception e)
            {
            //    Out.WriteLine(e.StackTrace);
            }
        }

        private void Attack()
        {
            try
            {
                var owner = Program.Users[UserId];
                // Раньше здесь проверялся только флаг Ship.isAttacking. Этот флаг
                // сервер выставляет в обработчике пакета "G" (отдаление цели на
                // 1000+ px), а при обычном удержании ЛКМ по NPC клиент шлёт лишь
                // "a|<цель>" + "1|x|y|...", и isAttacking для NPC мог оставаться
                // false — пет никогда не стрелял по мобам. Теперь цель берётся из
                // selectedUserId (его клиент шлёт пакетом SEL, см. лог:
                // "LOG.INPUT:SEL|36"), а факт атаки определяется так же, как у
                // корабля владельца: isAttacking ИЛИ выбранная цель с активным
                // режимом защиты (в guard-режиме пет должен атаковать всё, что
                // атакует игрок).
                var attacking = owner.Ship.isAttacking ||
                                (GuardMode && owner.Ship.selectedUserId != 0);
                if (!attacking)
                {
                    return;
                }
                var targetId = owner.Ship.selectedUserId;
                if (targetId == 0 || !Program.Maps.ContainsKey(owner.MapId))
                {
                    return;
                }

                // Цель < 1000 — NPC: раньше Attack() искала цель только среди
                // игроков (Program.Users[targetId]), из-за чего пет в режиме
                // защиты вообще не атаковал мобов. Теперь, как и корабль
                // владельца, пета атакует любого, кого выбрано: и игроков, и NPC.
                if (targetId < 1000)
                {
                    PetAttackAlien(owner, targetId);
                    return;
                }

                if (!Program.Users.ContainsKey(targetId) ||
                    Program.Users[targetId].MapId != owner.MapId)
                {
                    return;
                }
                if (!owner.Ship.InRange)
                {
                    return;
                }
                var userId = targetId;
                var enemy = Program.Users[userId];
                // Урон пета по игроку = 10% урона корабля владельца.
                var power = Convert.ToInt32(owner.Ship.damage * (owner.LaserId == 5 ? 2 : 1) * 0.1);
                if (power < 1) power = 1;
                var realDamage = power;
                power = power + new Random().Next(-10, 10);
                var packet = "0|a|" + PetId + "|" + userId + "|" + (Program.Users[UserId].LaserId - 1) + "|1|1";
                var packet2 = "0|Y|" + PetId + "|" + userId + "|L|" + enemy.Ship.HP + "|" + enemy.Ship.shield + "|" + power + "|100|1";
                var packet3 = "0|M|" + PetId + "|" + userId + "|L|" + enemy.Ship.HP + "|" + enemy.Ship.shield + "|" + power + "|100|1";
                if (enemy.Ship.ISH)
                {
                    realDamage = 0;
                    power = 0;
                }
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
                foreach (var pair in Program.Maps[Program.Users[UserId].MapId].Users)
                {
                    pair.Value.Send(packet);
                    if (enemy.Ship.ISH)
                    {
                        realDamage = 0;
                        if (pair.Value.Ship.selectedUserId == userId || userId == pair.Value.UserId)
                        {
                            pair.Value.Send(packet3);
                        }
                    }
                    else
                    {
                        if (pair.Value.Ship.selectedUserId == userId || userId == pair.Value.UserId)
                        {
                            pair.Value.Send(packet2);
                        }
                    }
                }
                if (enemy.Ship.HP - realDamage < 1 || enemy.Ship.HP > enemy.Ship.maxHP)
                {
                    enemy.Ship.HP = 0;
                    Program.Users[UserId].Ship.isAttacking = false;
                    if (enemy.Ship.destroyed == false)
                    {
                        enemy.Ship.destroyed = true;
                        foreach (var pair in Program.Maps[Program.Users[UserId].MapId].Users)
                        {
                            pair.Value.Send("0|K|" + Program.Users[UserId].Ship.selectedUserId);
                        }
                        if (enemy.Ship.destroyed)
                        {
                            Program.Users[UserId].Send("0|A|STD|You ripped " + enemy.Ship.Name + ".");
                            foreach (var pair in Program.Maps[Program.Users[UserId].MapId].Users)
                            {
                                pair.Value.Send("0|K|" + Program.Users[UserId].Ship.selectedUserId);
                            }
                            if (Program.Users[UserId].Ship.factionId == enemy.Ship.factionId)
                            {
                                if (Program.Users[UserId].Ship.exp > 0)
                                {
                                    Program.Users[UserId].Ship.exp -= 1;
                                    Program.Users[UserId].Send("0|LM|ST|EP|-1|" + Program.Users[UserId].Ship.exp);
                                }
                                else
                                {
                                    Program.Users[UserId].Ship.exp = 0;
                                    Program.Users[UserId].Send("0|LM|ST|EP|-1|" + Program.Users[UserId].Ship.exp);
                                }
                            }
                            else
                            {
                                Program.Users[UserId].Ship.exp += 1;
                                Program.Users[UserId].Send("0|LM|ST|EP|1|" + Program.Users[UserId].Ship.exp);
                            }
                            if (Program.Users[UserId].Ip == enemy.Ip)
                            {
                                Program.Users[UserId].Ship.exp -= 1;
                                Program.Users[UserId].Send("0|LM|ST|EP|-1|" + Program.Users[UserId].Ship.exp);
                                Logger.Log(UserId.ToString(CultureInfo.InvariantCulture), "Received", "-1 exp");
                            }
                        }
                        Program.Maps[Program.Users[UserId].MapId].RemoveUser(Program.Users[UserId].Ship.selectedUserId);
                        enemy.discconectUser(1);
                        Program.Users[UserId].Ship.selectedUserId = 0;
                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_players SET exp = '" + Program.Users[UserId].Ship.exp + "' WHERE playerID = '" + UserId + "'");
                        }
                    }
                    Program.Users[UserId].Ship.isAttacking = false;
                }
            }
            catch (Exception e)
            {
                // Out.WriteLine(e.StackTrace);
            }
        }

        /// <summary>
        /// Атака пета по NPC (мобу), которого атакует владелец в режиме защиты.
        /// Пакеты и расчёт урона повторяют лазерную атаку игрока
        /// (User.Alienattack): "0|a|<атакующий>|<цель>|<лазер>|1|1", урон — "0|Y|...",
        /// "недостаточно далеко" — "0|O|". Отдельный cooldown-список, чтобы не
        /// мешать циклу лазеров самого игрока.
        /// </summary>
        private readonly List<uint> _petLaserCooldown = new List<uint>();
        private void PetAttackAlien(game.User owner, uint targetId)
        {
            try
            {
                var map = Program.Maps[owner.MapId];
                if (!map.Aliens.ContainsKey(targetId)) return;
                var alien = map.Aliens[targetId];
                if (alien.Destroyed) return;

                var distance = Distance.Get(alien.X, X, alien.Y, Y);
                if (distance > 840)
                {
                    // Пет ещё не долетел до моба — ждём (клиенту ничего не шлём,
                    // предупреждение "0|O|" относится к кораблю игрока).
                    return;
                }

                if (_petLaserCooldown.Contains(targetId)) return;
                _petLaserCooldown.Add(targetId);
                Task.Delay(1500).ContinueWith(_ => _petLaserCooldown.Remove(targetId));

                // Урон пета = 10% урона корабля владельца (по требованию).
                var power = Convert.ToInt32(owner.Ship.damage * 0.1) + Program.Random.Next(-10, 10);
                if (power < 1) power = 1;
                var realDamage = power;
                var absorb = 0;

                if (alien.Shield > 0)
                {
                    if (alien.Shield > (uint)realDamage)
                    {
                        alien.Shield -= Convert.ToUInt32(realDamage);
                        absorb = realDamage;
                        realDamage = 0;
                    }
                    else
                    {
                        absorb = (int)alien.Shield;
                        realDamage -= (int)alien.Shield;
                        alien.Shield = 0;
                    }
                }
                realDamage = Math.Abs(realDamage);
                realDamage = realDamage - Convert.ToInt32(realDamage *
                    (Program.Npcs[alien.TypeId].ShieldAbsorb / 100m));

                var packet = "0|a|" + PetId + "|" + targetId + "|4|1|1";
                var packet2 = "0|Y|" + PetId + "|" + targetId + "|L|" + alien.Hp + "|" + alien.Shield + "|" + power + "|100|1";
                var packet3 = "0|M|" + PetId + "|" + targetId + "|L|" + alien.Hp + "|" + alien.Shield + "|" + power + "|100|1";

                alien.ReceivedAttack(owner.UserId);

                if (alien.Hp <= (uint)realDamage || alien.Hp > Program.Npcs[alien.TypeId].Hp)
                {
                    alien.Hp = 0;
                    foreach (var pair in map.Users)
                    {
                        pair.Value.Send(packet);
                        if (pair.Value.Ship.selectedUserId == targetId || pair.Value.UserId == owner.UserId)
                            pair.Value.Send(packet2);
                    }
                    if (!alien.Destroyed)
                    {
                        alien.SelectedUserId = 0;
                        alien.Destroyed = true;
                        foreach (var user in alien.Move.PlayersInRange.Where(user => Program.Users.ContainsKey(user)))
                        {
                            Program.Users[user].Send("0|K|" + targetId);
                        }
                        // Опыт за килл начисляется владельцу пета (как при его собственной атаке).
                        owner.UpdateUser(2, targetId);
                        alien.Regenerate();
                    }
                }
                else
                {
                    alien.Hp -= Convert.ToUInt32(realDamage);
                    foreach (var pair in map.Users)
                    {
                        pair.Value.Send(packet);
                        if (pair.Value.Ship.selectedUserId == targetId || pair.Value.UserId == owner.UserId)
                        {
                            pair.Value.Send(packet2);
                            pair.Value.Send(packet3);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion
        #region Movement

        private async void Movement()
        {
            try
            {
                // При переходе на другую карту старый цикл не должен вызывать
                // DeActivate(): пет остаётся активным, а на новой карте цикл
                // перезапускается из RestoreAfterMapChange. Проверяем только
                // что пользователь ещё на сервере (не дисконектнулся).
                while (PetActive && !Kami && Program.Users.ContainsKey(UserId))
                {
                    Move();
                    await Task.Delay(365);
                }
            }
            catch (Exception e)
            {
                //     Out.WriteLine(e.StackTrace);
            }
        }

        private void Move()
        {
            try
            {
                var s = Program.Users[UserId];
                var distance = Math.Sqrt(Math.Pow(Program.Users[UserId].Ship.x - X, 2) + Math.Pow(Program.Users[UserId].Ship.y - Y, 2));
                if (distance < 190 && !Kami)
                {
                }
                else
                {
                    var petx = Program.Users[UserId].Ship.GetPosX() + Program.Random.Next(-100, 100);
                    var pety = Program.Users[UserId].Ship.GetPosY() + Program.Random.Next(-100, 100);
                    if (Kami)
                    {
                        var enemy =
                            Program.Maps[Program.Users[UserId].MapId].Aliens[Program.Users[UserId].Ship.selectedUserId];
                        petx = enemy.X;
                        pety = enemy.Y;
                    }
                    distance = Math.Sqrt(Math.Pow(petx - X, 2) + Math.Pow(pety - Y, 2));
                    var time = Convert.ToInt32((distance / Speed) * 1000);
                    var pet = "0|1|" + (s.UserId + 1000) + "|" + petx + "|" + pety + "|" + time;
                    // Console.WriteLine(pet+" & "+Speed+ " " + distance +" "+petx + " " +x);
                    Send(pet);
                    UpdateXy(petx, pety);
                }
            }
            catch { }
        }
        private void Send(string packet)
        {
            foreach (var user in Program.Maps[Program.Users[UserId].MapId].Users)
            {
                user.Value.Send(packet);
            }
        }
        private void UpdateXy(int a, int b)
        {
            var newPosX = a;
            var newPosY = b;
            var oldx = X;
            var oldy = Y;
            var distance = Math.Sqrt(Math.Pow(oldx - newPosX, 2.0D) + Math.Pow(oldy - newPosY, 2.0D));
            var speed = Speed * 0.37D;
            var dir = Math.Atan2(newPosY - oldy, newPosX - oldx) * 180.0D / (Math.PI);
            if (distance >= speed)
            {
                oldx += Convert.ToInt32((speed * Math.Cos((Math.PI * dir) / 180)));
                oldy += Convert.ToInt32((speed * Math.Sin((Math.PI * dir) / 180)));
                X = oldx;
                Y = oldy;
            }
            else
            {
                X = newPosX;
                Y = newPosY;
            }
        }

        #endregion
        public string PetPacket()
        {
            return "f|C|" + PetId + "|15|1|" + Program.Users[UserId].Ship.tagName + "|" + Program.Users[UserId].Ship.Name + "'s PET|" + X + "|" + Y + "|" + Program.Users[UserId].Ship.factionId + "|-1|0|0|1|0";
        }
        public string PetOwnerPacket()
        {
            return "0|PET|L|1|" + PetId + "|15|" + Program.Users[UserId].Ship.Name + "'s PET|" + Program.Users[UserId].Ship.factionId + "|1|1|" + Program.Users[UserId].Ship.tagName + "|0|" + X + "|" + Y + "|1|1";
        }
    }
}