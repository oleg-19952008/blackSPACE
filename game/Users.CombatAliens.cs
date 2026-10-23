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
 * Partial-class User: Бой с NPC-чужими: Alienattack, AlienRsbAttack, AlienRocketAttack.
 */
partial class User
{
        private void Alienattack()
        {
            try
            {
                var selected = Ship.selectedUserId;

                if (!Ship.isAttacking || selected > 1000 || !CheckLaser() || LaserCooldownList.Contains(selected) || LaserCooldownList.Count > 2)
                {
                    return;
                }
                if (selected < 1000 && (!Program.Maps.ContainsKey(MapId) || Program.Maps[MapId].Aliens == null || !Program.Maps[MapId].Aliens.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                    return;
                }

                double eX = Program.Maps[MapId].Aliens[selected].X, eY = Program.Maps[MapId].Aliens[selected].Y,
    uX = Ship.x, uY = Ship.y;
                var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));

                if (distancia > 840)
                {
                    if (!Ship.AvisedO)
                    {
                        Ship.AvisedO = true;
                        Send("0|O");
                    }
                }
                else
                {
                    LaserCooldown(selected);

                    Ship.InRange = true;
                    if (Ship.cloaked == 1)
                    {
                        Ship.cloaked = 0;
                        Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        foreach (var user in PlayersInRange)
                        {
                            Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        }
                    }
                    Ship.InSecureArea = false;
                    var random5 = _rnd5.Next(1, 10);
                    Program.Maps[MapId].Aliens[selected].ReceivedAttack(UserId);
                    if (random5 != 5)
                    {
                        if (LaserId == 5)
                        {
                            new Thread(UpdateLaser).Start();

                            if (Ship.AvisedO)
                            {
                                Send("0|X");
                            }
                            Ship.AvisedO = false;
                            var power = Convert.ToInt32(Ship.damage * (LaserId == 5 ? 2 : 1));

                            power = power + new Random().Next(-10, 10);
                            var absorb = 0;

                            var realDamage = power;

                            if (Program.Maps[MapId].Aliens[selected].Shield > 0)
                            {
                                if (Program.Maps[MapId].Aliens[selected].Shield - realDamage > 0)
                                {
                                    Program.Maps[MapId].Aliens[selected].Shield -= Convert.ToUInt32(realDamage);
                                    absorb = realDamage;
                                }
                                else
                                {
                                    if (Program.Maps[MapId].Aliens[selected].Shield - realDamage < 0)
                                    {
                                        absorb = int.Parse(Program.Maps[MapId].Aliens[selected].Shield.ToString());
                                    }

                                    Program.Maps[MapId].Aliens[selected].Shield = 0;
                                }
                            }
                            if (Ship.shield != Ship.maxShield)
                            {
                                var rest = int.Parse(Ship.maxShield.ToString()) - int.Parse(Ship.shield.ToString());
                                if (rest >= uint.Parse((absorb * 2 / 100 * 60).ToString()))
                                    Ship.shield += uint.Parse((absorb * 2 / 100 * 60).ToString());
                                else
                                    Ship.shield = Ship.maxShield;
                            }
                            var packet = "0|a|" + UserId + "|" + selected + "|" + (LaserId - 1) + "|1|1";
                            var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                            var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                            Send(packet);
                            Send(packet2);
                            foreach (var user in PlayersInRange.Where(user => user != UserId))
                            {
                                Program.Users[user].Send(packet);
                            }
                            UpdateShip();

                        }
                        else if (LaserId < 5)
                        {
                            new Thread(UpdateLaser).Start();

                            Ship.InSecureArea = false;
                            if (Ship.AvisedO)
                            {
                                Send("0|X");

                            }
                            Ship.InSecureArea = false;
                            Ship.AvisedO = false;
                            var power = Convert.ToInt32(Ship.damage * LaserId);
                            if (LaserId == 4)
                            {
                                power = power * 1;
                            }
                            var minPower = (power / 18);
                            power = power + new Random().Next(-minPower, minPower);

                            var realDamage = power;

                            if (Program.Maps[MapId].Aliens[selected].Shield > 0)
                            {
                                if (Program.Maps[MapId].Aliens[selected].Shield - realDamage > 0)
                                {
                                    Program.Maps[MapId].Aliens[selected].Shield -= Convert.ToUInt32(realDamage);
                                }
                                else
                                {

                                    Program.Maps[MapId].Aliens[selected].Shield = 0;
                                }
                                realDamage = power - Convert.ToInt32(power * (Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].ShieldAbsorb / 100m));
                            }

                            realDamage = Math.Abs(realDamage);

                            if (Program.Maps[MapId].Aliens[selected].Hp - realDamage < 1 || Program.Maps[MapId].Aliens[selected].Hp > Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].Hp)
                            {
                                Program.Maps[MapId].Aliens[selected].Hp = 0;
                                Ship.isAttacking = false;
                                if (Program.Maps[MapId].Aliens[selected].Destroyed == false)
                                {
                                    Program.Maps[MapId].Aliens[selected].Destroyed = true;
                                    var packet = "0|a|" + UserId + "|" + selected + "|" + (LaserId - 1) + "|1|1";
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";

                                    Send(packet);
                                    Send(packet2);
                                    foreach (var user in Program.Maps[MapId].Aliens[Ship.selectedUserId].Move.PlayersInRange.Where(user => user != UserId))
                                    {
                                        Program.Users[user].Send(packet);
                                    }
                                    UpdateUser(2, selected);
                                }
                                Ship.isAttacking = false;
                            }
                            else
                            {
                                try
                                {
                                    ushort laser = 0;
                                    if (LaserId < 6)
                                    {
                                        laser = LaserId;
                                    }
                                    Program.Maps[MapId].Aliens[selected].Hp -= Convert.ToUInt32(realDamage);
                                    var packet = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield +
                                                  "|" + power + "|100|1";
                                    var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield +
                                                  "|" + power + "|100|1";
                                    if (LaserId < 6)
                                    {
                                        Attackinglaser(power, packet, packet2, packet3, 2);
                                    }
                                }
                                catch (Exception)
                                { }
                            }
                        }
                    }
                    else
                    {
                        var laser = LaserId;
                        var packe = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                        var packe2 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|0|100|1";
                        Send(packe2);
                        Send(packe);
                        foreach (var user in PlayersInRange.Where(user => user != UserId))
                        {
                            Program.Users[user].Send(packe);
                        }

                    }
                }
            }
            catch (Exception)
            { }
        }
        private void AlienRsbAttack()
        {
            try
            {
                var selected = Ship.selectedUserId;
                if (selected > 1000 || !Ship.isAttacking || !CheckLaser() || !Ship.CanUseRSB)
                {
                    return;
                }
                if ((selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                }

                double eX = Program.Maps[MapId].Aliens[selected].X, eY = Program.Maps[MapId].Aliens[selected].Y,
    uX = Ship.x, uY = Ship.y;
                var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));

                if (distancia > 800)
                {
                    if (Ship.AvisedO) return;
                    Ship.AvisedO = true;
                    Send("0|O");
                }
                else
                {
                    Program.Maps[MapId].Aliens[selected].ReceivedAttack(UserId);
                    Ship.InRange = true;
                    if (Ship.cloaked == 1)
                    {
                        Ship.cloaked = 0;
                        Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        foreach (var user in PlayersInRange)
                        {
                            Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        }
                    }
                    new Thread(UpdateLaser).Start();
                    Ship.InSecureArea = false;
                    var random6 = _rnd6.Next(1, 10);
                    if (random6 != 5)
                    {
                        Ship.InSecureArea = false;
                        if (Ship.AvisedO)
                        {
                            Send("0|X");

                        }
                        Ship.InSecureArea = false;
                        Ship.AvisedO = false;
                        var power = Convert.ToInt32(Ship.damage * LaserId);
                        if (LaserId == 4)
                        {
                            power=power;
                        }
                        var minPower = (power / 18);
                        power = power + new Random().Next(-minPower, minPower);

                        var realDamage = power;

                        if (!Ship.CanUseRSB)
                        {
                        }
                        else
                        {
                            if (Program.Maps[MapId].Aliens[selected].Shield > 0)
                            {
                                if (Program.Maps[MapId].Aliens[selected].Shield - realDamage > 0)
                                {
                                    Program.Maps[MapId].Aliens[selected].Shield -= Convert.ToUInt32(realDamage);
                                }
                                else
                                {
                                    if (Program.Maps[MapId].Aliens[selected].Shield - realDamage < 0)
                                    {
                                        realDamage += +(Convert.ToInt32(Program.Maps[MapId].Aliens[selected].Shield) - realDamage);
                                    }

                                    Program.Maps[MapId].Aliens[selected].Shield = 0;
                                }
                                realDamage = power - Convert.ToInt32(power * (Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].ShieldAbsorb / 100m));
                            }

                            realDamage = Math.Abs(realDamage);

                            if (Program.Maps[MapId].Aliens[selected].Hp - realDamage < 1 || Program.Maps[MapId].Aliens[selected].Hp > Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].Hp)
                            {
                                Program.Maps[MapId].Aliens[selected].Hp = 0;
                                Ship.isAttacking = false;
                                if (Program.Maps[MapId].Aliens[selected].Destroyed == false)
                                {
                                    Program.Maps[MapId].Aliens[selected].Destroyed = true;
                                    UpdateUser(2, selected);

                                    try
                                    {

                                        //this.Ship.UCB100 -= 15;

                                        var packet = "0|a|" + UserId + "|" + selected + "|6|1|1";
                                        var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                                        var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";

                                        Attackingrsb(packet, packet2, packet3, 2);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                                Ship.isAttacking = false;
                            }
                            else
                            {
                                try
                                {
                                    Program.Maps[MapId].Aliens[selected].Hp -= Convert.ToUInt32(realDamage);
                                    var packet = "0|a|" + UserId + "|" + selected + "|6|1|1";
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                                    var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                                    Attackingrsb(packet, packet2, packet3, 2);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
                    else
                    {
                        Ship.CanUseRSB = false;
                        Send("0|A|CLD|RSB|3");
                        SetRsb();
                        var packe = "0|a|" + UserId + "|" + selected + "|6|1|1";
                        var packe2 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|0|100|1";
                        Send(packe);
                        Send(packe2);
                        foreach (var user in PlayersInRange.Where(user => user != UserId))
                        {
                            Program.Users[user].Send(packe);
                        }

                    }
                }
            }
            catch (Exception)
            { }
        }
        private void AlienRocketAttack()
        {
            try
            {

                var selected = Ship.selectedUserId;
                if ((selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected)) || (selected > 999 && !Program.Maps[MapId].Users.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                }
                if (selected > 1000 || !Ship.CanUseRockets)
                    return;

                double eX = Program.Maps[MapId].Aliens[selected].X, eY = Program.Maps[MapId].Aliens[selected].Y,
        uX = Ship.x, uY = Ship.y;
                var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));
                if (Ship.logout3 == 1)
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
                    Program.Maps[MapId].Aliens[selected].ReceivedAttack(UserId);
                    Ship.CanUseRockets = false;
                    SetRocket();
                    UpdateRocket();
                    if (Ship.cloaked == 1)
                    {
                        Ship.cloaked = 0;
                        Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        foreach (var user in PlayersInRange)
                        {
                            Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                        }
                    }
                    Ship.InSecureArea = false;
                    var random4 = _rnd4.Next(1, 10);
                    if (random4 != 5)
                    {
                        if (RocketId == 5)
                        {
                            //if (!CanUsePld) return;

                            //CanUsePld = false;
                            //UpdateRocket();
                            //PldCooldown();
                            //Program.Maps[MapId].Aliens[selected].ActivatePld();
                            //var packet = "0|v|" + UserId + "|" + selected + "|P|" + RocketId + "|" + Precision + "|" + Precision;
                            //Send("0|A|CLD|PLA|45");
                            //foreach (var pair in PlayersInRange)
                            //{
                            //    Program.Users[pair].Send(packet);
                            //}

                        }
                        else
                        {
                            var power2 = 900 + new Random().Next(-100, 150);
                            var power = Convert.ToInt32(power2 * RocketId);

                            var realDamage = power;
                            Send("0|A|CLD|ROK|3");

                            if (Program.Maps[MapId].Aliens[selected].Shield > 0)
                            {
                                if (Program.Maps[MapId].Aliens[selected].Shield - realDamage > 0)
                                {
                                    Program.Maps[MapId].Aliens[selected].Shield -= Convert.ToUInt32(realDamage);
                                }
                                else
                                {

                                    Program.Maps[MapId].Aliens[selected].Shield = 0;
                                }
                                realDamage = power - Convert.ToInt32(power * (Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].ShieldAbsorb / 100m));
                            }

                            realDamage = Math.Abs(realDamage);

                            if (Program.Maps[MapId].Aliens[selected].Hp - realDamage < 1 || Program.Maps[MapId].Aliens[selected].Hp > Program.Npcs[Program.Maps[MapId].Aliens[selected].TypeId].Hp)
                            {
                                Program.Maps[MapId].Aliens[selected].Hp = 0;
                                Ship.isAttacking = false;
                                if (Program.Maps[MapId].Aliens[selected].Destroyed == false)
                                {
                                    Program.Maps[MapId].Aliens[selected].Destroyed = true;
                                    var packet = "0|v|" + UserId + "|" + selected + "|H|" + RocketId + "|" + Precision + "|" + Precision;
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";

                                    foreach (var user in Program.Maps[MapId].Aliens[selected].Move.PlayersInRange)
                                    {
                                        Program.Users[user].Send(packet);
                                        Program.Users[user].Send(packet2);
                                    }
                                    if (Program.Maps[MapId].Aliens[selected].Destroyed)
                                    {
                                        UpdateUser(2, selected);
                                    }
                                }
                                Ship.isAttacking = false;
                            }
                            else
                            {

                                Program.Maps[MapId].Aliens[selected].Hp -= Convert.ToUInt32(realDamage);
                                var packet = "0|v|" + UserId + "|" + selected + "|H|" + RocketId + "|" + Precision + "|" + Precision;
                                var packet2 = "0|Y|" + UserId + "|" + selected + "|R|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|" + power + "|100|1";
                                foreach (var user in PlayersInRange)
                                {
                                    Program.Users[user].Send(packet);
                                    if (Program.Users[user].Ship.selectedUserId == selected)
                                        Program.Users[user].Send(packet2);
                                }
                            }
                        }
                    }
                    else
                    {
                        var packe = "0|v|" + UserId + "|" + selected + "|H|" + RocketId + "|" + Precision + "|" + Precision;
                        var packe2 = "0|M|" + UserId + "|" + selected + "|R|" + Program.Maps[MapId].Aliens[selected].Hp + "|" + Program.Maps[MapId].Aliens[selected].Shield + "|0|100|1";
                        foreach (var user in PlayersInRange)
                        {
                            Program.Users[user].Send(packe);
                            if (Program.Users[user].Ship.selectedUserId == selected)
                                Program.Users[user].Send(packe2);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        readonly Random _rnd2 = new Random();
        readonly Random _rnd3 = new Random();
}
}
