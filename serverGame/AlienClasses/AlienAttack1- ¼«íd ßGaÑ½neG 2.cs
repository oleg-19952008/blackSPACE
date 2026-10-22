using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Do.game;

namespace Do.serverGame.AlienClasses
{
    class AlienAttack
    {
        private Alien Alien { get; set; }
        public uint Damage { get; set; }
        public bool IsAttacking = false;
        private const int LaserId = 1;
        private bool _protegitsReleased;
        private bool _takingOff;
        public List<uint> Childs = new List<uint>();

        public AlienAttack(Alien a)
        {
            Alien = a;
        }
   
        public async void Start()
        {
            while (true)
            {
                

	   await Task.Delay(1000);
       if (!Program.Maps[Alien.MapId].Aliens.ContainsKey(Alien.Id))
           return;
		var selected= Alien.SelectedUserId;
	       if (selected == 0 || Alien.Destroyed)
	       {
	       }
	       else
	       {
	           if (!Program.Maps[Alien.MapId].Users.ContainsKey(selected))
	           {
	               Alien.SelectedUserId = 0;
	           }
	           else
	           {
	               try
	               {
	                   var enemy = Program.Maps[Alien.MapId].Users[selected];
	                   double eX = Alien.X,
	                       eY = Alien.Y,
	                       uX = enemy.Ship.x,
	                       uY = enemy.Ship.y;
	                   var distancia = Convert.ToInt32(Distance.Get(eX, uX, eY, uY));
                       var dist = 550;
                       if (Alien.TypeId == 81) dist = 900;
	                   if (distancia > dist)
	                   {
	                   }
	                   else if (enemy.Ship.InSecureArea)
	                   {
	                       Alien.RelCount = 0;
	                       Alien.SelectedUserId = 0;
	                       Alien.Move.NewRandomCoord();
	                   }
	                   else
	                   {
	                       enemy.Ship.regen = 0;
	                       var power = Convert.ToInt32(Damage);
                           var powerRandom = Program.Random.Next(-power/100, power/100);
                           power += powerRandom;
	                       if (enemy.Ship.Spectrum)
	                       {
	                           power = power/6;
	                       }
	                       else if (enemy.Ship.ShieldLeech &&
	                                enemy.Ship.shield > 0)
	                       {
	                           power = (power*3)/2;
	                       }
	                       var realDamage = power;
	                       if (enemy.Ship.ISH)
	                       {
	                           realDamage = 0;
	                           power = 0;
	                       }
	                       enemy.TakeDamage(realDamage);

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
	                           realDamage = power - Convert.ToInt32(power*(enemy.Ship.shieldAbsorb/100m));
	                       }

	                       realDamage = Math.Abs(realDamage);


	                       if (enemy.Ship.HP - realDamage < 1 || enemy.Ship.HP > enemy.Ship.maxHP)
	                       {
	                           enemy.Ship.HP = 0;
	                           IsAttacking = false;
	                           if (enemy.Ship.destroyed == false)
	                           {
	                               enemy.Ship.destroyed = true;

	                               foreach (var pair in enemy.PlayersInRange)
	                               {
	                                   Program.Users[pair].Send("0|K|" + selected);
	                               }
	                               if (enemy.Ship.destroyed)
	                               {
	                                   foreach (var pair in enemy.PlayersInRange)
	                                   {
                                           Program.Users[pair].Send("0|K|" + selected);
	                                   }
	                                   enemy.UpdateUser(3,1);
	                                   enemy.discconectUser(1);
	                                   Program.Maps[Alien.MapId].RemoveUser(selected);

	                               }
	                           }
	                           Alien.SelectedUserId = 0;
	                           IsAttacking = false;
	                       }
	                       else
	                       {

	                           enemy.Ship.HP -= Convert.ToUInt32(realDamage);
	                           var packet = "0|a|" + Alien.Id + "|" + selected + "|" + LaserId + "|1|1";
	                               var packet3 = "0|M|" + Alien.Id + "|" + selected + "|L|" + enemy.Ship.HP + "|" +
	                                         enemy.Ship.shield + "|" + power + "|100|1";
	                           if (enemy.Ship.ISH)
	                           {
	                               foreach (var pair in enemy.PlayersInRange)
	                               {
                                       Program.Users[pair].Send(packet);
	                               }
	                               enemy.Send(packet3);
	                           }
	                           else
	                           {
	                               foreach (var pair in enemy.PlayersInRange)
	                               {
                                       Program.Users[pair].Send(packet);
	                               }
	                           }
	                       }
	                   }


	               }
	               catch (Exception e)
	               {
	               }
	           }
	       }
	   }

        }

