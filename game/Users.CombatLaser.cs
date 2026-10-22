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
 * Partial-class User: Лазерная атака по игрокам (LaserAttack).
 */
partial class User
{
        private void LaserAttack()
        {
            try
            {

                var selected = Ship.selectedUserId;

                if (DestroyedClass || !Program.Users.ContainsKey(UserId) || !CheckLaser() || LaserCooldownList.Contains(selected) || LaserCooldownList.Count > 2)
                {
                    return;
                }

                if (Ship.isAttacking == false || selected == 0)
                {
                }
                else if (Ship.selectedUserId == 0)
                {
                    Ship.isAttacking = false;
                }
                else if ((selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected)) || (selected > 999 && !Program.Maps[MapId].Users.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                }
                else if (LaserId > 5)
                {
                }
                else
                {
                    int eX = Convert.ToInt32(Program.Maps[MapId].Users[selected].Ship.x), eY = Convert.ToInt32(Program.Maps[MapId].Users[selected].Ship.y),
                        uX = Convert.ToInt32(Ship.x), uY = Convert.ToInt32(Ship.y);
                    var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));

                    if (Program.Maps[MapId].Users[selected].Ship.isAttacking == false && Program.Maps[MapId].Users[selected].Ship.InSecureArea)
                    {
                        Send("0|P");
                        Program.Maps[MapId].Users[selected].Send("0|P");
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
                        if (!Ship.AvisedO)
                        {
                            Ship.InRange = false;
                            Ship.AvisedO = true;
                            Send("0|O");
                            Program.Maps[MapId].Users[selected].Send("0|V");
                        }
                    }
                    else
                    {
                        LaserCooldown(selected);
                        Program.Maps[MapId].Users[selected].Ship.regen = 0;
                        if (Ship.cloaked == 1)
                        {
                            Ship.cloaked = 0;
                            Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                            foreach (var user in PlayersInRange)
                            {
                                Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
                            }
                        }
                        Ship.InRange = true;
                        Ship.InSecureArea = false;
                        new Thread(UpdateLaser).Start();
                        var random2 = _rnd2.Next(1, 10);
                        if (random2 != 5)
                        {
                            if (LaserId == 5)
                            {
                                if (Ship.AvisedO)
                                {
                                    Send("0|X");
                                }
                                Ship.AvisedO = false;
                                var power = Convert.ToInt32(Ship.damage * (LaserId == 5 ? 2 : 1));
                                power = power + new Random().Next(-10, 10);
                                var absorb = 0;
                                if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum == false)
                                {
                                    power = power / 2;
                                }
                                else if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                                {
                                    power = power / 6;
                                }
                                else if (Ship.Spectrum == false && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                                {
                                    power = power / 3;
                                }
                                else if (Program.Maps[MapId].Users[selected].Ship.ShieldLeech && Program.Maps[MapId].Users[selected].Ship.shield > 0)
                                {
                                    power = (power * 3) / 2;
                                }
                                var realDamage = power;
                                if (Program.Maps[MapId].Users[selected].Ship.ISH)
                                {
                                    realDamage = 0;
                                    power = 0;
                                    absorb = 0;
                                }
                                Program.Maps[MapId].Users[selected].TakeDamage(realDamage);
                                if (Program.Maps[MapId].Users[selected].Ship.shield > 0)
                                {
                                    if (Program.Maps[MapId].Users[selected].Ship.shield - realDamage > 0)
                                    {
                                        Program.Maps[MapId].Users[selected].Ship.shield -= Convert.ToUInt32(realDamage);
                                        absorb = realDamage;
                                    }
                                    else
                                    {
                                        if (Program.Maps[MapId].Users[selected].Ship.shield - realDamage < 0)
                                        {
                                            absorb = int.Parse(Program.Maps[MapId].Users[selected].Ship.shield.ToString());
                                        }
                                        Program.Maps[MapId].Users[selected].Ship.shield = 0;
                                    }
                                }
                                try
                                {
                                    if (Ship.shield != Ship.maxShield)
                                    {
                                        var rest = int.Parse(Ship.maxShield.ToString()) - int.Parse(Ship.shield.ToString());
                                        if (rest >= uint.Parse((absorb * 2 / 100 * 60).ToString()))
                                            Ship.shield += uint.Parse((absorb * 2 / 100 * 60).ToString());
                                        else
                                            Ship.shield = Ship.maxShield;
                                    }
                                    var packet = "0|a|" + UserId + "|" + selected + "|" + (LaserId - 1) + "|1|1";
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                    var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";

                                    Send(packet);
                                    if (Program.Maps[MapId].Users[selected].Ship.ISH)
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
                                        UpdateShip();
                                    }
                                }
                                catch (Exception)
                                {
                                }

                            }
                            else if (LaserId < 5)
                            {

                                Ship.InSecureArea = false;
                                if (Ship.AvisedO)
                                {
                                    Send("0|X");
                                    Program.Maps[MapId].Users[selected].Send("0|X");
                                }
                                Ship.InSecureArea = false;
                                Ship.AvisedO = false;
                                var power = Convert.ToInt32(Ship.damage * LaserId);
                                if (LaserId == 4)
                                {
                                    power = power * 2;
                                }
                                var minPower = (power / 18);
                                power = power + new Random().Next(-minPower, minPower);

                                if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum == false)
                                {
                                    power = power / 2;
                                }
                                else if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                                {
                                    power = power / 6;
                                }
                                else if (Ship.Spectrum == false && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                                {
                                    power = power / 3;
                                }
                                else if (Program.Maps[MapId].Users[selected].Ship.ShieldLeech && Program.Maps[MapId].Users[selected].Ship.shield > 0)
                                {
                                    power = (power * 3) / 2;
                                }
                                var realDamage = power;
                                if (Program.Maps[MapId].Users[selected].Ship.ISH)
                                {
                                    realDamage = 0;
                                    power = 0;
                                }
                                Program.Maps[MapId].Users[selected].TakeDamage(realDamage);

                                if (Program.Maps[MapId].Users[selected].Ship.shield > 0)
                                {
                                    if (Program.Maps[MapId].Users[selected].Ship.shield - realDamage > 0)
                                    {
                                        Program.Maps[MapId].Users[selected].Ship.shield -= Convert.ToUInt32(realDamage);
                                    }
                                    else
                                    {

                                        Program.Maps[MapId].Users[selected].Ship.shield = 0;
                                    }
                                    realDamage = power - Convert.ToInt32(power * (Program.Maps[MapId].Users[selected].Ship.shieldAbsorb / 100m));
                                }

                                realDamage = Math.Abs(realDamage);

                                if (Program.Maps[MapId].Users[selected].Ship.HP - realDamage < 1 || Program.Maps[MapId].Users[selected].Ship.HP > Program.Maps[MapId].Users[selected].Ship.maxHP)
                                {
                                    Program.Maps[MapId].Users[selected].Ship.HP = 0;
                                    Ship.isAttacking = false;
                                    if (Program.Maps[MapId].Users[selected].Ship.destroyed == false)
                                    {
                                        Program.Maps[MapId].Users[selected].Ship.destroyed = true;

                                        Send("0|A|STD|" + " You destroyed " +
                                             Program.Maps[MapId].Users[selected].Ship.Name);
                                        UpdateUser(1, selected);
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
                                        Program.Maps[MapId].Users[selected].Ship.HP -= Convert.ToUInt32(realDamage);

                                        var packet = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                                        var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                        var packet3 = "0|M|" + UserId + "|" + selected + "|P|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                        if (LaserId < 6)
                                        {
                                            Attackinglaser(power, packet, packet2, packet3);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }

                            }
                        }
                        else
                        {
                            ushort laser = 0;
                            laser = LaserId;
                            var packe = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                            var packe2 = "0|M|" + UserId + "|" + selected + "|M|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|0|100|1";
                            Ship.current_hp = Program.Maps[MapId].Users[UserId].Ship.HP;
                            Send(packe);
                            Send(packe2);
                            foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                            {
                                Program.Users[pair].Send(packe);
                            }

                        }
                    }
                    Ship.current_hp = Ship.HP;
                }
            }
            catch (Exception)
            { }
        }
        private void RsbAttack()
        {
            try
            {
                if (DestroyedClass || !Program.Users.ContainsKey(UserId) || !CheckLaser() || !Ship.CanUseRSB)
                {
                    return;
                }
                var selected = Ship.selectedUserId;

                if (Ship.isAttacking == false || selected == 0 || !CheckLaser())
                {
                }
                else if ((selected < 1000 && !Program.Maps[MapId].Aliens.ContainsKey(selected)) || (selected > 999 && !Program.Maps[MapId].Users.ContainsKey(selected)))
                {
                    Ship.isAttacking = false;
                }
                else
                {
                    int eX = Convert.ToInt32(Program.Maps[MapId].Users[selected].Ship.x), eY = Convert.ToInt32(Program.Maps[MapId].Users[selected].Ship.y),
                        uX = Convert.ToInt32(Ship.x), uY = Convert.ToInt32(Ship.y);
                    var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));

                    if (Program.Maps[MapId].Users[selected].Ship.isAttacking == false && Program.Maps[MapId].Users[selected].Ship.InSecureArea)
                    {
                        Send("0|P");
                        Program.Maps[MapId].Users[selected].Send("0|P");
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
                        if (!Ship.AvisedO)
                        {
                            Ship.InRange = false;
                            Ship.AvisedO = true;
                            Send("0|O");
                            Program.Maps[MapId].Users[selected].Send("0|V");
                        }
                    }
                    else
                    {
                        Program.Maps[MapId].Users[selected].Ship.regen = 0;
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
                        var random3 = _rnd3.Next(1, 10);
                        if (random3 != 5)
                        {
                            Ship.InSecureArea = false;
                            if (Ship.AvisedO)
                            {
                                Send("0|X");
                                Program.Maps[MapId].Users[selected].Send("0|X");
                            }
                            Ship.InSecureArea = false;
                            Ship.AvisedO = false;
                            var power = Convert.ToInt32(Ship.damage * LaserId);
                            if (LaserId == 4)
                            {
                                power = power * 2;
                            }
                            var minPower = (power / 18);
                            power = power + new Random().Next(-minPower, minPower);

                            if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum == false)
                            {
                                power = power / 2;
                            }
                            else if (Ship.Spectrum && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                            {
                                power = power / 6;
                            }
                            else if (Ship.Spectrum == false && Program.Maps[MapId].Users[selected].Ship.Spectrum)
                            {
                                power = power / 3;
                            }
                            else if (Program.Maps[MapId].Users[selected].Ship.ShieldLeech && Program.Maps[MapId].Users[selected].Ship.shield > 0)
                            {
                                power = (power * 3) / 2;
                            }
                            var realDamage = power;
                            if (Program.Maps[MapId].Users[selected].Ship.ISH)
                            {
                                realDamage = 0;
                                power = 0;
                            }
                            if (!Ship.CanUseRSB)
                            {
                            }
                            else
                            {
                                Program.Users[selected].TakeDamage(realDamage);
                                Ship.lunchrsb = true;
                                if (Program.Maps[MapId].Users[selected].Ship.shield > 0)
                                {
                                    if (Program.Maps[MapId].Users[selected].Ship.shield - realDamage > 0)
                                    {
                                        Program.Maps[MapId].Users[selected].Ship.shield -= Convert.ToUInt32(realDamage);
                                    }
                                    else
                                    {
                                        if (Program.Maps[MapId].Users[selected].Ship.shield - realDamage < 0)
                                        {
                                            realDamage += +(Convert.ToInt32(Program.Maps[MapId].Users[selected].Ship.shield) - realDamage);
                                        }

                                        Program.Maps[MapId].Users[selected].Ship.shield = 0;
                                    }
                                    realDamage = power - Convert.ToInt32(power * (Program.Maps[MapId].Users[selected].Ship.shieldAbsorb / 100m));
                                }

                                realDamage = Math.Abs(realDamage);

                                if (Program.Maps[MapId].Users[selected].Ship.HP - realDamage < 1 || Program.Maps[MapId].Users[selected].Ship.HP > Program.Maps[MapId].Users[selected].Ship.maxHP)
                                {
                                    Program.Maps[MapId].Users[selected].Ship.HP = 0;
                                    Ship.isAttacking = false;
                                    if (Program.Maps[MapId].Users[selected].Ship.destroyed == false)
                                    {
                                        Program.Maps[MapId].Users[selected].Ship.destroyed = true;
                                        Send("0|A|STD|" + " You destroyed " + Program.Maps[MapId].Users[selected].Ship.Name);
                                        UpdateUser(1, selected);
                                        ushort laser = 0;
                                        if (LaserId == 6)
                                        {
                                            laser = 7;
                                        }
                                        var packet = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                                        var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                        var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";

                                        Attackingrsb(packet, packet2, packet3);
                                    }
                                    Ship.isAttacking = false;
                                }
                                else
                                {
                                    ushort laser = 0;
                                    if (LaserId == 6)
                                    {
                                        laser = 7;
                                    }
                                    Program.Maps[MapId].Users[selected].Ship.HP -= Convert.ToUInt32(realDamage);
                                    var packet = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                                    var packet2 = "0|Y|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                    var packet3 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|" + power + "|100|1";
                                    Attackingrsb(packet, packet2, packet3);
                                }

                            }

                        }
                        else
                        {
                            Ship.CanUseRSB = false;
                            Send("0|A|CLD|RSB|3");
                            SetRsb();
                            ushort laser = 0;
                            if (LaserId == 6)
                            {
                                laser = 7;
                            }
                            var packe = "0|a|" + UserId + "|" + selected + "|" + (laser - 1) + "|1|1";
                            var packe2 = "0|M|" + UserId + "|" + selected + "|L|" + Program.Maps[MapId].Users[selected].Ship.HP + "|" + Program.Maps[MapId].Users[selected].Ship.shield + "|0|100|1";
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
        private void Attackingrsb(string packet, string packet2, string packet3, int num = 1)
        {
            if (num == 1)
            {

                Ship.CanUseRSB = false;
                Send("0|A|CLD|RSB|3");
                foreach (var pair in PlayersInRange)
                {
                    if (Program.Users[Ship.selectedUserId].Ship.ISH)
                    {

                        Program.Users[pair].Send(packet);
                        if (Program.Users[pair].Ship.selectedUserId == Ship.selectedUserId || Ship.selectedUserId == pair)
                            Program.Users[pair].Send(packet3);

                    }
                    else
                    {
                        Program.Users[pair].Send(packet);
                        if (Program.Users[pair].Ship.selectedUserId == Ship.selectedUserId)
                            Program.Users[pair].Send(packet2);
                    }
                }
                SetRsb();

            }
            else
            {
                Ship.CanUseRSB = false;
                Send("0|A|CLD|RSB|3");
                foreach (var pair in PlayersInRange)
                {
                    Program.Users[pair].Send(packet);
                    if (Program.Users[pair].Ship.selectedUserId == Ship.selectedUserId || Ship.selectedUserId == pair)
                        Program.Users[pair].Send(packet2);
                }
                SetRsb();

            }
        }
        private void Attackinglaser(int power, string packet, string packet2, string packet3, int num = 1)
        {
            if (num == 1)
            {
                if (EnergyLeechActive)
                {
                    var hpr = Convert.ToUInt32(power / 10);
                    long hp = Ship.maxHP - Ship.HP;
                    long hp2 = Ship.maxHP - hpr - 1;
                    long hp3 = Ship.maxHP - hpr;
                    long hp4 = Ship.maxHP - 1;
                    if (Ship.HP == Ship.maxHP)
                    {
                    }

                    if (Ship.HP <= hp2)
                    {
                        Ship.HP += hpr;
                        Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.HP + "|" + hpr);
                    }
                    if (Ship.HP >= hp3 && Ship.HP <= hp4)
                    {
                        Ship.HP = Ship.maxHP;
                        Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.HP + "|" + hp);
                    }
                }
                Send(packet);
                if (Program.Users[Ship.selectedUserId].Ship.ISH)
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
            else
            {
                if (EnergyLeechActive)
                {
                    var hpr = Convert.ToUInt32(power / 10);
                    long hp = Ship.maxHP - Ship.HP;
                    long hp2 = Ship.maxHP - hpr - 1;
                    long hp3 = Ship.maxHP - hpr;
                    long hp4 = Ship.maxHP - 1;
                    if (Ship.HP == Ship.maxHP)
                    {
                    }

                    else if (Ship.HP <= hp2)
                    {
                        Ship.HP += hpr;
                        Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.HP + "|" + hpr);
                    }
                    else if (Ship.HP >= hp3 && Ship.HP <= hp4)
                    {
                        Ship.HP = Ship.maxHP;
                        Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.HP + "|" + hp);
                    }
                }
                Send(packet);
                Send(packet2);
                foreach (var pair in PlayersInRange.Where(pair => pair != UserId))
                {
                    Program.Users[pair].Send(packet);
                }
            }
        }

        readonly Random _rnd4 = new Random();
}
}
