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
 * Partial-class User: Отправка данных: Send/SendCallback, sendConfig, обновление лазеров/ракет, SendUserShit.
 */
partial class User
{
        public async void Send(String data)
        {
            try { 

       //  System.IO.File.AppendAllText(Environment.CurrentDirectory + "\\send.txt", data + "\n");
            }
            catch { }    
            Out.WriteLine("LOG.SEND: " + data.Replace('|', '|'));
            try
            {
                if (_handler == null || !_handler.Connected) return;
                var byteData = Encoding.UTF8.GetBytes(data.Replace('|', '|') + (char)0x00);

                _handler.BeginSend(byteData, 0, byteData.Length, 0,
                                       SendCallback, _handler);
            }
            catch (SocketException e)
            {
                if (_socketClosed)
                {
                    if (e.NativeErrorCode.Equals(10035))
                    {
                    }
                    else
                    {
                        _socketClosed = true;
                        DiscconectUserInactivity();
                    }
                }
            }
            await Task.Delay(1);
        }
        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                var handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                var bytesSent = handler.EndSend(ar);
            }
            catch (Exception)
            {
                //Console.WriteLine(e.ToString());
            }
        }

        public void sendConfig()
        {
            if (UserId== 20002||UserId== 13064)
             {
                 if (UserId == 13064)//oleg
                 {
                     Send("0|n|t|" + this.UserId + "Мастер Орбиты");
                 }
                 if (UserId == 20002)//dima
                 {
                     Send("0|n|t|" + this.UserId + "server Admin");
                 }
             }
            Send("0|A|SET|" + Settings.SET);
            Send("0|7|MINIMAP_SCALE|" + Settings.minimapScale);
            Send("0|7|DISPLAY_PLAYER_NAMES|" + Settings.displayPlayerNames);
            Send("0|7|DISPLAY_CHAT|" + Settings.displayChat);
            Send("0|7|PLAY_MUSIC|" + Settings.playMusic);
            Send("0|7|PLAY_SFX|" + Settings.playSfx);
            Send("0|7|BAR_STATUS|" + Settings.barStatus);
            Send("0|7|" + Settings.windowSettings);
            Send("0|UI|W|HW|10");
            Send("0|UI|W|HW|11");
            Send("0|UI|W|HW|6");
            Send("0|UI|W|HW|20");
            //      Send("0|7|RESIZABLE_WINDOWS" + Settings.RESIZABLE_WINDOWS);
            Send("0|7|AUTO_REFINEMENT|" + Settings.autorefine);
            Send("0|7|QUICKSLOT_STOP_ATTACK|" + Settings.quickslotStopAttack);
            Send("0|7|DOUBLECLICK_ATTACK|" + Settings.doubleclickAttack);
            Send("0|7|AUTO_START|" + Settings.autoStart);
            Send("0|7|DISPLAY_NOTIFICATIONS|" + Settings.displayNotif);
            Send("0|7|SHOW_DRONES|" + Settings.showDrones);
            Send("0|7|DISPLAY_WINDOW_BACKGROUND|" + Settings.displayWindowBackground);
            Send("0|7|ALWAYS_DRAGGABLE_WINDOWS|" + Settings.alwaysDraggableWindows);
            Send("0|7|PRELOAD_USER_SHIPS|" + Settings.preloadUserShips);
            Send("0|7|QUALITY_PRESETTING|" + Settings.qualityPresetting);
            Send("0|7|QUALITY_CUSTOMIZED|" + Settings.qualityCustomized);
            Send("0|7|QUALITY_BACKGROUND|" + Settings.qualityBg);
            Send("0|7|QUALITY_POIZONE|" + Settings.qualityPoiz);
            Send("0|7|QUALITY_SHIP|" + Settings.qualityShip);
            Send("0|7|QUALITY_ENGINE|" + Settings.qualityEng);
            Send("0|7|QUALITY_COLLECTABLE|" + Settings.qualityColl);
            Send("0|7|QUALITY_ATTACK|" + Settings.qualityAtta);
            Send("0|7|QUALITY_EFFECT|" + Settings.qualityEff);
            Send("0|7|QUALITY_EXPLOSION|" + Settings.qualityExp);
            Send("0|7|QUICKBAR_SLOT|" + Settings.quickbar);
            Send("0|7|SLOTMENU_POSITION|" + Settings.slotmenuPos);
            Send("0|7|SLOTMENU_ORDER|" + Settings.slotmenuOrder);
            Send("0|7|MAINMENU_POSITION|" + Settings.mainmenuPos);
        }
        //private   async void UpdateLaser()
        //{
        //    Thread.Sleep(2000);
        //    Ship.x1 = Program.loadAmmos(UserId, "ammunition_laser_lcb-10");
        //    Ship.x2 = Program.loadAmmos(UserId, "ammunition_laser_mcb-25");
        //    Ship.x3 = Program.loadAmmos(UserId, "ammunition_laser_mcb-50");
        //    Ship.UCB100 = Program.loadAmmos(UserId, "ammunition_laser_ucb-100");
        //    Ship.rsb = Program.loadAmmos(UserId, "ammunition_laser_rsb-75");
        //    Ship.sab = Program.loadAmmos(UserId, "ammunition_laser_sab-50");
        //    var q = Program.Manager.getQueryreactor();

        //        q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.x1 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_lcb-10'");
        //    q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.x2 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_mcb-25'");
        //    Thread.Sleep(20); q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.x3 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_mcb-50'");
        //    Thread.Sleep(20); q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.UCB100 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_ucb-100'");
        //    Thread.Sleep(20); q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.rsb + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_rsb-75'");
        //    Thread.Sleep(20); q.query("UPDATE server_1_player_all_items SET Q = '" + Ship.sab + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_laser_sab-50'");

        //    Send("0|B|" + Ship.x1 + "|" + Ship.x2 + "|" + Ship.x3 + "|" + Ship.UCB100 + "|" + Ship.sab + "|" + Ship.rsb);
        //    await Task.Delay(1);       
        //}
        // Универсальное сохранение количества боеприпаса в БД.
        // ВАЖНО: runQuery() теперь пробрасывает DatabaseException (корректный rollback
        // транзакций). Вызывающие UpdateLaser()/UpdateRocket() выполняются напрямую из
        // обработки пакетов атаки (ReadCallback) без try/catch: любое исключение отсюда
        // раньше молча обрывало цепочку ДО финальной синхронизации клиента
        // ("0|B|..." / SendSpAmmo) — купленные в магазине PLT-3030 не отображались в
        // космосе, а часть логики пакета не доисполнялась. Ошибку логируем, но синхронизацию
        // выполняем ВСЕГДА; loadAmmos() на каждом выстреле перечитывает счётчик из БД,
        // поэтому сбойная запись не копит рассинхрон серверного кэша.
        private bool SaveAmmoCount(string lootid, int quantity)
        {
            try
            {
                using (var dbClient = Program.Manager.getQueryreactor())
                {
                    dbClient.query("UPDATE server_1_player_all_items SET Q = '" + quantity + "' WHERE playerID = '" + UserId + "' AND lootid = '" + lootid + "'");
                }
                return true;
            }
            catch (Exception e)
            {
                Out.WriteLine("SaveAmmoCount failed [" + lootid + "]: " + e.Message, "User.UpdateAmmo", ConsoleColor.DarkYellow);
                return false;
            }
        }

        // Флаг "последняя запись боеприпасов в БД не удалась": пока он сброшен,
        // серверные счётчики (Ship.*) перезагружаются из БД на каждом выстреле;
        // при сбое записи серверное значение сохраняется, чтобы перезагрузка из
        // устаревшей строки БД не отменяла покупку ("купил PLT-3030 — пропали").
        private bool _ammoDbDirty = false;

        private void UpdateLaser()
        {
            int dbX1 = Program.loadAmmos(UserId, "ammunition_laser_lcb-10");
            int dbX2 = Program.loadAmmos(UserId, "ammunition_laser_mcb-25");
            int dbX3 = Program.loadAmmos(UserId, "ammunition_laser_mcb-50");
            int dbUcb100 = Program.loadAmmos(UserId, "ammunition_laser_ucb-100");
            int dbRsb = Program.loadAmmos(UserId, "ammunition_laser_rsb-75");
            int dbSab = Program.loadAmmos(UserId, "ammunition_laser_sab-50");
            if (!_ammoDbDirty)
            {
                Ship.x1 = dbX1;
                Ship.x2 = dbX2;
                Ship.x3 = dbX3;
                Ship.UCB100 = dbUcb100;
                Ship.rsb = dbRsb;
                Ship.sab = dbSab;
            }
            switch (LaserId)
            {
                case 1:
                    Ship.x1 -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_lcb-10", Ship.x1);
                    break;
                case 2:
                    Ship.x2 -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_mcb-25", Ship.x2);
                    break;
                case 3:
                    Ship.x3 -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_mcb-50", Ship.x3);
                    break;
                case 4:
                    Ship.UCB100 -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_ucb-100", Ship.UCB100);
                    break;
                case 5:
                    Ship.sab -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_sab-50", Ship.sab);
                    break;
                case 6:
                    Ship.rsb -= Ship.lasers;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_laser_rsb-75", Ship.rsb);
                    break;
                default:
                    break;
            }
            // Синхронизация клиента выполняем ВСЕГДА, даже если запись в БД не удалась:
            // именно этот пакет показывает купленные/расходованные боеприпасы в UI.
            Send("0|B|" + Ship.x1 + "|" + Ship.x2 + "|" + Ship.x3 + "|" + Ship.UCB100 + "|" + Ship.sab + "|" + Ship.rsb+"|1000");
        }

        private void UpdateRocket()
        {
            // ВАЖНО: loadAmmos() перечитывает ВСЕ боеприпасы из БД. Если предыдущая
            // запись в БД не удалась (серверный кэш ушёл от базы), перезагрузка
            // молча ОТМЕНЯЛА покупку: игрок стрелял, а купленные PLT-3030 "пропадали"
            // и не отображались в космосе. Поэтому при неудачной записи сохраняем
            // серверное значение, пока БД снова не станет доступна.
            int dbPlt2021 = Program.loadAmmos(UserId, "ammunition_rocket_plt-2021");
            int dbPlt3030 = Program.loadAmmos(UserId, "ammunition_rocket_plt-3030");
            int dbPlt2026 = Program.loadAmmos(UserId, "ammunition_rocket_plt-2026");
            int dbR310 = Program.loadAmmos(UserId, "ammunition_rocket_r-310");
            if (!_ammoDbDirty)
            {
                Ship.PLT2021 = dbPlt2021;
                Ship.PLT3030 = dbPlt3030;
                Ship.PLT2026 = dbPlt2026;
                Ship.R310 = dbR310;
            }
            switch (RocketId)
            {
                case 1:
                    Ship.R310 -= 1;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_rocket_r-310", Ship.R310);
                    break;
                case 2:
                    Ship.PLT2026 -= 1;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_rocket_plt-2026", Ship.PLT2026);

                    break;
                case 3:
                    Ship.PLT2021 -= 1;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_rocket_plt-2021", Ship.PLT2021);
                    break;
                case 4:
                    Ship.PLT3030 -= 1;
                    _ammoDbDirty = !SaveAmmoCount("ammunition_rocket_plt-3030", Ship.PLT3030);
                    break;
                    //case 5:
                    //    Ship.sab -= Ship.lasers;
                    //    using (var dbClient = Program.manager.getQueryreactor())
                    //    {
                    //        dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Ship.sab + "' WHERE playerID = '" + userId + "' AND lootid = 'ammunition_laser_sab-50'");
                    //    }
                    //    break;
                    //case 6:
                    //    Ship.rsb -= Ship.lasers;
                    //    using (var dbClient = Program.manager.getQueryreactor())
                    //    {
                    //        dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Ship.rsb + "' WHERE playerID = '" + userId + "' AND lootid = 'ammunition_laser_rsb-75'");
                    //    }
                    //    break;
            }
            SendSpAmmo();
        }
        private bool CheckLaser()
        {

            if (LaserId == 1 && Ship.x1 - Ship.lasers < 0)
            {
                return false;
            }
            else if (LaserId == 2 && Ship.x2 - Ship.lasers < 0)
            {
                return false;
            }
            else if (LaserId == 3 && Ship.x3 - Ship.lasers < 0)
            {
                return false;
            }
            else if (LaserId == 4 && Ship.UCB100 - Ship.lasers < 0)
            {
                return false;
            }
            else if (LaserId == 5 && Ship.sab - Ship.lasers < 0)
            {
                return false;
            }
            else if (LaserId == 6 && Ship.rsb - Ship.lasers < 0)
            {
                return false;
            }
            return true;
        }
        private bool CheckRocket()
        {
            if (RocketId == 1 && Ship.R310 - 1 < 0)
            {
                return false;
            }
            else if (RocketId == 2 && Ship.PLT2026 - 1 < 0)
            {
                return false;
            }
            else if (RocketId == 3 && Ship.PLT2021 - 1 < 0)
            {
                return false;
            }
            else if (RocketId == 4 && Ship.PLT3030 - 1 < 0)
            {
                return false;
            }
            //else if (rocketId == 5 && Ship.sab - Ship.lasers < 0)
            //{
            //    return false;
            //}
            //else if (rocketId == 6 && Ship.rsb - Ship.lasers < 0)
            //{
            //    return false;
            //}
            return true;
        }

}
}
