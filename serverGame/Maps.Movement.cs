using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Do.serverGame
{
    /// <summary>
    /// Движение кораблей игроков: тик перемещения, видимость NPC и игроков,
    /// безопасные зоны/радиация и столкновения с минами.
    /// </summary>
    partial class Map
    {
        /// <summary>
        /// Запускает цикл тиков движения для игрока (один цикл на игрока).
        /// </summary>
        public void Move(uint userId)
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    if (!Users.ContainsKey(userId)) return;
                    if (Users[userId].Ship.buclemover) return;
                    Users[userId].Ship.buclemover = true;
                    while (Users.ContainsKey(userId))
                    {
                        _MoveShip(userId);
                        await Task.Delay(MovementTickMs);
                    }
                });
            }
            catch (Exception) { }
        }

        private void _MoveShip(uint userId)
        {
            try
            {
                string movementPacket;
                InterpolatePosition(userId, out movementPacket);

                int nX = Users[userId].Ship.x, nY = Users[userId].Ship.y;

                UpdateAlienVisibility(userId, nX, nY);
                UpdatePlayersVisibility(userId, nX, nY, movementPacket);
                // Spaceball: мяч виден/скрыт по мере приближения-удаления игрока.
                if (_id == SpaceballFieldMap)
                {
                    int ballDist = Distance.Get(BallX, nX, BallY, nY);
                    UpdateBallVisibility(userId, ballDist < ViewRange);
                }

                // Пересчёт позиции после интерполяции для зон безопасности/радиации
                nX = Users[userId].Ship.GetPosX();
                nY = Users[userId].Ship.GetPosY();

                UpdateSecureAndRadiation(userId, nX, nY);
                CheckMineCollisions(userId, nX, nY);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Интерполяция позиции корабля к точке назначения и формирование пакета движения.
        /// </summary>
        private void InterpolatePosition(uint userId, out string movementPacket)
        {
            movementPacket = "";
            var ship = Users[userId].Ship;
            var teleporting = ship.teleporting;
            var teleporting2 = ship.teleporting2;
            var logout3 = ship.logout3;

            if (ship.x == ship.destinationx && ship.y == ship.destinationy || teleporting == 1 || teleporting2 == 1 || logout3 == 1)
            {
                ship.isMoving = false;
                ship.tickCount = 0;
            }
            else if (Users[userId].DestroyedClass || !Users.ContainsKey(userId))
            {
                return;
            }
            else
            {
                ship.isMoving = true;
                var newPosX = ship.destinationx;
                var newPosY = ship.destinationy;
                var oldx = ship.x;
                var oldy = ship.y;
                var distancia = Math.Sqrt(((oldx - newPosX) * (oldx - newPosX)) + ((oldy - newPosY) * (oldy - newPosY)));
                var speed2 = Math.Round(Convert.ToDouble(ship.speed * 0.36));
                var moveBitch = Math.Atan2(newPosY - oldy, newPosX - oldx);
                var speed = Math.Round(distancia / ship.speed * 1000);
                movementPacket = "0|1|" + userId + "|" + newPosX + "|" + newPosY + "|" + speed;

                if (distancia >= ship.speed)
                {
                    ship.x += (int)Math.Round(speed2 * Math.Cos(moveBitch));
                    ship.y += (int)Math.Round(speed2 * Math.Sin(moveBitch));
                }
                else
                {
                    ship.x = newPosX;
                    ship.y = newPosY;
                    ship.isMoving = false;
                }
            }
        }

        /// <summary>
        /// Добавление/убирание NPC из видимости игрока при движении.
        /// </summary>
        private void UpdateAlienVisibility(uint userId, int nX, int nY)
        {
            try
            {
                foreach (var pair in Aliens)
                {
                    var distance = Convert.ToInt32(Distance.Get(pair.Value.X, nX, pair.Value.Y, nY));
                    if (distance < ViewRange)
                    {
                        if (pair.Value.Move.PlayersInRange.Contains(userId) || pair.Value.Destroyed) continue;
                        pair.Value.Move.PlayersInRange.Add(userId);
                        Users[userId].Send("f|C|" + pair.Value.Id + "|" + Program.Npcs[pair.Value.TypeId].Id + "|1|" + "" + "|" + Program.Npcs[pair.Value.TypeId].Name + "|" + pair.Value.X + "|" + pair.Value.Y + "||-1|0|0|0|0");
                        pair.Value.Move.SendMovement(userId);
                        if (pair.Value.TypeId == 80)
                            Users[userId].Send("0|n|BOOOM|" + pair.Value.Id);
                    }
                    else if (Users[userId].Ship.selectedUserId != pair.Value.Id &&
                             pair.Value.Move.PlayersInRange.Contains(userId))
                    {
                        Users[userId].Send("f|R|" + pair.Value.Id);
                        pair.Value.Move.PlayersInRange.Remove(userId);
                    }
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Обновление списка игроков в зоне видимости движущегося корабля.
        /// </summary>
        private void UpdatePlayersVisibility(uint userId, int nX, int nY, string movementPacket)
        {
            foreach (var pair in Users)
            {
                try
                {
                    foreach (var user in Users[userId].PlayersInRange)
                    {
                        if (!Program.Users.ContainsKey(user) || Program.Users[user].MapId != _id || Users[user].DestroyedClass)
                            Users[userId].PlayersInRange.Remove(user);
                    }
                }
                catch (Exception) { }

                if (pair.Key == userId) continue;
                var distance = Convert.ToInt32(Distance.Get(pair.Value.Ship.GetPosX(), nX, pair.Value.Ship.GetPosY(), nY));
                if (distance < ViewRange)
                {
                    if (Users[userId].Ship.MovingTask == 0)
                    {
                        Users[pair.Key].Send(movementPacket);
                    }

                    if (Users[userId].PlayersInRange.Contains(pair.Key)) continue;
                    Users[userId].PlayersInRange.Add(pair.Key);
                    Users[pair.Key].Send(BuildShipCreatePacket(userId));
                    Users[userId].Send(BuildShipCreatePacket(pair.Value.UserId));
                    Users[pair.Key].Send(movementPacket);
                    Users[userId].Send("0|n|d|" + pair.Key + "|" + Users[pair.Key].Ship.DronePacket);
                    Users[pair.Key].Send("0|n|d|" + userId + "|" + Users[userId].Ship.DronePacket);
                }
                else if (pair.Key != userId)
                {
                    if (Users[userId].Ship.selectedUserId != pair.Key && !(_factionId != pair.Value.Ship.factionId && IsStarterMap))
                    {
                        if (Users[userId].PlayersInRange.Contains(pair.Key))
                        {
                            Users[userId].PlayersInRange.Remove(pair.Key);
                            Users[userId].Send("f|R|" + pair.Key);
                        }
                    }
                    if (Users[pair.Key].Ship.selectedUserId == userId ||
                        _factionId != Users[userId].Ship.factionId && IsStarterMap) continue;
                    if (!Users[userId].PlayersInRange.Contains(pair.Key)) continue;
                    Users[userId].PlayersInRange.Remove(pair.Key);
                    Program.Users[pair.Key].Send("f|R|" + userId);
                }
            }

            Users[userId].Ship.MovingTask = 1;
        }

        /// <summary>
        /// Пакет создания корабля игрока для клиента (f|C|...).
        /// </summary>
        private static string BuildShipCreatePacket(uint targetUserId)
        {
            var target = Program.Users[targetUserId];
            return "f|C|" + targetUserId + "|" + target.Ship.Id + "|4|" + target.Ship.tagName + "|" + target.Ship.Name +
                   "|" + target.Ship.x + "|" + target.Ship.y + "|" + target.Ship.factionId + "|0|" + target.Ship.rank +
                   "|0|0|7||0|" + target.Ship.cloaked;
        }

        /// <summary>
        /// Расчёт безопасной зоны и радиации; отправка 0|D|... при смене состояния.
        /// </summary>
        private void UpdateSecureAndRadiation(uint userId, int nX, int nY)
        {
            var inSecureArea = false;
            var radiation = false;

            // На PvP-картах безопасные зоны у порталов не действуют
            if (!PvpMaps.Contains(_id))
            {
                foreach (var portal in Portals)
                {
                    var distance = Convert.ToInt32(Distance.Get(portal.X, nX, portal.Y, nY));
                    if (distance >= SecureAreaRange || Users[userId].Ship.isAttacking) continue;
                    inSecureArea = true;
                    break;
                }
            }

            radiation = IsOutsideWorldBounds(nX, nY);

            if (!inSecureArea && !Users[userId].Ship.isAttacking &&
                IsInFactionSecureZone(Users[userId].Ship.factionId, nX, nY))
            {
                inSecureArea = true;
            }

            if (inSecureArea != Users[userId].Ship.InSecureArea ||
                            radiation != Users[userId].Ship.Radiation)
                Users[userId].Send("0|D|10400|6400|" + Program.ToEnum(inSecureArea) + "|1|1|" +
                                   Program.ToEnum(radiation) + "|1|0");

            Users[userId].Ship.InSecureArea = inSecureArea;
            Users[userId].Ship.Radiation = radiation;
        }

        /// <summary>
        /// Столкновение корабля с минами и применение эффектов мин.
        /// </summary>
        private void CheckMineCollisions(uint userId, int nX, int nY)
        {
            foreach (var mine in Mines)
            {
                int pX = Convert.ToInt32(mine.Value[0]), pY = Convert.ToInt32(mine.Value[1]);

                if (Distance.Get(nX, pX, nY, pY) > MineTriggerRange) continue;
                foreach (var player in Users)
                {
                    player.Value.Send("0|LM|ST|MIN|" + mine.Key);
                }
                Mines.Remove(mine.Key);
                switch (mine.Value[2])
                {
                    case "1":
                        var acm = Users[userId].Ship.GetShipHP() / 5;
                        Users[userId].Ship.HP -= acm;
                        Users[userId].Send("0|Y|" + userId + "|" + userId + "|R|" + Users[userId].Ship.GetShipHP() + "|" + Users[userId].Ship.shield + "|" + acm + "|100|100");
                        Users[userId].Send("0|A|HPT|" + Users[userId].Ship.GetShipHP() + "|" + Users[userId].Ship.GetShipMaxHP() + "|" + userId);
                        break;
                    case "2":
                        Users[userId].Ship.cloaked = 0;
                        foreach (var ship in Users.Values)
                        {
                            ship.Send("0|n|INV|" + userId + "|0");
                        }
                        break;
                    case "3":
                        Users[userId].Ship.shield -= Users[userId].Ship.shield / 2;
                        Users[userId].Send("0|A|SHD|" + Users[userId].Ship.shield + "|" + Users[userId].Ship.maxShield + "|" + userId);
                        break;
                    case "4":
                        var ddm = Users[userId].Ship.GetShipMaxHP() / 5;
                        if (Users[userId].Ship.HP - ddm < 1)
                        {
                            Users[userId].Ship.HP = 0;
                            Users.Remove(userId);
                            foreach (var ship in Users.Values)
                            {
                                ship.Send("0|K|" + userId);
                            }
                            Users[userId].discconectUser(1);
                        }
                        else
                        {
                            Users[userId].Ship.HP -= ddm;
                        }
                        Users[userId].Send("0|Y|" + userId + "|" + userId + "|R|" + Users[userId].Ship.GetShipHP() + "|" + Users[userId].Ship.shield + "|" + ddm + "|100|100");
                        Users[userId].Send("0|A|HPT|" + Users[userId].Ship.GetShipHP() + "|" + Users[userId].Ship.GetShipMaxHP() + "|" + userId);
                        break;
                }
            }
        }
    }
}
