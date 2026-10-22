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
 * Partial-class User: Активные способности корабля: невидимость, яд, уменьшитель, спектр, часовой, кулдауны способностей.
 */
partial class User
{
        private void UseAbility()
        {
            try
            {
                if (!Ship.CanUseAbility)
                {
                    return;
                }
                Ship.CanUseAbility = false;
                AbilityCooldown();
                switch (Ship.Id)
                {
                    case 63:
                        {
                            Send("0|SD|S|1|3|160");

                            foreach (var ship in PlayersInRange)
                            {
                                Program.Users[ship].Send("0|SD|A|R|1|" + UserId);
                            }
                            var repair = Ship.HP + (Ship.maxHP / 3);
                            if (repair > Ship.maxHP)
                            {
                                var solace = Ship.maxHP - Ship.HP;
                                Ship.HP = Ship.maxHP;
                                Send("0|A|HPT|" + Ship.HP + "|" + Ship.maxHP + "|" + UserId);
                                Send("0|A|HL|1|" + UserId + "|HPT|" + Ship.HP + "|" + solace);
                            }
                            else
                            {
                                Ship.HP += (Ship.maxHP / 3);
                                Send("0|A|HL|1|" + UserId + "|HPT|" + Ship.HP + "|" + Ship.maxHP / 3);
                                Send("0|A|HPT|" + Ship.HP + "|" + Ship.maxHP + "|" + UserId);
                            }
                        }
                        break;
                    case 64:
                        if (Program.Maps[MapId].Users.ContainsKey(Ship.selectedUserId))
                        {
                            if (Program.Users[Ship.selectedUserId].Ship.InSecureArea)
                            {
                            }
                            else
                            {
                                Program.Users[Ship.selectedUserId].Ship.ShieldLeech = true;
                                DiminisherCooldown();
                                Program.Users[Ship.selectedUserId].DiminisherCooldown();

                                foreach (var ship in PlayersInRange)
                                {
                                    Program.Users[ship].Send("0|SD|A|R|2|" + UserId);
                                    Program.Users[ship].Send("0|SD|A|R|2|" + Ship.selectedUserId);
                                }
                                Send("0|SD|S|2|1|160");

                            }
                        }
                        break;
                    case 65:
                        foreach (var ship in Program.Maps[MapId].Users.Values)
                        {
                            ship.Send("0|SD|A|R|3|" + Ship.Id + "|" + UserId);
                        }
                        Send("0|SD|S|3|1|160");
                        Ship.Spectrum = true;
                        SpectrumCooldown();
                        break;
                    case 18:
                        //venik
                        foreach (var ship in PlayersInRange)
                        {
                            Program.Users[ship].Send("0|SD|A|R|6|" + Ship.Id + "|" + UserId);

                        }
                        var old_speed = Ship.speed;
                        var new_speed = old_speed + (old_speed / 2);
                        Send("0|A|v|" + new_speed);
                        Ship.speed = new_speed;
                        foreach (var ship in PlayersInRange)
                        {
                            Program.Users[ship].Send("0|SD|A|0|6|" + UserId);
                            //Program.Users[ship].Send("0|SD|A|R|6|" + UserId);
                        }
                        Send("0|SD|S|6|1|160");

                        AfterBurnerColdown();
                        break;
                    case 66:
                        Ship.Fortress = true;
                        foreach (var ship in PlayersInRange)
                        {
                            Program.Users[ship].Send("0|SD|A|R|4|" + UserId);
                        }
                        Send("0|SD|S|4|1|160");
                        SentinelCooldown();
                        break;
                    case 67:
                        if (Program.Maps[MapId].Users.ContainsKey(Ship.selectedUserId))
                        {
                            if (Program.Users[Ship.selectedUserId].Ship.InSecureArea)
                            {
                            }
                            else
                            {
                                Program.Users[Ship.selectedUserId].Ship.Singularity = true;

                                foreach (var ship in Program.Maps[MapId].Users.Values)
                                {
                                    ship.Send("0|SD|A|R|5|" + UserId);
                                    ship.Send("0|SD|A|R|5|" + Ship.selectedUserId);
                                }
                                Send("0|SD|S|5|1|160");

                                Program.Users[Ship.selectedUserId].Venom(UserId);
                                Program.Users[Ship.selectedUserId].VenomCooldown();
                                VenomCooldown();
                            }
                        }
                        break;
                }
            }
            catch { }

        }

