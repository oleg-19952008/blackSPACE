using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Do.game;

namespace Do.serverGame
{
    /// <summary>
    /// Работа с игроками на карте: вход, выход и удаление NPC-«детёнышей».
    /// </summary>
    partial class Map
    {
        /// <summary>
        /// Вход пользователя на карту (вызывается из обработчика пакета "map").
        /// Сохраняет исходное имя add_user для совместимости с существующими вызовами.
        /// </summary>
        public async static void add_user(uint user)
        {
            var map = Program.Maps[Program.Users[user].MapId];
            if (!map.Users.ContainsKey(user))
            {
                map.Users.Add(user, Program.Users[user]);
            }
            map.Users[user].SendUserShit();
            map.Users[user].sendStations();
            map.Users[user].Ship.lunchrsb = false;
            map.SendPortals(user);

            if (map.Users[user].Ship.teleported)
            {
                try
                {
                    await Task.Delay(TeleportDelayMs);

                    map.Users[user].Ship.teleport1 = false;
                    map.Users[user].Ship.buclemover = false;
                    map.Users[user].Ship.teleported = false;
                    map.Users[user].Ship.teleporting = 0;
                }
                catch (Exception) { }
            }
            if (map.Users[user].Ship.teleported2)
            {
                try
                {
                    map.Users[user].Ship.buclemover = false;
                    await Task.Delay(BucleTeleportDelayMs);
                    map.Users[user].Ship.teleport2 = false;

                    map.Users[user].Ship.teleported2 = false;
                    map.Users[user].Ship.teleporting2 = 0;
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Публичный экземплярный вход пользователя на карту (используется при телепортации).
        /// </summary>
        public void AddUser(uint userId)
        {
            if (!Users.ContainsKey(userId))
            {
                Users.Add(userId, Program.Users[userId]);
            }
            Users[userId].SendUserShit();
            Users[userId].sendStations();
            Users[userId].Ship.lunchrsb = false;
            SendPortals(userId);
            SendMines(userId);
            SendCargos(userId);
            // Синхронизация состояния пета после смены карты. Раньше здесь при
            // каждом входе на карту отправлялся "0|PET|D" (для выключенного пета)
            // и оконные пакеты G|A/G|SEL (для включённого) — клиент из-за этого
            // проигрывал звук запуска/остановки пета просто от входа на карту,
            // хотя пользователь пета не трогал. Теперь RestoreAfterMapChange:
            //  - пет выключен -> ничего не шлём (состояние окна на клиенте и так
            //    корректное: оно переживает переход без сброса);
            //  - пет включён -> пересылаем только визуальные пакеты дрона, сам
            //    пет появляется на новой карте с тем же выбранным механизмом.
            try
            {
                var pet = Program.Users[userId].Pet;
                if (pet != null) pet.RestoreAfterMapChange();
            }
            catch (Exception) { }
            if (Users[userId].Ship.teleported)
            {
                FinishFirstTeleport(userId);
            }
            if (Users[userId].Ship.teleported2)
            {
                FinishSecondTeleport(userId);
            }
        }

        /// <summary>
        /// Завершение «первого» телепорта после входа на карту (исходные Ssss/Sss).
        /// </summary>
        private async void FinishFirstTeleport(uint userId)
        {
            try
            {
                await Task.Delay(TeleportDelayMs);

                Users[userId].Ship.teleport1 = false;
                Users[userId].Ship.buclemover = false;
                Users[userId].Ship.teleported = false;
                Users[userId].Ship.teleporting = 0;
                Move(userId);
            }
            catch (Exception) { }
        }

        private async void FinishSecondTeleport(uint userId)
        {
            try
            {
                Users[userId].Ship.buclemover = false;
                await Task.Delay(BucleTeleportDelayMs);
                Users[userId].Ship.teleport2 = false;

                Users[userId].Ship.teleported2 = false;
                Users[userId].Ship.teleporting2 = 0;
                Move(userId);
            }
            catch (Exception) { }
        }

        public void RemoveUser(uint userId)
        {
            try
            {
                Users.Remove(userId);
                Parallel.ForEach(Users, pair => pair.Value.Send("f|R|" + userId));
            }
            catch (Exception) { }
        }

        public void RemoveDaughters(uint kidId)
        {
            try
            {
                var players = Aliens[kidId].Move.PlayersInRange;
                Aliens[kidId].Destroyed = true;
                Aliens.Remove(kidId);
                foreach (var pair in players)
                {
                    Users[pair].Send("f|R|" + kidId);
                }
            }
            catch (Exception) { }
        }
    }
}
