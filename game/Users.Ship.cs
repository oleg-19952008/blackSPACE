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
 * Partial-class User: Обновление корабля (UpdateShip), выход из игры (IsLogout), sendStations.
 */
partial class User
{
        public async void UpdateShip()
        {

            try
            {
                string shield;

                if (Ship.maxShield == 0 || Ship.maxShield2 == 0)
                {
                    //shield = "";
                    //shield2 = "";
                }
                //else
                //{

                //}

                if (CurrentConfig == 1)
                {
                    // Out.WriteLine("я тут1");
                    Send("HwkU|I|" + UserId + "|" + Ship.Name + "|" + Ship.Id + "|" + Ship.speed + "|" + Ship.shield.ToString() + "|" + Ship.maxShield.ToString() + "|" + Ship.HP + "|" + Ship.GetShipMaxHP() + "|5|100|" + Ship.x + "|" + Ship.y + "|" + MapId + "|" + Ship.GetFactionID() + "|" + Ship.clanID + "|1500|0|15|" + Ship.premium + "|" + Ship.exp + "|" + Ship.honor + "|" + Ship.level + "|" + Ship.credits + "|" + Ship.uri + "|10|" + Ship.rank + "|" + Ship.tagName + "|0|1|" + Ship.cloaked + "|50|25");

                }
                else if (CurrentConfig == 2)
                {
                    //  Out.WriteLine("я тут2");
                    Send("HwkU|I|" + UserId + "|" + Ship.Name + "|" + Ship.Id + "|" + Ship.speed + "|" + Ship.shield.ToString() + "|" + Ship.maxShield.ToString() + "|" + Ship.HP + "|" + Ship.GetShipMaxHP() + "|5|100|" + Ship.x + "|" + Ship.y + "|" + MapId + "|" + Ship.GetFactionID() + "|" + Ship.clanID + "|1500|0|15|" + Ship.premium + "|" + Ship.exp + "|" + Ship.honor + "|" + Ship.level + "|" + Ship.credits + "|" + Ship.uri + "|10|" + Ship.rank + "|" + Ship.tagName + "|0|1|" + Ship.cloaked + "|50|25");
                }
                await Task.Delay(1);
            }
            catch { }
        }

        private async void IsLogout()
        {
            try
            {
                for (var i = 25; i > 0; i--)
                {
                    if (Ship.isLogout)
                    {
                        foreach (var player in Program.Maps[MapId].Users)
                        {
                            if (player.Value.Ship.selectedUserId == UserId && player.Value.Ship.isAttacking)
                            {
                                Send("0|t");
                                Ship.isLogout = false;
                                Ship.logout2 = 0;
                                Ship.logout3 = 0;
                                break;
                            }
                        }
                        Ship.isAttacking = false;
                    }
                    else
                    {

                        Ship.isLogout = false;
                        Ship.logout2 = 0;
                        Ship.logout3 = 0;
                        break;

                    }
                    await Task.Delay(200);
                }
                if (Ship.isLogout)
                {
                    foreach (var player in Program.Maps[MapId].Users)
                    {
                        if (player.Value.Ship.selectedUserId == UserId && player.Value.Ship.isAttacking)
                        {
                            Send("0|t");
                            Ship.isLogout = false;
                            Ship.logout2 = 0;
                            Ship.logout3 = 0;
                            break;
                        }
                    }
                    Ship.logout2 = 0;
                    Ship.logout3 = 0;
                    Send("0|l");
                    Send("0|l");
                    Program.Users[UserId].discconectUser(-1);
                    Program.Maps[MapId].RemoveUser(UserId);
                }
                else
                {
                    Ship.isLogout = false;
                    Ship.logout2 = 0;
                    Ship.logout3 = 0;
                }
            }
            catch (Exception)
            { }
        }
        public void sendStations()
        {

            if (MapId == 1)
            {
                Send("0|s|0|1|redStation|1|1500|1000|1000");
            }
            else if (MapId == 5)
            {
                Send("0|s|0|1|blueStation|2|1500|19800|1000");
            }
            else if (MapId == 9)
            {
                Send("0|s|0|1|greenStation|3|1500|19600|11900");
            }

        }
}
}