        private async void Venom(uint userId)
        {
            // БЫЛО: while(true) без задержки на первой итерации и без try/catch —
            // любое исключение "тихо" убивало async void, а при быстрой смерти цели
            // цикл мог крутиться без паузы. СТАЛО: проверяем условие выхода ДО тела,
            // задержка есть на каждой итерации, ошибки логируются, отмена по CancellationToken.
            var token = _venomCts.Token;
            // Снимок HP на момент входа: Venom снимает HP серверного корабля каждые ~1.1с,
            // но клиент синхронизирует полоску HP только пакетами HPT/HL (см. регрессию
            // "корабль не имеет хп"). Поэтому каждый тик шлём и "0|A|HPT|hp|max|uid",
            // чтобы клиентская полоска не оставалась с рассинхронизированным значением.
            uint lastReportedHp = Ship.HP;
            try
            {
                while (!token.IsCancellationRequested && !DestroyedClass && Ship.Singularity)
                {
                    await Task.Delay(1100, token);

                    var sing = Convert.ToUInt32(Program.Random.Next(2500, 3000));
                    if (Ship.HP - sing < 1)
                    {
                        foreach (var ship in PlayersInRange)
                        {
                            Program.Users[ship].Send("0|K|" + UserId);
                        }
                        discconectUser();
                        Program.RemoveUser(UserId);
                        Program.Maps[MapId].Users.Remove(UserId);
                        break;
                    }
                    else
                    {
                        Ship.HP -= sing;

                        var packet2 = "0|Y|" + userId + "|" + UserId + "|L|" + Ship.HP + "|" + Ship.shield + "|" + sing + "|100|1";
                        foreach (var ship in PlayersInRange.Where(ship => Program.Users[ship].Ship.selectedUserId == Ship.selectedUserId || Ship.selectedUserId == ship))
                        {
                            Program.Users[ship].Send(packet2);
                        }

                        // Синхронизация клиентской полоски HP (см. комментарий выше).
                        if (Ship.HP != lastReportedHp)
                        {
                            Send("0|A|HPT|" + Ship.HP + "|" + Ship.maxHP + "|" + UserId);
                            lastReportedHp = Ship.HP;
                        }
                    }
                }

                // Деактивация эффекта на клиенте отправляем ВСЕГДА при выходе из цикла
                // (в т.ч. после отмены токена), а не только пока Singularity ещё true —
                // иначе у игроков останется "висячий" индикатор способностей.
                if (!DestroyedClass)
                {
                    foreach (var ship in PlayersInRange)
                    {
                        Program.Users[ship].Send("0|SD|D|R|5|" + UserId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальная отмена (пользователь отключился).
            }
            catch (Exception e)
            {
                Out.WriteLine("Venom loop error: " + e.Message, "User.Venom", ConsoleColor.Red);
            }
        }
        private readonly CancellationTokenSource _venomCts = new CancellationTokenSource();

        private async void DiminisherCooldown()
        {
            await Task.Delay(25000);
            foreach (var user in PlayersInRange)
            {
                Program.Users[user].Send("0|SD|D|R|2|" + UserId);
            }
            if (Ship.ShieldLeech)
            {
                Ship.ShieldLeech = false;
            }

        }
        private async void VenomCooldown()
        {
            await Task.Delay(25000);

            foreach (var ship in PlayersInRange)
            {
                Program.Users[ship].Send("0|SD|D|R|5|" + UserId);
            }

            if (Ship.Singularity)
            {
                Ship.Singularity = false;
            }

        }
        public async void AfterBurnerColdown()
        {
            await Task.Delay(25000);

            foreach (var ship in PlayersInRange)
            {
                Program.Users[ship].Send("0|SD|D|R|6|" + UserId);
                var old_speed = Ship.speed;
                var new_speed = old_speed + (old_speed / 2);
                Send("0|A|v|" + old_speed);
                Ship.speed = old_speed;
            }
            var old_speed_ = Ship.speed;
            var new_speed_ = old_speed_ + (old_speed_ / 2);
            Send("0|A|v|" + old_speed_);
            Ship.speed = old_speed_;

        }
        private async void SpectrumCooldown()
        {
            await Task.Delay(25000);

            foreach (var ship in PlayersInRange)
            {
                Program.Users[ship].Send("0|SD|D|R|3|" + UserId);
            }
            Ship.Spectrum = false;
        }
        private async void SentinelCooldown()
        {
            try
            {
                await Task.Delay(25000);

                foreach (var ship in PlayersInRange)
                {
                    Program.Users[ship].Send("0|SD|D|R|4|" + UserId);
                }

                Ship.Fortress = false;

            }
            catch { }
        }
        private async void AbilityCooldown()
        {
            await Task.Delay(120000);

            Ship.CanUseAbility = true;

        }
}
}
