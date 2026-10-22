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
 * Partial-class User: Атака ракетами и РСС (RocketAttack, RsbAttack, Attackingrsb).
 */
partial class User
{
        private void RocketAttack()//atack ship code
        {
            try
            {
                if (!Ship.CanUseRockets)
                {
                    return;
                }
                if (Ship.HP < 1)
                {
                    discconectUser(1);
                    Program.Maps[MapId].RemoveUser(UserId);
                }
                var selected = Ship.selectedUserId;
                Ship.InRange = false;
                if (selected == 0)
                {
                }
                else if ((selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected)) || (selected > 999 && !Program.Maps[MapId].Users.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                }
                else if (selected < 1000)
                {
                    AlienRocketAttack();
                }
                else
                {
                    var enemy = Program.Users[selected];
                    int eX = Convert.ToInt32(enemy.Ship.x), eY = Convert.ToInt32(enemy.Ship.y),
                        uX = Convert.ToInt32(Ship.x), uY = Convert.ToInt32(Ship.y);
                    var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));
                    if (enemy.Ship.isAttacking == false && enemy.Ship.InSecureArea)
                    {
                        Send("0|P");
                        enemy.Send("0|P");
                        Ship.InRange = false;
                        Ship.isAttacking = false;
                    }
                    else if (Ship.logout3 == 1)
                    {
                        Ship.isAttacking = false;
                        Ship.InRange = false;
                    }
                    else if (distancia > 800)
                    {
                        Ship.InSecureArea = false;
                    }
                    else
                    {
                        enemy.Ship.regen = 0;
                        if (Ship.cloaked == 1)
                        {
                            Ship.cloaked = 0;
                            Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                            foreach (var user in PlayersInRange)
                            {
                                Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                            }
                        }
                        Ship.CanUseRockets = false;
                        SetRocket();
                        UpdateRocket();
                        Ship.InSecureArea = false;
                        var random4 = _rnd4.Next(1, 10);
                        if (random4 != 5)
                        {
                            switch (RocketId)
                            {
                                case 5:
                                    //{
                                    //    if (!CanUsePld) return;

                                    //    CanUsePld = false;
                                    //    UpdateRocket();
                                    //    PldCooldown();
                                    //    enemy.ActivatePld();
                                    //    var packet = "0|v|" + UserId + "|" + selected + "|P|" + RocketId + "|" + Precision + "|" + Precision;
                                    //    Send("0|A|CLD|PLA|45");
                                    //    Send(packet);
                                    //    foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                                    //    {
                                    //        Program.Users[pair].Send(packet);
                                    //    }
                                    //}
                                    break;
                                case 6:
                                    //{

                                    //    if (!CanUseWiz) return;

                                    //    CanUseWiz = false;
                                    //    //UpdateRocket();
                                    //    WizCooldown();
                                    //    enemy.ActivateWiz();
                                    //    var packet = "0|v|" + UserId + "|" + selected + "|P|" + RocketId + "|" + Precision + "|" + Precision;
                                    //    Send("0|A|CLD|WIZ|45");
                                    //    Send(packet);
                                    //    foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                                    //    {
                                    //        Program.Users[pair].Send(packet);
                                    //    }
                                    //}
                                    break;
                                default:
                                    if (RocketId < 5)
                                    {
                                        var power2 = 900 + new Random().Next(-100, 100);
                                        var power = Convert.ToInt32(power2 * RocketId);
                                        if (Ship.Spectrum && enemy.Ship.Spectrum == false)
                                        {
                                            power = power / 2;
                                        }
                                        else if (Ship.Spectrum && enemy.Ship.Spectrum)
                                        {
                                            power = power / 6;
                                        }
                                        else if (Ship.Spectrum == false && enemy.Ship.Spectrum)
                                        {
                                            power = power / 3;
                                        }
                                        else if (enemy.Ship.ShieldLeech && enemy.Ship.shield > 0)
                                        {
                                            power = (power * 3) / 2;
                                        }
                                        var realDamage = power;
                                        if (enemy.Ship.ISH)
                                        {
                                            realDamage = 0;
                                            power = 0;

                                        }
                                        Program.Users[selected].TakeDamage(realDamage);

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
                                        if (enemy.Ship.HP - realDamage < 1 || enemy.Ship.HP > enemy.Ship.maxHP)
                                        {
                                            enemy.Ship.HP = 0;
                                            Ship.isAttacking = false;
                                            if (enemy.Ship.destroyed == false)
                                            {
                                                enemy.Ship.destroyed = true;
                                                enemy.Ship.HP = 0;
                                                Send("0|A|STD|" + " You destroyed " + enemy.Ship.Name);
                                                UpdateUser(1, selected);
                                            }
                                            Ship.isAttacking = false;
                                        }
                                        else
                                        {
                                            try
                                            {
                                                enemy.Ship.HP -= Convert.ToUInt32(realDamage);
                                                var packet = "0|v|" + UserId + "|" + selected + "|H|" + RocketId + "|" + Precision + "|" + Precision;
                                                var packet2 = "0|Y|" + UserId + "|" + selected + "|R|" + enemy.Ship.HP + "|" + enemy.Ship.shield + "|" + power + "|100|1";
                                                var packet3 = "0|M|" + UserId + "|" + selected + "|R|" + enemy.Ship.HP + "|" + enemy.Ship.shield + "|" + power + "|100|1";
                                                Send("0|A|CLD|ROK|3");
                                                Send(packet);
                                                if (enemy.Ship.ISH)
                                                {
                                                    Send(packet3);
                                                    foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                                                    {
                                                        Program.Users[pair].Send(packet);
                                                    }
                                                }
                                                else
                                                {
                                                    Send(packet2);
                                                    foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                                                    {
                                                        Program.Users[pair].Send(packet);
                                                    }
                                                }
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            var packe = "0|v|" + UserId + "|" + selected + "|H|" + RocketId + "|" + Precision + "|" + Precision;
                            var packe2 = "0|M|" + UserId + "|" + selected + "|R|" + enemy.Ship.HP + "|" + enemy.Ship.shield + "|0|100|1";

                            Send(packe);
                            Send(packe2);
                            foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                            {
                                Program.Users[pair].Send(packe);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            { }
        }
        private async void SetRsb()
        {
            try
            {
                Out.WriteLine("userId " + UserId + "Sended packet of RSB reload", Ip, ConsoleColor.Yellow);
                await Task.Delay(3000);
                Ship.CanUseRSB = true;
                Ship.lunchrsb = false;
            }
            catch (Exception)
            { }
        }

        private async void SetRocket()

        {
            if (Ship.premium == 1)
                await Task.Delay(2000);
            else
                await Task.Delay(3000);

            Ship.CanUseRockets = true;
        }

}
}
