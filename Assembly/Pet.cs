using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Do.game; // User (Program.Users)
using Do.serverGame; // Map, Alien (Program.Maps)

namespace Do.Assembly
{
    class Pet
    {
        public uint PetId;
        public bool PetActive = false, Kami = false, GuardMode = false, Destroyed = false;
        public uint KamikazeLvl;
        public int Hp, Shield, Experience, Damage;
        public int X, Y;
        public uint Speed;
        public DateTime KamiCld = new DateTime(2017, 3, 1, 7, 0, 0);
        public uint UserId { get; set; }
        public Pet(uint userId, int random)
        {
            try
            {
                UserId = userId;
                PetId = uint.Parse(random + "" + userId.ToString(CultureInfo.InvariantCulture));
                //    PetId = userId + 10000;
                X = Program.Users[userId].Ship.x;
                Y = Program.Users[userId].Ship.y;
                Speed = Program.Users[userId].Ship.speed;
                KamikazeLvl = 3;
                Hp = 10000;
                Shield = 10000;
                Damage = 3000;
            }
            catch { }
        }
        public void Activate()
        {
            try
            {
              
                var exp = Do.game.User.exp_/900;
            
                PetActive = true;
                Speed = Program.Users[UserId].Ship.speed;
                X = Program.Users[UserId].Ship.GetPosX();
                Y = Program.Users[UserId].Ship.GetPosY();
                Program.Users[UserId].Send("0|PET|I|1|1|1");
                Program.Users[UserId].Send("0|PET|S|1|15|"+ exp +"|"+(exp+10000)+"|999|999|999|999|1|15|1|[PET 15] " + Program.Users[UserId].Ship.Name);
         
                Program.Users[UserId].Send("0|PET|G|A|101"); // passive
                Program.Users[UserId].Send("0|PET|G|SEL|101");
                Program.Users[UserId].Send("0|PET|G|A|102"); // guard
                                                             //Program.Users[userID].Send("0|PET|G|A|1"); // autolot
                                                             //Program.Users[userID].Send("0|PET|G|A|2"); // rescoll
                                                             //Program.Users[UserId].Send("0|PET|G|A|6"); // rep
                Program.Users[UserId].Send("0|PET|G|A|7"); // kami

           

                //  this.Send("0|PET|S|15|12|exp|exp|hp|maxhp|shd|maxshd|fuel|maxfuel|15|14|13");
                for (var index = 0; index < Program.Maps[Program.Users[UserId].MapId].Users.Count; index++)
                {
                    var s = Program.Users[UserId];

                    Program.Maps[Program.Users[UserId].MapId].Users.ElementAt(index).Value.Send("0|PET|L|1|" + (s.UserId + 1000) + "|15|[PET 15] " + Program.Users[UserId].Ship.Name + " |" + Program.Users[UserId].Ship.factionId + "|1|1|" + Program.Users[UserId].Ship.tagName + "|0|" + X + "|" + Y + "|1|1|");
                 //   Program.Maps[Program.Users[UserId].MapId].Users.ElementAt(index).Value.Send("0|n|d|" + (s.UserId + 1000) + "|" + Program.Users[UserId].Ship.DronePacket);
                }
                Movement();
                AttackLoop();
            }
            catch (Exception e)
            {
           //     Out.WriteLine(e.StackTrace);
            }
        }
        public void DeActivate()
        {
            try
            {
                var s = Program.Users[UserId];
                PetActive = false;
                Kami = false;
                GuardMode = false;
                for (var index = 0; index < Program.Maps[Program.Users[UserId].MapId].Users.Count; index++)
                {
                    Program.Maps[Program.Users[UserId].MapId].Users.ElementAt(index).Value.Send("f|R|" + (s.UserId + 1000));
                }
                Program.Users[UserId].Send("0|PET|D");
            }
            catch (Exception e)
            {
              //  Out.WriteLine(e.StackTrace);
            }
        }
        public void Destroy()
        {
            try
            {
                var s = Program.Users[UserId];
                PetActive = false;
                Destroyed = true;
                Kami = false;
                GuardMode = false;
                for (var index = 0; index < Program.Maps[Program.Users[UserId].MapId].Users.Count; index++)
                {
                    Program.Maps[Program.Users[UserId].MapId].Users.ElementAt(index).Value.Send("0|K|" + (s.UserId + 1000));
                }
                Program.Users[UserId].Send("0|PET|Z");
            }
            catch (Exception e)
            {
                //Out.WriteLine(e.StackTrace);
            }
        }
        public void Repair()
        {
            Program.Users[UserId].Send("0|PET|R");
        }
        #region Gears
        public void SelectGear(string content)
        {
            try
            {
                if (!PetActive)
                {
                    return;
                }
                GuardMode = false;
                Kami = false;
                var packet = content.Split('|');
                var gear = Convert.ToInt32(packet[3]);
                switch (gear)
                {
                    case 7:
                        Kamikaze();
                        break;
                    case 102:
                        AttackLoop();
                        GuardMode = true;
                        break;
                }
            }
            catch (Exception e)
            {
                Out.WriteLine(e.StackTrace);
            }
        }
        #region Kamikaze
        public async void Kamikaze()
        {
            try
            {
                var cooldown = (DateTime.Now - KamiCld);
                if (cooldown.TotalSeconds < 30)
                {
                    var a = 30 - Convert.ToInt32(cooldown.TotalSeconds);
                    Program.Users[UserId].Send("0|A|STD|Kamikaze is on cooldown! " + a + " seconds left!");
                    return;
                }
                var exp = Do.game.User.exp_;
                KamiMove();
                await Task.Delay(3500);
                var damage = (exp / 2010) * 2;
           //         (uint) 25000;// KamikazeLvl * 20000; //урон камиком
                if (!PetActive)
                {
                    return;
                }
                KamiCld = DateTime.Now;

                //foreach (var user in Program.Maps[Program.Users[userID].mapId].Users.Values)
                //{
                //    double distance = Math.Sqrt(Math.Pow(Program.Users[userID].Ship.x - x, 2) + Math.Pow(Program.Users[userID].Ship.y - y, 2));
                //    if (distance < 330 && Program.Users[userID].userId != this.userID)
                //    {
                //        Send("0|Y|1|" + Program.Users[userID].userId + "|L|" + Program.Users[userID].Ship.HP + "|" + Program.Users[userID].Ship.shield + "|" + damage + "|100|1");
                //        if (Program.Users[userID].Ship.HP - damage < 1)
                //        {
                //            Program.Users[userID].Ship.HP = 0;
                //            Program.Maps[Program.Users[userID].mapId].Users.Remove(this.userID);
                //            foreach (var ship in Program.Maps[Program.Users[userID].mapId].Users.Values)
                //            {
                //                ship.Send("0|K|" + this.userID);
                //            }
                //            Program.Users[userID].discconectUser(1);

                //        }
                //        else
                //        {
                //            Program.Users[userID].Ship.HP -= damage;
                //        }
                //        Program.Users[userID].Send("0|A|HPT|" + Program.Users[userID].Ship.HP + "|" + Program.Users[userID].Ship.maxHP + "|" + Program.Users[userID].userId);
                //        Program.Users[userID].Send("0|A|SHD|" + Program.Users[userID].Ship.shield + "|" + Program.Users[userID].Ship.maxShield + "|" + Program.Users[userID].userId);

                //    }
                //}
                foreach (var alien in from alien in Program.Maps[Program.Users[UserId].MapId].Aliens.Values let distance = Math.Sqrt(Math.Pow(alien.X - X, 2) + Math.Pow(alien.Y - Y, 2)) where distance < 330 select alien)
                {
                    Send("0|Y|1|" + alien.Id + "|L|" + alien.Hp + "|" + alien.Shield + "|" + damage + "|100|1");
                    if (alien.Hp - damage < 1)
                    {
                        alien.SelectedUserId = 0;
                        alien.Destroyed = true;
                        alien.Hp = 0;
                        foreach (var ship in Program.Maps[Program.Users[UserId].MapId].Aliens[alien.Id].Move.PlayersInRange)
                        {
                            Program.Users[ship].Send("0|K|" + alien.Id);
                        }
                        //Program.Users[userID].UpdateUser(2);
                        alien.Regenerate();
                    }
                    else
                    {
                        alien.Hp -= (uint)damage;
                    }
                }
                Destroy();
            }
            catch (Exception e)
            {
             //   Out.WriteLine(e.StackTrace);
            }
        }
        private async void KamiMove()
        {
            try
            {
                Speed = 1400;
                while (Kami && PetActive)
                {
                    Move();
                    await Task.Delay(500);
                }
            }
            catch (Exception e)
            {
           //     Out.WriteLine(e.StackTrace);
            }
        }
        #endregion
        #endregion
        #region Attack
        private async void AttackLoop()
        {
            try
            {
                while (PetActive)
                {
                    if (GuardMode)
                    {
                        Attack();
                    }
                    await Task.Delay(900);
                }
            }
            catch (Exception e)
            {
            //    Out.WriteLine(e.StackTrace);
            }
        }

