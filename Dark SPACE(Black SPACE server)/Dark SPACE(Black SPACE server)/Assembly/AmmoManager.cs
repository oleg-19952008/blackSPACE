using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Do.Assembly
{
   class AmmoManager
    {
        public DateTime ISH = new DateTime(2017, 3, 1, 7, 0, 0);
        public DateTime EMP = new DateTime(2017, 3, 1, 7, 0, 0);
        public DateTime SMB = new DateTime(2017, 3, 1, 7, 0, 0);
        public DateTime Mine = new DateTime(2017, 3, 1, 7, 0, 0);
        public DateTime Fwx = new DateTime(2017, 3, 1, 7, 0, 0);
        public uint CreatedFireworks = 0;

        public uint UserId { get; set; }
        public AmmoManager(uint userID)
        {
            this.UserId = userID;
        }
      
        #region EMP
        private async void SetLock()
        {
            try
            {
                await Task.Delay(2900);
                Program.Users[UserId].Ship.CanBeLocked = true;
            }
            catch { }
        }
       private void SetCooldown(string S, int duration)
        {
           if(Program.Users[UserId].Ship.rank==21 && Program.Users[UserId].GodMode)
           {
               return;
           }
            switch(S)
            {
                case "SMB":
                    SMB = System.DateTime.Now;
                    break;
                case "EMP":
                    EMP = System.DateTime.Now;
                    break;
                case "ISH":
                    ISH = System.DateTime.Now;
                    break;
            }
            Do.Logger.Log(this.UserId.ToString(),"Special", "Just fired a "+S);

    Program.Users[UserId].Send("0|A|CLD|"+S+"|"+duration);
        }
        public void UseEMP()
        {
            var Cooldown = (System.DateTime.Now - EMP);
            if (Cooldown.TotalSeconds < 40)
            {
                return;
            }
            SetCooldown("EMP", 40);
            this.SetLock();
            Program.Users[UserId].Ship.CanBeLocked = false;
            if (Program.Users[UserId].Ship.Singularity)
            {
                Program.Users[UserId].Ship.Singularity = false;
            }
            foreach (var user in Program.Maps[Program.Users[UserId].MapId].Users)
            {
                var distance = Distance.Get(user.Value.Ship.GetPosX(), Program.Users[UserId].Ship.GetPosX(), user.Value.Ship.GetPosY(), Program.Users[UserId].Ship.GetPosY());
                if (distance < 2000)
                    user.Value.Send("0|n|EMP|" + this.UserId);
                if(user.Value.Ship.selectedUserId==UserId)
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
        #endregion
        #region SMB
        public void UseSMB()
        {
            var Cooldown = (System.DateTime.Now - SMB);
            if (Cooldown.TotalSeconds < 30 || Program.Users[UserId].Ship.InSecureArea)
            {
                return;
            }
            SetCooldown("SMB", 30);
            foreach (var user in Program.Maps[Program.Users[UserId].MapId].Users)
            {
                var distance = Distance.Get(user.Value.Ship.GetPosX(),Program.Users[UserId].Ship.GetPosX(),user.Value.Ship.GetPosY(),Program.Users[UserId].Ship.GetPosY());
                if (distance < 2000)
                   user.Value.Send("0|n|SMB|" + this.UserId);
               if (distance < 500 && !(!user.Value.Ship.isAttacking && user.Value.Ship.InSecureArea) && user.Value.UserId!=UserId)
                   user.Value.SmbDamage();
            } 
        }
        #endregion
        #region ISH
        public async void UseISH()
        {
            var Cooldown = (System.DateTime.Now - ISH);
            if (Cooldown.TotalSeconds < 25)
            {
                return;
            }
            SetCooldown("ISH", 25);
            foreach (var user in Program.Maps[Program.Users[UserId].MapId].Users)
            {
                var distance = Distance.Get(user.Value.Ship.GetPosX(), Program.Users[UserId].Ship.GetPosX(), user.Value.Ship.GetPosY(), Program.Users[UserId].Ship.GetPosY());
                if (distance < 2000)
                user.Value.Send("0|n|ISH|" + this.UserId);
            }
            Program.Users[UserId].Ship.ISH = true;
            await Task.Delay(2900);
            Program.Users[UserId].Ship.ISH = false;
        }
        #endregion
        #region Mine
        public void createMine(int type)
        {
            if (Program.Users[UserId].Ship.InSecureArea)
            {
                return;
            }
            var Cooldown = (System.DateTime.Now - this.Mine);
            if (Cooldown.TotalSeconds < 30)
            {
                return;
            }
            this.Mine = System.DateTime.Now;
            Program.Users[UserId].Send("0|A|CLD|MIN|30");
            var id = Program.Random.Next(9, 100000);
            var packet = "0|L|" + id + "|" + type + "|" + Program.Users[UserId].Ship.GetPosX() + "|" + Program.Users[UserId].Ship.GetPosY();
            Program.Maps[Program.Users[UserId].MapId].AddMine(id, Program.Users[UserId].Ship.GetPosX(), Program.Users[UserId].Ship.GetPosY(), type);
            foreach (var user in Program.Maps[Program.Users[UserId].MapId].Users)
            {
                user.Value.Send(packet);
            } 
        }

        #endregion
        #region Fireworks
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
            if(CreatedFireworks>4)
            {
                return;
            }
            CreatedFireworks++;
            var hash = GetHash(32);
            var packet = "0|L|" + hash + "|333|" + Program.Users[UserId].Ship.GetPosX() + "|" + Program.Users[UserId].Ship.GetPosY();
            foreach (var user in Program.Maps[Program.Users[UserId].MapId].Users)
            {
                user.Value.Send(packet);
            }
            Program.Maps[Program.Users[UserId].MapId].AddFireworkBox(hash, Program.Users[UserId].Ship.GetPosX(), Program.Users[UserId].Ship.GetPosY(), Convert.ToInt32(this.UserId));
        }
         
        #endregion
        #region SendAmmoInfo
        #endregion
    }
}
