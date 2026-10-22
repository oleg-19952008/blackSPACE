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
 * Partial-class User: Технологии и кулдауны: SetCooldown, ELA, BRB, SBU, PrecisionTargeter, ChainImpulse.
 */
partial class User
{
        private void SetCooldown(string S, int duration)
        {
            try
            {
                if (Ship.rank == 21 && GodMode)
                {
                    return;
                }
                var slotone = "none";
                var slottwo = "none";
                var slotthree = "none";

                using (var dbClient = Program.Manager.getQueryreactor())
                {
                    var row = (DataTable)dbClient.query("SELECT slot_one,slot_three,slot_two FROM server_1_player_nanotech WHERE playerId = '" + UserId + "'");
                    slotone = row.Rows[0]["slot_one"].ToString();
                    slottwo = row.Rows[0]["slot_two"].ToString();
                    slotthree = row.Rows[0]["slot_three"].ToString();
                }
                /*
    SRPM -- прицел
    SBRB -- ремонтник
    SSBU -- щит     
    SECI -- молния Хд
    SELA -- трансфер
                 */
                if (slotone != "none" && slotone != "locked")
                {
                    var techone = slotone.Split('|');

                    if (techone[0] == "SRPM")
                        Ship.RPMA = Convert.ToInt32(techone[1]);
                    if (techone[0] == "SBRB")
                        Ship.BRBA = Convert.ToInt32(techone[1]);
                    if (techone[0] == "SSBU")
                        Ship.SBUA = Convert.ToInt32(techone[1]);
                    if (techone[0] == "SECI")
                        Ship.ECIA = Convert.ToInt32(techone[1]);
                    if (techone[0] == "SELA")
                        Ship.ELAA = Convert.ToInt32(techone[1]);
                }
                string[] techtwo;
                if (slottwo != "none" && slottwo != "locked")
                {
                    techtwo = slotone.Split('|');

                    if (techtwo[0] == "SRPM")
                        Ship.RPMA = Convert.ToInt32(techtwo[1]);
                    if (techtwo[0] == "SBRB")
                        Ship.BRBA = Convert.ToInt32(techtwo[1]);
                    if (techtwo[0] == "SSBU")
                        Ship.SBUA = Convert.ToInt32(techtwo[1]);
                    if (techtwo[0] == "SECI")
                        Ship.ECIA = Convert.ToInt32(techtwo[1]);
                    if (techtwo[0] == "SELA")
                        Ship.ELAA = Convert.ToInt32(techtwo[1]);
                }
                if (slotthree != "none" && slotthree != "locked")
                {
                    var techthree = slotone.Split('|');

                    if (techthree[0] == "SRPM")
                        Ship.RPMA = Convert.ToInt32(techthree[1]);
                    if (techthree[0] == "SBRB")
                        Ship.BRBA = Convert.ToInt32(techthree[1]);
                    if (techthree[0] == "SSBU")
                        Ship.SBUA = Convert.ToInt32(techthree[1]);
                    if (techthree[0] == "SECI")
                        Ship.ECIA = Convert.ToInt32(techthree[1]);
                    if (techthree[0] == "SELA")
                        Ship.ELAA = Convert.ToInt32(techthree[1]);
                }

                if (S == "RPM")
                {
                    _rpm = DateTime.Now;
                    Ship.RPMA -= 1;

                    if (slotone.StartsWith("SRPM"))
                    {
                        var rpm = "none";
                        if (Ship.RPMA > 0)
                        {
                            var tech = slotone.Split('|');
                            rpm = tech[0] + "|" + Ship.RPMA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_one = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                    else if (slottwo.StartsWith("SRPM"))
                    {

                        var rpm = "none";
                        if (Ship.RPMA > 0)
                        {
                            var tech = slottwo.Split('|');
                            rpm = tech[0] + "|" + Ship.RPMA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_two = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }

                    }
                    else if (slotthree.StartsWith("SRPM"))
                    {
                        var rpm = "none";
                        if (Ship.RPMA > 0)
                        {
                            var tech = slotthree.Split('|');
                            rpm = tech[0] + "|" + Ship.RPMA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_three = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                }
                else if (S == "BRB")
                {
                    _brb = DateTime.Now;
                    Ship.BRBA -= 1;

                    if (slotone.StartsWith("SBRB"))
                    {
                        var rpm = "none";
                        if (Ship.BRBA > 0)
                        {
                            var tech = slotone.Split('|');
                            rpm = tech[0] + "|" + Ship.BRBA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_one = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                    else if (slottwo.StartsWith("SBRB"))
                    {

                        var rpm = "none";
                        if (Ship.BRBA > 0)
                        {
                            var tech = slottwo.Split('|');
                            rpm = tech[0] + "|" + Ship.BRBA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_two = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }

                    }
                    else if (slotthree.StartsWith("SBRB"))
                    {
                        var rpm = "none";
                        if (Ship.BRBA > 0)
                        {
                            var tech = slotthree.Split('|');
                            rpm = tech[0] + "|" + Ship.BRBA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_three = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                }
                else if (S == "SBU")
                {
                    _sbu = DateTime.Now;
                    Ship.SBUA -= 1;

                    if (slotone.StartsWith("SSBU"))
                    {
                        var rpm = "none";
                        if (Ship.SBUA > 0)
                        {
                            var tech = slotone.Split('|');
                            rpm = tech[0] + "|" + Ship.SBUA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_one = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                    else if (slottwo.StartsWith("SSBU"))
                    {

                        var rpm = "none";
                        if (Ship.SBUA > 0)
                        {
                            var tech = slottwo.Split('|');
                            rpm = tech[0] + "|" + Ship.SBUA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_two = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }

                    }
                    else if (slotthree.StartsWith("SSBU"))
                    {
                        var rpm = "none";
                        if (Ship.SBUA > 0)
                        {
                            var tech = slotthree.Split('|');
                            rpm = tech[0] + "|" + Ship.SBUA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_three = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                }
                else if (S == "ECI")
                {
                    _eci = DateTime.Now;
                    Ship.ECIA -= 1;

                    if (slotone.StartsWith("SECI"))
                    {
                        var rpm = "none";
                        if (Ship.ECIA > 0)
                        {
                            var tech = slotone.Split('|');
                            rpm = tech[0] + "|" + Ship.ECIA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_one = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                    else if (slottwo.StartsWith("SECI"))
                    {

                        var rpm = "none";
                        if (Ship.ECIA > 0)
                        {
                            var tech = slottwo.Split('|');
                            rpm = tech[0] + "|" + Ship.ECIA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_two = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }

                    }
                    else if (slotthree.StartsWith("SECI"))
                    {
                        var rpm = "none";
                        if (Ship.ECIA > 0)
                        {
                            var tech = slotthree.Split('|');
                            rpm = tech[0] + "|" + Ship.ECIA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_three = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                }
                else if (S == "ELA")
                {
                    _ela = DateTime.Now;
                    Ship.ELAA -= 1;

                    if (slotone.StartsWith("SELA"))
                    {
                        var rpm = "none";
                        if (Ship.ELAA > 0)
                        {
                            var tech = slotone.Split('|');
                            rpm = tech[0] + "|" + Ship.ELAA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_one = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                    else if (slottwo.StartsWith("SELA"))
                    {

                        var rpm = "none";
                        if (Ship.ELAA > 0)
                        {
                            var tech = slottwo.Split('|');
                            rpm = tech[0] + "|" + Ship.ELAA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_two = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }

                    }
                    else if (slotthree.StartsWith("SELA"))
                    {
                        var rpm = "none";
                        if (Ship.ELAA > 0)
                        {
                            var tech = slotthree.Split('|');
                            rpm = tech[0] + "|" + Ship.ELAA + "|" + tech[2];
                        }

                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_player_nanotech SET slot_three = '" + rpm + "' WHERE playerId = '" + UserId + "'");
                        }
                    }
                }
                else if (S == "SMB")
                {
                    _smb = DateTime.Now;
                }
                else if (S == "EMP")
                {
                    _emp = DateTime.Now;
                }
                else if (S == "ISH")
                {
                    _ish = DateTime.Now;
                }
                Send("0|A|CLD|" + S + "|" + duration);

            }
            catch { }
        }
        public async void ActivateELA()
        {
            var Cooldown = (DateTime.Now - _ela);
            if (Cooldown.TotalSeconds < 120 || Ship.ELAA == 0)
            {
            }
            else
            {
                EnergyLeechActive = true;
                Send("0|TX|A|0|ELA|" + UserId);
                SetCooldown("ELA", 120);
                await Task.Delay(60000);

                EnergyLeechActive = false;
                Send("0|TX|D|0|ELA|" + UserId);
            }
        }
        public async void ActivateBRB()
        {
            try
            {
                var Cooldown = (DateTime.Now - _brb);
                if (Cooldown.TotalSeconds < 38 || Ship.BRBA == 0)
                {
                }
                else
                {
                    SetCooldown("BRB", 45);
                    foreach (var user in PlayersInRange)
                    {
                        Program.Users[user].Send("0|TX|A|R|BRB|" + UserId);
                    }
                    var count = 1;
                    while (count <= 10)
                    {
                        BRBRepair();

                        await Task.Delay(1000);

                        count++;
                    }
                    foreach (var user in PlayersInRange)
                    {
                        Program.Users[user].Send("0|TX|D|R|BRB|" + UserId);
                    }
                }
            }
            catch { }
        }
        private void BRBRepair()
        {
            try
            {
                if (!Program.Users.ContainsKey(UserId))
                {
                    return;
                }
                if (Ship.GetShipHP() == Ship.GetShipMaxHP())
                {
                    Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.GetShipHP() + "|0");

                }
                else if (Ship.GetShipHP() + 10000 > Ship.GetShipMaxHP())
                {
                    long hp = Ship.GetShipMaxHP() - Ship.GetShipHP();
                    Ship.HP = Ship.GetShipMaxHP();
                    Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.GetShipHP() + "|" + hp);
                }
                else
                {
                    Ship.HP += 10000;
                    Send("0|A|HL|0|" + UserId + "|HPT|" + Ship.GetShipHP() + "|10000");
                }
            }
            catch { }
        }
        public void ActivateSBU()
        {
            var Cooldown = (DateTime.Now - _sbu);
            if (Cooldown.TotalSeconds < 26 || Ship.SBUA == 0)
            {
            }
            else
            {
                SetCooldown("SBU", 30);
                if (Ship.shield + 75000 > Ship.maxShield)
                {
                    long shield = Ship.maxShield - Ship.shield;
                    Ship.shield = Ship.maxShield;
                    Send("0|A|HL|0|" + UserId + "|SHD|" + Ship.shield + "|" + shield);
                    Send("0|A|CLD|SBU|30");
                    foreach (var user in PlayersInRange)
                    {
                        Program.Users[user].Send("0|TX|A|0|SBU|" + UserId);
                    }
                }
                else
                {
                    Ship.shield += 75000;
                    Send("0|A|HL|0|" + UserId + "|SHD|" + Ship.shield + "|75000");
                    foreach (var user in PlayersInRange)
                    {
                        Program.Users[user].Send("0|TX|A|0|SBU|" + UserId);
                    }
                }
            }
        }
        public async void PrecisionTargeter()
        {
            var Cooldown = (DateTime.Now - _rpm);
            if (Cooldown.TotalSeconds < 150 || Ship.RPMA == 0)
            {
            }
            else
            {
                SetCooldown("RPM", 200);
                Precision = 2;
                await Task.Delay(60000);
                Precision = 0;
            }
        }
        public void ChainImpulse()
        {
            var Cooldown = (DateTime.Now - _eci);
            if (Cooldown.TotalSeconds < 60 || Ship.selectedUserId == 0 || Ship.ECIA == 0)
            {
            }
            else
            {
                SetCooldown("ECI", 60);
                var target = Ship.selectedUserId.ToString();
                var nX = Program.Users[Ship.selectedUserId].Ship.GetPosX();
                var nY = Program.Users[Ship.selectedUserId].Ship.GetPosY();
                var MaxTargets = 1;

                foreach (var user in PlayersInRange)
                {
                    var pX = Program.Users[user].Ship.GetPosX();
                    var pY = Program.Users[user].Ship.GetPosY();
                    var distance = Convert.ToInt32(Distance.Get(pX, nX, pY, nY));
                    if (distance < 800 && UserId != user && Ship.selectedUserId != user)
                    {
                        MaxTargets++;
                        target += "|" + user;
                    }
                    if (MaxTargets == 7)
                    {
                        break;
                    }
                }
                foreach (var user in PlayersInRange)
                {
                    Program.Users[user].Send("0|TX|ECI|R|" + UserId + "|" + target);
                }
                var Targets = target.Split('|');
                foreach (var userid in Targets)
                {
                    var _userID = UInt32.Parse(userid);
                    if (Program.Users[_userID].Ship.HP - 9970 < 1)
                    {
                        //foreach (var pir in Program.Maps[mapId].Users)
                        //{);
                    }
                    else
                    {
                        Program.Users[_userID].Ship.HP -= 9970;
                    }
                    Program.Users[UserId].Send("0|Y|1|" + _userID + "|L|" + Program.Users[_userID].Ship.HP + "|" + Program.Users[_userID].Ship.shield + "|9970|100|1");
                    Program.Users[_userID].Send("0|Y|1|" + _userID + "|L|" + Program.Users[_userID].Ship.HP + "|" + Program.Users[_userID].Ship.shield + "|9970|100|1");
                    Program.Users[_userID].Send("0|A|HPT|" + Program.Users[_userID].Ship.HP + "|" + Program.Users[_userID].Ship.maxHP + "|" + _userID);
                    Program.Users[_userID].Send("0|A|SHD|" + Program.Users[_userID].Ship.shield + "|" + Program.Users[_userID].Ship.maxShield + "|" + _userID);
                }
            }
        }
}
}