        private void Attack()
        {
            try
            {
                if (!Program.Users[UserId].Ship.isAttacking || !Program.Users[UserId].Ship.InRange)
                {
                    return;
                }
                var userId = Program.Users[UserId].Ship.selectedUserId;
                var enemy = Program.Users[userId];
                var power = Convert.ToInt32(Program.Users[UserId].Ship.damage * (Program.Users[UserId].LaserId == 5 ? 2 : 1));
                var realDamage = power;
                power = power + new Random().Next(-10, 10);
                var packet = "0|a|" + PetId + "|" + userId + "|" + (Program.Users[UserId].LaserId - 1) + "|1|1";
                var packet2 = "0|Y|" + PetId + "|" + userId + "|L|" + enemy.Ship.HP + "|" + enemy.Ship.shield + "|" + power + "|100|1";
                var packet3 = "0|M|" + PetId + "|" + userId + "|L|" + enemy.Ship.HP + "|" + enemy.Ship.shield + "|" + power + "|100|1";
                if (enemy.Ship.ISH)
                {
                    realDamage = 0;
                    power = 0;
                }
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
                foreach (var pair in Program.Maps[Program.Users[UserId].MapId].Users)
                {
                    pair.Value.Send(packet);
                    if (enemy.Ship.ISH)
                    {
                        realDamage = 0;
                        if (pair.Value.Ship.selectedUserId == userId || userId == pair.Value.UserId)
                        {
                            pair.Value.Send(packet3);
                        }
                    }
                    else
                    {
                        if (pair.Value.Ship.selectedUserId == userId || userId == pair.Value.UserId)
                        {
                            pair.Value.Send(packet2);
                        }
                    }
                }
                if (enemy.Ship.HP - realDamage < 1 || enemy.Ship.HP > enemy.Ship.maxHP)
                {
                    enemy.Ship.HP = 0;
                    Program.Users[UserId].Ship.isAttacking = false;
                    if (enemy.Ship.destroyed == false)
                    {
                        enemy.Ship.destroyed = true;
                        foreach (var pair in Program.Maps[Program.Users[UserId].MapId].Users)
                        {
                            pair.Value.Send("0|K|" + Program.Users[UserId].Ship.selectedUserId);
                        }
                        if (enemy.Ship.destroyed)
                        {
                            Program.Users[UserId].Send("0|A|STD|You ripped " + enemy.Ship.Name + ".");
                            foreach (var pair in Program.Maps[Program.Users[UserId].MapId].Users)
                            {
                                pair.Value.Send("0|K|" + Program.Users[UserId].Ship.selectedUserId);
                            }
                            if (Program.Users[UserId].Ship.factionId == enemy.Ship.factionId)
                            {
                                if (Program.Users[UserId].Ship.exp > 0)
                                {
                                    Program.Users[UserId].Ship.exp -= 1;
                                    Program.Users[UserId].Send("0|LM|ST|EP|-1|" + Program.Users[UserId].Ship.exp);
                                }
                                else
                                {
                                    Program.Users[UserId].Ship.exp = 0;
                                    Program.Users[UserId].Send("0|LM|ST|EP|-1|" + Program.Users[UserId].Ship.exp);
                                }
                            }
                            else
                            {
                                Program.Users[UserId].Ship.exp += 1;
                                Program.Users[UserId].Send("0|LM|ST|EP|1|" + Program.Users[UserId].Ship.exp);
                            }
                            if (Program.Users[UserId].Ip == enemy.Ip)
                            {
                                Program.Users[UserId].Ship.exp -= 1;
                                Program.Users[UserId].Send("0|LM|ST|EP|-1|" + Program.Users[UserId].Ship.exp);
                                Logger.Log(UserId.ToString(CultureInfo.InvariantCulture), "Received", "-1 exp");
                            }
                        }
                        Program.Maps[Program.Users[UserId].MapId].RemoveUser(Program.Users[UserId].Ship.selectedUserId);
                        enemy.discconectUser(1);
                        Program.Users[UserId].Ship.selectedUserId = 0;
                        using (var dbClient = Program.Manager.getQueryreactor())
                        {
                            dbClient.query("UPDATE server_1_players SET exp = '" + Program.Users[UserId].Ship.exp + "' WHERE playerID = '" + UserId + "'");
                        }
                    }
                    Program.Users[UserId].Ship.isAttacking = false;
                }
            }
            catch (Exception e)
            {
                // Out.WriteLine(e.StackTrace);
            }
        }
        #endregion
        #region Movement

