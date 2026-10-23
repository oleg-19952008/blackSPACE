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
 * Partial-class User: Управление боеприпасами: учёт, проверка, отправка остатков, кулдаун лазера.
 */
partial class User
{
        public void SendUserShit()
        {
            //UpdateGroup();
            UpdateShip();
            Send("0|B|" + Ship.x1 + "|" + Ship.x2 + "|" + Ship.x3 + "|" + Ship.UCB100 + "|" + Ship.sab + "|" + Ship.rsb);
            Send("0|7|HS");
            sendStations();
            Send("0|S|CFG|" + CurrentConfig);
            Send("0|RL|R|" + Ship.HSTRM01 + "|" + Ship.UBR100 + "|" + Ship.ECO10);
            Send("0|RL|S|" + Ship.HSTRM + "|" + HellstormId + "|" + LoadedHellstorm);
            Send("0|A|v|" + Ship.speed);
            //     this.Send("0|SD|S|" + this.Ship.Skill);
            Send("0|A|JV|" + "0");
            Send("0|A|BK|" + "0");
            Send("0|A|ITM|0|0|0|0|" + Ship.RepBot + "|0|0|" + Program.ToEnum(Ship.SMBCPU) + "|" + Program.ToEnum(Ship.ISHCPU) + "|0|0|" + Program.ToEnum(Ship.AROCKET) + "|0|0|0|0|0");
            Send("0|TX|S|" + ((Ship.ELAA != 0) ? "1" : "0") + "|" + Ship.ELAA + "|0|" + ((Ship.ECIA != 0) ? "1" : "0") + "|" + Ship.ECIA + "|0|" + ((Ship.RPMA != 0) ? "1" : "0") + "|" + Ship.RPMA + "|0|" + ((Ship.SBUA != 0) ? "1" : "0") + "|" + Ship.SBUA + "|0|" + ((Ship.BRBA != 0) ? "1" : "0") + "|" + Ship.BRBA + "|0");
            Send("0|ps|blk|0");
            Send("0|3|" + Ship.R310 + "|" + Ship.PLT2026 + "|" + Ship.PLT2021 + "|" + Ship.PLT3030 + "|" + Ship.PLD8 + "|" + Ship.DCR250 + "|" + Ship.WIZ + "|" + Ship.ACM01 + "|" + Ship.SMBA + "|" + Ship.ISHA + "|" + Ship.EMPA + "|" + Ship.EMPM01 + "|" + Ship.SABM01 + "|" + Ship.DDM01);
            //this.Send("0|A|CPU|C|" + this.Ship.cloaks);
            Send("0|RDY|POI");
            // SPACEBALL: рассылка контрольных счётчиков станций фракций.
            // Клиент по этим значениям показывает/разблокирует кнопку перехода на
            // боевую станцию своей фракции (spaceball). Класс Spaceball с полями
            // MMO/EIC/VRU в этом коде сервера отсутствует, поэтому отправляем
            // нулевые счётчики напрямую — этого достаточно, чтобы клиент отрисовал
            // элементы spaceball и начал слать "n|s|<faction>", который сервер
            // обрабатывает через SetSpaceball().
            Send("0|n|ssi");
            Send("0|n|ssc|1|0");
            Send("0|n|ssc|2|0");
            Send("0|n|ssc|3|0");
            // this.Send("0|m|" + this.mapId + "|" + this.Ship.x + "|" + this.Ship.y);
            Send("0|A|BS|0/0/" + Ship.dmgbooster + "/" + Ship.shieldbooster + "/0/0/0/" + Ship.hpbooster);
            Send("0|n|d|" + UserId + "|" + Ship.DronePacket);
            Send("0|A|STD|Server " + Program.VERSION);
            Send("0|A|STD|Admin`s server SeVeN & NavigatoR");
            // Счётчик больше не может уйти в отрицательные значения (DecrementOnline защищён),
            // поэтому прежняя "коррекция" online < 0 -> online = 1 удалена как маскирующая баг.
            //   Send("0|A|STD|Онлайн сервера: " + online);

            //"0|A|STD|+ текст
            if (Ship.rank == 21)
            {
                Send("0|n|pt|" + UserId + "|ADMIN");
                Send("0|PET|I|1|1|1");
                Send("0|SD|S|1|1|0");
                Send("0|SD|S|2|1|0");
                Send("0|SD|S|3|1|0");
                Send("0|SD|S|4|1|0");
                Send("0|SD|S|5|1|0");
                Send("0|SD|S|6|1|0");
            }
            var file_prem = System.IO.File.ReadAllLines(Environment.CurrentDirectory + @"\prem.txt");
            foreach (var user__ in file_prem)
            {
                Ship.premium = 1;
                if (UserId == Convert.ToInt32(user__))
                {
                    Send("0|PET|I|1|1|1");
                }
            }
            //тестовый кусок кода, попробовал добавить для админов все навыки которые есть в дарке.
            /* if(Ship.rank == 21  )
               {        
                       Send("0|SD|S|1|1|0");   
                       Send("0|SD|S|2|1|0");         
                       Send("0|SD|S|3|1|0");     
                       Send("0|SD|S|4|1|0");
                   Send("0|SD|S|5|1|0");      
               } */ //конец куска 
            switch (Ship.Id)
            {
                case 63:
                    Send("0|SD|S|1|1|0");
                    break;
                case 64:
                    Send("0|SD|S|2|1|0");
                    break;
                case 65:
                    Send("0|SD|S|3|1|0");
                    break;
                case 66:
                    Send("0|SD|S|4|1|0");
                    break;
                case 67:
                    Send("0|SD|S|5|1|0");
                    break;
                case 98:
                    Send("0|SD|S|1|1|0");
                    Send("0|SD|S|2|1|0");
                    Send("0|SD|S|3|1|0");
                    Send("0|SD|S|4|1|0");
                    Send("0|SD|S|5|1|0");
                    Send("0|SD|S|6|1|0");
                    break;
                default:

                    if (Ship.Id == 47 && Ship.premium == 1)
                    {
                        Send("0|SD|S|4|1|0");

                    }
                    else if (Ship.Id == 47 && Ship.premium == 0)
                    {
                        Send("0|n|pt|" + UserId + "|title_EliteHunter");
                    }
                    break;
            }
            //ниже петька
            
        }

        readonly Random _rnd5 = new Random();
        readonly Random rnd6 = new Random();
        private async void LaserCooldown(uint id)
        {
            if (LaserCooldownList.Contains(id)) return;
            LaserCooldownList.Add(id);
            await Task.Delay(800);
            LaserCooldownList.Remove(id);

        }

        readonly Random _rnd6 = new Random();

}
}