        public async void StartCubi()
        {


            try
            {
                while (true)
                {
                    if (Alien.SelectedUserId != 0)
                    {
                        if (_protegitsReleased == false)
                        {
                            _protegitsReleased = true;
                            ReleaseProts();
                        }
                    }
                    else
                    {
                        if (!_takingOff)
                        {
                            TakeOffProts();
                        }
                    }

                    await Task.Delay(1000);
                }
            }
            catch (Exception)
            {
            }

        }

        private async void ReleaseProts()
        {
          
            await Task.Delay(2000);
            if (Alien.TypeId == 116)
            {
                var created_ = 0;
                while (created_ < 12)
                {
                    try
                    {
                        ++created_;
                        var id = Convert.ToUInt32(Program.Maps[Alien.MapId].Aliens.Count + 1);
                        Childs.Add(id);
                        Program.Maps[Alien.MapId].Aliens.Add(id, new Alien(id, 117, Alien.MapId, Alien.Id));
                    }
                    catch (Exception) { }
                }
            }
            else
            {
                var created = 0;
                while (created < 24)
                {
                    try
                    {
                        ++created;
                        var id = Convert.ToUInt32(Program.Maps[Alien.MapId].Aliens.Count + 1);
                        Childs.Add(id);
                        Program.Maps[Alien.MapId].Aliens.Add(id, new Alien(id, 103, Alien.MapId, Alien.Id));
                    }
                    catch (Exception) { }
                }

                //  var created = 0;
                while (created < 34)
                {
                    try
                    {
                        ++created;
                        var id = Convert.ToUInt32(Program.Maps[Alien.MapId].Aliens.Count + 1);
                        Childs.Add(id);
                        Program.Maps[Alien.MapId].Aliens.Add(id, new Alien(id, 81, Alien.MapId, Alien.Id));
                    }
                    catch (Exception) { }
                }
            }

        }
        private async void Repair()
        {
            while(Alien.SelectedUserId == 0)
            {
                if (Alien.Hp == Alien.MaxHp)
                {
                    Alien.Shield = Alien.MaxShield;
                    break;
                }
                var rep = Alien.MaxHp / 20;
                if (Alien.Shield < Alien.MaxShield)
                {
                    var shd = Alien.MaxShield / 10;
                    if (Alien.Shield + shd > Alien.MaxShield)
                        Alien.Shield = Alien.MaxShield;
                    else
                        Alien.Shield += shd;
                }
                if (Alien.Hp + rep > Alien.MaxHp)
                    Alien.Hp = Alien.MaxHp;
                else
                Alien.Hp += rep;
                    
                await Task.Delay(1000);
            }

        }
        private async void TakeOffProts()
        {

            _takingOff = true;
            var tick = 0;
            while (tick < 20)
            {
                if (Alien.SelectedUserId != 0)
                {
                    _takingOff = false;
                    break;
                }
                tick++;
                await Task.Delay(1000);
            }
            if (_takingOff == false)
            {
                return;
            }
            foreach (var kid in Childs)
            {
                Program.Maps[Alien.MapId].RemoveDaughters(kid);
            }
            Childs = new List<uint>();
            _takingOff = false;
            _protegitsReleased = false;
           var a = new Task(Repair);
           a.Start();
        }
    }
}