        private async void Movement()
        {
            try
            {
                while (true)
                {
                    if (PetActive && !Kami)
                    {
                        Move();
                        if (!Program.Maps[Program.Users[UserId].MapId].Users.ContainsKey(UserId))
                        {
                            DeActivate();
                            return;
                        }
                        await Task.Delay(365);
                        continue;
                    }
                    break;
                }
            }
            catch (Exception e)
            {
                //     Out.WriteLine(e.StackTrace);
            }
        }

        private void Move()
        {
            try
            {
                var s = Program.Users[UserId];
                var distance = Math.Sqrt(Math.Pow(Program.Users[UserId].Ship.x - X, 2) + Math.Pow(Program.Users[UserId].Ship.y - Y, 2));
                if (distance < 190 && !Kami)
                {
                }
                else
                {
                    var petx = Program.Users[UserId].Ship.GetPosX() + Program.Random.Next(-100, 100);
                    var pety = Program.Users[UserId].Ship.GetPosY() + Program.Random.Next(-100, 100);
                    if (Kami)
                    {
                        var enemy =
                            Program.Maps[Program.Users[UserId].MapId].Aliens[Program.Users[UserId].Ship.selectedUserId];
                        petx = enemy.X;
                        pety = enemy.Y;
                    }
                    distance = Math.Sqrt(Math.Pow(petx - X, 2) + Math.Pow(pety - Y, 2));
                    var time = Convert.ToInt32((distance / Speed) * 1000);
                    var pet = "0|1|" + (s.UserId + 1000) + "|" + petx + "|" + pety + "|" + time;
                    // Console.WriteLine(pet+" & "+Speed+ " " + distance +" "+petx + " " +x);
                    Send(pet);
                    UpdateXy(petx, pety);
                }
            }
            catch { }
        }
        private void Send(string packet)
        {
            foreach (var user in Program.Maps[Program.Users[UserId].MapId].Users)
            {
                user.Value.Send(packet);
            }
        }
        private void UpdateXy(int a, int b)
        {
            var newPosX = a;
            var newPosY = b;
            var oldx = X;
            var oldy = Y;
            var distance = Math.Sqrt(Math.Pow(oldx - newPosX, 2.0D) + Math.Pow(oldy - newPosY, 2.0D));
            var speed = Speed * 0.37D;
            var dir = Math.Atan2(newPosY - oldy, newPosX - oldx) * 180.0D / (Math.PI);
            if (distance >= speed)
            {
                oldx += Convert.ToInt32((speed * Math.Cos((Math.PI * dir) / 180)));
                oldy += Convert.ToInt32((speed * Math.Sin((Math.PI * dir) / 180)));
                X = oldx;
                Y = oldy;
            }
            else
            {
                X = newPosX;
                Y = newPosY;
            }
        }

        #endregion
        public string PetPacket()
        {
            return "f|C|" + PetId + "|15|1|" + Program.Users[UserId].Ship.tagName + "|" + Program.Users[UserId].Ship.Name + "'s PET|" + X + "|" + Y + "|" + Program.Users[UserId].Ship.factionId + "|-1|0|0|1|0";
        }
        public string PetOwnerPacket()
        {
            return "0|PET|L|1|" + PetId + "|15|" + Program.Users[UserId].Ship.Name + "'s PET|" + Program.Users[UserId].Ship.factionId + "|1|1|" + Program.Users[UserId].Ship.tagName + "|0|" + X + "|" + Y + "|1|1";
        }
    }
}