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
 * Partial-class User: Спецбоеприпасы: EMP, SMB, ISH, мины, фейерверки.
 */
partial class User
{
        private async void SetLock()
        {
            await Task.Delay(2900);
            Ship.CanBeLocked = true;
        }
        public async void UseEMP()
        {
            var Cooldown = (DateTime.Now - _emp);
            if (Cooldown.TotalSeconds < 40 || Ship.EMPA < 1)
            {
            }
            else
            {
                Send("0|n|EMP|" + UserId); SetCooldown("EMP", 40);
                SetLock();
                updateMines(5);
                Ship.CanBeLocked = false;
                if (Ship.Singularity)
                {
                    Ship.Singularity = false;
                }

                foreach (var user in Program.Maps[MapId].Users)
                {
                    if (user.Key == UserId) continue;
                    var distance = Distance.Get(user.Value.Ship.GetPosX(), Ship.GetPosX(), user.Value.Ship.GetPosY(), Ship.GetPosY());
                    user.Value.Send("0|n|EMP|" + UserId);

                    if (user.Value.Ship.selectedUserId == UserId)
                    {
                        user.Value.Send("0|A|STM|msg_own_targeting_harmed");
                        user.Value.Send("0|N|0");
                        user.Value.Ship.isAttacking = false;
                        user.Value.Ship.selectedUserId = 0;
                    }
                    if (distance < 400 && user.Value.UserId != UserId)
                        user.Value.EmpEffect();
                }
            }
            await Task.Delay(1);
        }
        public async void UseSMB()
        {
            var Cooldown = (DateTime.Now - _smb);
            if (Cooldown.TotalSeconds < 30 || Ship.InSecureArea || Ship.ACM01 < 10 || !Ship.SMBCPU)
            {
            }
            else
            {
                Send("0|n|SMB|" + UserId);
                updateMines(0);
                SetCooldown("SMB", 30);

                foreach (var user in PlayersInRange)
                {
                    if (user == UserId) continue;
                    Program.Users[user].Send("0|n|SMB|" + UserId);
                    var distance = Distance.Get(Program.Users[user].Ship.GetPosX(), Ship.GetPosX(), Program.Users[user].Ship.GetPosY(), Ship.GetPosY());
                    if (distance < 500 && !(!Program.Users[user].Ship.isAttacking && Program.Users[user].Ship.InSecureArea) && user != UserId)
                        Program.Users[user].SmbDamage();
                }
            }
            await Task.Delay(1);
        }
        public async void UseISH()
        {
            var Cooldown = (DateTime.Now - _ish);
            if (Cooldown.TotalSeconds < 22 || Ship.ACM01 < 10 || !Ship.ISHCPU)
            {
            }
            else
            {
                SetCooldown("ISH", 25);
                updateMines(0);
                foreach (var user in PlayersInRange)
                {
                    Program.Users[user].Send("0|n|ISH|" + UserId);
                }
                Ship.ISH = true;
                await Task.Delay(2900);
                Ship.ISH = false;
            }
        }
        public void createMine(int type)
        {
            if (Ship.InSecureArea)
            {
                return;
            }
            if (type == 1 && Ship.ACM01 == 0)
            {
                return;
            }
            else if (type == 2 && Ship.EMPM01 == 0)
            {
                return;
            }
            else if (type == 3 && Ship.SABM01 == 0)
            {
                return;
            }
            else if (type == 4 && Ship.DDM01 == 0)
            {
                return;
            }
            updateMines(type);
            var Cooldown = (DateTime.Now - _mine);
            if (Cooldown.TotalSeconds > 26)
            {

                _mine = DateTime.Now;
                Send("0|A|CLD|MIN|30");
                var id = Program.Random.Next(9, 100000);
                var packet = "0|L|" + id + "|" + type + "|" + Ship.GetPosX() + "|" + Ship.GetPosY();
                Program.Maps[MapId].AddMine(id, Ship.GetPosX(), Ship.GetPosY(), type);
                foreach (var user in Program.Maps[MapId].Users)
                {
                    user.Value.Send(packet);
                }
            }
        }
        private void updateMines(int type = 0)
        {

            switch (type)
            {
                case 0:
                    var a = Program.loadAmmos(UserId, "ammunition_mine_acm-01");
                    if (a != Ship.ACM01)
                        Ship.ACM01 = a;

                    Ship.ACM01 -= 10;
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Ship.ACM01 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_mine_acm-01'");
                    }
                    double Ammo = Ship.ACM01 / 10;
                    Ship.ISHA = (int)Math.Truncate(Ammo);
                    Ship.SMBA = (int)Math.Truncate(Ammo);

                    break;
                case 1:
                    var c = Program.loadAmmos(UserId, "ammunition_mine_acm-01");
                    if (c != Ship.ACM01)
                        Ship.ACM01 = c;

                    Ship.ACM01 -= 1;
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Ship.ACM01 + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_mine_acm-01'");
                    }

                    break;
                case 2:
                    Ship.EMPM01 -= 1;
                    break;
                case 3:
                    Ship.SABM01 -= 1;
                    break;
                case 4:
                    Ship.DDM01 -= 1;
                    break;
                case 5:
                    var b = Program.loadAmmos(UserId, "ammunition_specialammo_emp-01");
                    if (b != Ship.EMPA)
                        Ship.EMPA = b;

                    Ship.EMPA -= 1;
                    using (var dbClient = Program.Manager.getQueryreactor())
                    {
                        dbClient.query("UPDATE server_1_player_all_items SET Q = '" + Ship.EMPA + "' WHERE playerID = '" + UserId + "' AND lootid = 'ammunition_specialammo_emp-01'");
                    }
                    break;
                default:
                    break;
            }
            SendSpAmmo();

        }
        private void SendSpAmmo()
        {
            Send("0|3|" + Ship.R310 + "|" + Ship.PLT2026 + "|" + Ship.PLT2021 + "|" + Ship.PLT3030 + "|" + Ship.PLD8 + "|" + Ship.DCR250 + "|" + Ship.WIZ + "|" + Ship.ACM01 + "|" + Ship.SMBA + "|" + Ship.ISHA + "|" + Ship.EMPA + "|" + Ship.EMPM01 + "|" + Ship.SABM01 + "|" + Ship.DDM01);
        }
        public static string GetHash(int length)
        {
            //rastgele stringimizin oluşturulacağı karakter dizisi
            const string characters = "acbdef"
            + "01345678";
            var randomString = "";
            var rasgele = new Random((int)DateTime.Now.Ticks);
            for (var i = 0; i < length; i++)
            {
                randomString += characters[rasgele.Next(characters.Length)];
            }
            return randomString;
        }

        public void createFireworkBox()
        {
            if (CreatedFireworks == 0)
            {
                return;
            }
            CreatedFireworks++;
            var hash = GetHash(32);
            var packet = "0|L|" + hash + "|333|" + Ship.GetPosX() + "|" + Ship.GetPosY();
            foreach (var user in Program.Maps[MapId].Users)
            {
                user.Value.Send(packet);
            }
            Program.Maps[MapId].AddFireworkBox(hash, Ship.GetPosX(), Ship.GetPosY(), Convert.ToInt32(UserId));
        }

}
}
