using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Do.serverGame
{
    /// <summary>
    /// Порталы и телепортация между картами.
    /// </summary>
    partial class Map
    {
        public void SendPortals(uint userId)
        {
            foreach (var portal in Portals)
            {
                Users[userId].Send("C|p|" + portal.Id + "|1|1|" + portal.X + "|" + portal.Y + "|1|0");
            }
        }

        /// <summary>
        /// Попытка использовать ближайший портал: проверки PvP/уровня, запуск телепортации.
        /// </summary>
        public void UsePortal(uint userId)
        {
            try
            {
                if (Users[userId].Ship.teleporting != 0) return;
                int uX = Users[userId].Ship.GetPosX(), uY = Users[userId].Ship.GetPosY();
                foreach (var portal in Portals)
                {
                    int pX = portal.X, pY = portal.Y;

                    var distancia = Convert.ToInt32(Distance.Get(pX, uX, pY, uY));
                    if (distancia >= PortalUseRange) continue;
                    if (IsPvp)
                    {
                        if (Users[userId].Ship.regen < 4)
                        {
                            Users[userId].Send("0|A|STM|jump_cpu_failed_attack2");
                            return;
                        }
                    }
                    if (Users[userId].Ship.level < Program.Maps[portal.Map].Level && Users[userId].Ship.rank != 21)
                    {
                        Users[userId].Send("0|k|" + Program.Maps[portal.Map].Level);
                        return;
                    }

                    Users[userId].Ship.teleporting = 1;

                    Users[userId].Send("0|U|" + portal.Map + "|" + portal.Id);
                    Accion(userId);
                    return;
                }
                Users[userId].Send("0|A|STM|jumpgate_failed_no_gate");
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Непосредственный переход через портал: анимация, смена координат и карты.
        /// </summary>
        private async void Accion(uint userId)
        {
            try
            {
                if (Users[userId].Ship.teleport1) return;
                Users[userId].Ship.teleport1 = true;
                int uX = Users[userId].Ship.GetPosX(), uY = Users[userId].Ship.GetPosY();

                foreach (var portal in Portals)
                {
                    int pX = portal.X, pY = portal.Y;

                    var distancia = Convert.ToInt32(Distance.Get(pX, uX, pY, uY));
                    if (distancia >= PortalUseRange) continue;
                    await Task.Delay(PortalAnimationDelayMs);
                    if (!Users.ContainsKey(userId)) continue;
                    Users[userId].Ship.InRange = false;

                    Users[userId].Ship.isAttacking = false;
                    ClearSelectionOnCurrentMap(userId);
                    Users[userId].Ship.selected1 = false;
                    Users[userId].Ship.x = portal.NewX;
                    Users[userId].Ship.y = portal.NewY;
                    Users[userId].Ship.destinationx = portal.NewX;
                    Users[userId].Ship.destinationy = portal.NewY;
                    Users[userId].PlayersInRange = new List<uint>();
                    Users[userId].PlayersInRange.Add(userId);
                    Users[userId].MapId = Convert.ToUInt16(portal.Map);
                    Users[userId].Send("0|z");
                    Users[userId].Ship.selectedUserId = 0;
                    Users[userId].Ship.isAttacking = false;
                    Users[userId].Send("0|N|-1");
                    Users[userId].Ship.tickCount = 0;
                    Users[userId].Ship.teleported = true;
                    try
                    {
                        foreach (var alien in Aliens)
                        {
                            if (alien.Value.Move.PlayersInRange.Contains(userId))
                                alien.Value.Move.PlayersInRange.Remove(userId);
                        }
                    }
                    catch (Exception) { }
                    ClearSelectionOnTargetMap(userId);

                    RemoveUser(userId);
                    Program.Maps[portal.Map].AddUser(userId);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Сброс выделения телепортируемого игрока у игроков текущей карты.
        /// </summary>
        private void ClearSelectionOnCurrentMap(uint userId)
        {
            foreach (var player in Users.Where(player => player.Value.Ship.selectedUserId == Users[userId].UserId))
            {
                player.Value.Ship.selectedUserId = 0;
                player.Value.Ship.isAttacking = false;
                player.Value.Send("SELECT|-1");
                player.Value.Send("0|N|-1");
                player.Value.Ship.selected1 = false;
            }
        }

        /// <summary>
        /// Сброс выделения игрока, который его «наследует» на целевой карте после перехода.
        /// </summary>
        private void ClearSelectionOnTargetMap(uint userId)
        {
            foreach (var player in Program.Maps[Users[userId].MapId].Users.Where(
                player => player.Value.Ship.selectedUserId == Users[userId].UserId))
            {
                try
                {
                    player.Value.Ship.selectedUserId = 0;
                    player.Value.Ship.isAttacking = false;
                    player.Value.Send("SELECT|-1");
                    player.Value.Send("0|N|-1");
                }
                catch (KeyNotFoundException) { }
            }
        }
    }
}
