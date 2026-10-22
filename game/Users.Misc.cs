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
 * Partial-class User: Мелкие вспомогательные методы: подбор бонус-боксов, телепорт, спам способностями.
 */
partial class User
{
        private void CollectBox(uint id)
        {
            if (!Program.Maps[MapId].BonusBox[id].Collected)
            {
                Program.Maps[MapId].BonusBox[id].Collect(UserId);
            }
        }
        private async void Usetelep()
        {
            try
            {
                await Task.Delay(500);
                Ship.usetelep = false;
            }
            catch (Exception)
            { }
        }
        private async void Spam(string spam, int sec)
        {

            if (!GodMode)
            {
                return;
            }
            while (GodMode && !DestroyedClass)
            {
                switch (spam)
                {
                    case "SMB":
                        new Thread(UseSMB).Start();
                        break;
                    case "ISH":

                        new Thread(UseISH).Start(); break;
                    case "EMP":
                        new Thread(UseEMP).Start();
                        break;
                    case "BRB":
                        ActivateBRB();
                        break;
                    case "SBU":
                        ActivateSBU();
                        break;
                }
                await Task.Delay(sec);
            }
        }
        public void EmpEffect()
        {
            if (Ship.cloaked == 1)
                Ship.cloaked = 0;
            foreach (var user in PlayersInRange)
            {
                Program.Users[user].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
            }

        }
        public void SmbDamage()
        {
            var smbdamage = Ship.HP / 8;

            Ship.HP -= smbdamage;
            Send("0|A|HPT|" + Ship.HP + "|" + Ship.maxHP + "|" + UserId);
            Send("0|A|SHD|" + Ship.shield + "|" + Ship.maxShield + "|" + UserId);
            foreach (var user in PlayersInRange)
            {
                Program.Users[user].Send("0|Y|1|" + UserId + "|L|" + Ship.HP + "|" + Ship.shield + "|" + smbdamage + "|100|1");
            }
        }
        private void Cloak()
        {
            //if (Ship.cloaks <= 0 || Ship.cloaked == 1)

                if (Ship.cloaks <= 0)
            {
                return;
            }
            Ship.cloaked = 1;
            Ship.cloaks -= 1;
            foreach (var ship in PlayersInRange)
            {
                Program.Users[ship].Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
            }
            Send("0|n|INV|" + UserId + "|" + Ship.cloaked);
            Send("0|A|CPU|C|" + Ship.cloaks);

        }

}
}
