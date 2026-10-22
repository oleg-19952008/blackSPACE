using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Do.serverGame.AlienClasses
{
    class AlienMovement
    {
        public int MovementSpeed { get; set; }
        private Alien Alien { get; set; }
        public int X, Y;
        public int RadX, RadY;
        public int UserX;
        public int UserY;
        public int OldX;
        public int OldY;
        public bool Moving = false;
        public int Tick = 0;
        private int _ai;
        private static Random _rand;
// ReSharper disable InconsistentNaming
        private const int MOVE_RANDOM = 1;
        private const int MOVE_TO_PLAYER = 2;
        private const int MOVE_TO_LOCKED = 3;
        private const int MOVE_TO_CORNER = 4;
        private const int MOVE_AROUND_MAMA = 5;
        // ReSharper restore InconsistentNaming
        public List<uint> PlayersInRange = new List<uint>();
        public AlienMovement(Alien a)
        {
            Alien = a;
            _rand=new Random();
        }
        #region Start Movement
        public async void Start()
        {
            try
            {
                while (true)
                {
                    if (!Alien.Destroyed)
                    {
                        if (Alien.Hp < Program.Npcs[Alien.TypeId].Hp/5)
                        {
                            MoveToCorner();
                        }
                        else if (Alien.SelectedUserId == 0)
                        {
                            if (Alien.Lock == 0)
                            {
                                MoveRandom();
                            }
                            else
                            {
                                MoveToLocked();
                            }
                        }
                        else
                        { 
                            MoveToPlayer();
                        }
                    }
                    await Task.Delay(500);
                }
            }
            catch (Exception) { }
            //catch (Exception e)
            //{ Console.WriteLine("" + e); }
            //await Task.Delay(500);
            //Start();
        }

        public async void StartProt()
        {
            try
            {
                while (true)
                {
                    if (!Program.Maps[Alien.MapId].Aliens.ContainsKey(Alien.Id))
                        return;

                    MoveAroundMama();
                    await Task.Delay(500);
                }
            }
                catch (Exception)
                {
                }

        }

        #endregion        
        #region AIStages
       private Random _randy = new Random();
        private void MoveAroundMama()
        {
            if (_ai != MOVE_AROUND_MAMA)
            {
                _ai = MOVE_AROUND_MAMA;
                Tick = 0;
            }
            var oldx = Alien.X;
            var oldy = Alien.Y;
            if (Alien.X==UserX && Alien.Y==UserY || Tick==0)
            {
                OldX = Program.Maps[Alien.MapId].Aliens[Alien.OwnerId].X;
                OldY = Program.Maps[Alien.MapId].Aliens[Alien.OwnerId].Y;
                UserX = OldX + _rand.Next(-500, 500);
                UserY = OldY + _randy.Next(-500, 500);
                Tick = 0;
            }

            var distancia = Math.Sqrt(Math.Pow(oldx - UserX, 2) + Math.Pow(oldy - UserY, 2));
            var speed2 = MovementSpeed * 0.5;
            var unghi = Math.Atan2(UserY - oldy, UserX - oldx);
            if (Tick == 0)
            {
                SendMovement();
                Tick++;
            }
            if (distancia >= speed2)
            {
                Alien.X += (int)(speed2 * Math.Cos( unghi));
                Alien.Y += (int)(speed2 * Math.Sin(unghi));
            }
            else
            {
                Alien.X = UserX;
                Alien.Y = UserY;

            }
        }
        private void MoveToPlayer()
          {
            if (Alien.SelectedUserId == 0 || !Program.Maps[Alien.MapId].Users.ContainsKey(Alien.SelectedUserId) )
            {
                Alien.SelectedUserId = 0;
                return;
            }
            if (OldX != Program.Users[Alien.SelectedUserId].Ship.GetPosX() || OldY != Program.Users[Alien.SelectedUserId].Ship.GetPosY())
            {
                Tick = 0;
                OldX = Program.Users[Alien.SelectedUserId].Ship.GetPosX();
                OldY = Program.Users[Alien.SelectedUserId].Ship.GetPosY();
                UserX = OldX + _rand.Next(-150, 150);
                UserY = OldY + _rand.Next(-150, 150);
            }
              if(_ai!=MOVE_TO_PLAYER)
              {
                  _ai = MOVE_TO_PLAYER;
                  Tick = 0;
              }
            var oldx = Alien.X;
            var oldy = Alien.Y;
            if (Distance.Get(UserX, oldx, UserY, oldy) < 175)
                return;
            
            var distancia = Math.Sqrt(Math.Pow(oldx - UserX, 2) + Math.Pow(oldy - UserY, 2));
            var speed2 = MovementSpeed * 0.5;
            var unghi = Math.Atan2(UserY - oldy, UserX - oldx);
            if (Tick == 0)
            {
                SendMovement();
                Tick++;
            }
            if (distancia >= speed2)
            {
                Alien.X += (int)(speed2 * Math.Cos(unghi));
                Alien.Y += (int)(speed2 * Math.Sin(unghi));
            }
            else
            {
                Alien.X = UserX;
                Alien.Y = UserY;
            }

          }
          private void MoveToLocked()
          {
              if (Alien.Lock == 0 || !Program.Maps[Alien.MapId].Users.ContainsKey(Alien.Lock))
              {
                  Alien.Lock = 0;
                  return;
              }
              if (OldX != Program.Users[Alien.Lock].Ship.GetPosX() || OldY != Program.Users[Alien.Lock].Ship.GetPosY())
                  Tick = 0;
              if(_ai!=MOVE_TO_LOCKED)
              {
                  _ai = MOVE_TO_LOCKED;
                  Tick = 0;
              }
              OldX = Program.Users[Alien.Lock].Ship.GetPosX();
              OldY = Program.Users[Alien.Lock].Ship.GetPosY();
              UserX = OldX + _rand.Next(-150, 150);
              UserY = OldY + _rand.Next(-150, 150);
              var oldx = Alien.X;
              var oldy = Alien.Y;
              if (Distance.Get(UserX, oldx, UserY, oldy) < 175)
                  return;

              var distancia = Math.Sqrt(Math.Pow(oldx - UserX, 2) + Math.Pow(oldy - UserY, 2));
              var speed2 = MovementSpeed * 0.5;
              var unghi = Math.Atan2(UserY - oldy, UserX - oldx);
              if (Tick == 0)
              {
                  SendMovement();
                  Tick++;
              }
              if (distancia >= speed2)
              {
                  Alien.X += (int)(speed2 * Math.Cos( unghi));
                  Alien.Y += (int)(speed2 * Math.Sin(unghi));
              }
              else
              {
                  Alien.X = UserX;
                  Alien.Y = UserY;
              }

          }
          private void MoveRandom()
          {
              var oldx = Alien.X;
              var oldy = Alien.Y;

              if (oldx == X && oldy == Y)
                  NewRandomCoord();
              if (_ai != MOVE_RANDOM)
              {
                  _ai = MOVE_RANDOM;
                  Tick = 0;
              }
              var distancia = Math.Sqrt(Math.Pow(oldx - X, 2) + Math.Pow(oldy - Y, 2));
              var speed2 = MovementSpeed * 0.5;
              var unghi = Math.Atan2(Y - oldy, X - oldx) ;
              if (Tick == 0)
              {
                  SendMovement();
                  Tick++;
              }
              if (distancia >= speed2)
              {
                  Alien.X += (int)(speed2 * Math.Cos(unghi));
                  Alien.Y += (int)(speed2 * Math.Sin(unghi));
              }
              else
              {
                  Alien.X = X;
                  Alien.Y = Y;
                  NewRandomCoord();
              }
          }
          private void MoveToCorner()
          {
              var oldx = Alien.X;
              var oldy = Alien.Y;
              if(_ai!=MOVE_TO_CORNER)
              {
                  Tick = 0;
                  _ai = MOVE_TO_CORNER;
              }
                  
              //if (oldx == 0 && oldy == 0)
              //{
              //    if (Program.Users.ContainsKey(Alien.SelectedUserId) && Distance.Get(oldx, Program.Users[Alien.SelectedUserId].Ship.x, oldy,
              //            Program.Users[Alien.SelectedUserId].Ship.y) <= 2000) return;
              //    Alien.Shield = Program.Npcs[Alien.TypeId].Shield;
              //    //Alien.Hp = Program.Npcs[Alien.TypeId].Hp;
              //    RadX = 1;
              //    RadY = 1;
              //    return;
              //}
              RadX = 0;
              RadY = 0;
              var distancia = Math.Sqrt(Math.Pow(oldx - RadX, 2) + Math.Pow(oldy - RadY, 2));
              var speed2 = MovementSpeed * 0.5;
              var unghi = Math.Atan2(RadY - oldy, RadX - oldx);
              if (Tick == 0)
              {
                  SendMovement();
                   Tick++;
              }
              if (distancia >= speed2)
              {
                  Alien.X += (int)(speed2 * Math.Cos( unghi));
                  Alien.Y += (int)(speed2 * Math.Sin( unghi));
              }
              else
              {
                  Alien.X = RadX;
                  Alien.Y = RadY;
                  RadX = 1;
                  RadY = 1;

                  NewRandomCoord();
              }
          }
        #endregion
        #region Randomizing
          public void NewRandomCoord()
          {
              _rand = new Random();
              X = _rand.Next(1000, 19000);
              Y = _rand.Next(1000, 11100);
              Moving = true;
              Tick = 0;

          }
          #endregion
        #region SendShit
                public void SendMovement(uint id=0)
                {
                    //if (Alien.TypeId == 80 )
                    //{
                    //    return;
                    //}
                    if(id==0)
                    {
                if (Alien.TypeId == 100)
                {
                    var distancia = Math.Sqrt(Math.Pow(Alien.X - UserX, 2.0D) + Math.Pow(Alien.Y - UserY, 2.0D));
                    var speed = Math.Round(distancia / MovementSpeed * 1000);
                    var packet = "0|1|" + Alien.Id + "|" + UserX + "|" + UserY + "|" + speed;
                    foreach (var user in PlayersInRange)
                    {
                        try
                        {
                            Program.Users[user].Send(packet);
                        }
                        catch (Exception)
                        {
                        }
                    }


                    return;
                }
                    if (Alien.TypeId == 81)
                        {
                            var distancia = Math.Sqrt(Math.Pow(Alien.X - UserX, 2.0D) + Math.Pow(Alien.Y - UserY, 2.0D));
                            var speed = Math.Round(distancia / MovementSpeed * 1000);
                            var packet = "0|1|" + Alien.Id + "|" + UserX + "|" + UserY + "|" + speed;
                            foreach (var user in PlayersInRange)
                            {
                                try
                                {
                                    Program.Users[user].Send(packet);
                                }
                                catch (Exception)
                                {
                                }
                            }

                            return;
                        }
                if (Alien.TypeId == 117)
                {
                    var distancia = Math.Sqrt(Math.Pow(Alien.X - UserX, 2.0D) + Math.Pow(Alien.Y - UserY, 2.0D));
                    var speed = Math.Round(distancia / MovementSpeed * 1000);
                    var packet = "0|1|" + Alien.Id + "|" + UserX + "|" + UserY + "|" + speed;
                    foreach (var user in PlayersInRange)
                    {
                        try
                        {
                            Program.Users[user].Send(packet);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    return;
                }
                if (_ai == MOVE_TO_CORNER)
                        {
                            var distancia = Math.Sqrt(Math.Pow(Alien.X - RadX, 2.0D) + Math.Pow(Alien.Y - RadY, 2.0D));
                            var speed = Math.Round(distancia / MovementSpeed * 1000);
                            var packet = "0|1|" + Alien.Id + "|" + RadX + "|" + RadY + "|" + speed;
                            foreach (var user in PlayersInRange)
                            {
                                try
                                {
                                    Program.Users[user].Send(packet);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                        else if (Alien.SelectedUserId == 0)
                        {
                            if (_ai == MOVE_TO_LOCKED)
                            {
                                var distancia =
                                    Math.Sqrt(Math.Pow(Alien.X - UserX, 2.0D) + Math.Pow(Alien.Y - UserY, 2.0D));
                                var speed = Math.Round(distancia/MovementSpeed*1000);
                                var packet = "0|1|" + Alien.Id + "|" + UserX + "|" + UserY + "|" + speed;
                                foreach (var user in PlayersInRange)
                                {
                                    try
                                    {
                                        Program.Users[user].Send(packet);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                            else
                            {
                                var distancia = Math.Sqrt(Math.Pow(Alien.X - X, 2.0D) + Math.Pow(Alien.Y - Y, 2.0D));
                                var speed = Math.Round(distancia/MovementSpeed*1000);
                                var packet = "0|1|" + Alien.Id + "|" + X + "|" + Y + "|" + speed;
                                foreach (var user in PlayersInRange)
                                {
                                    try
                                    {
                                        Program.Users[user].Send(packet);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }
                        else
                        {
                            var distancia = Math.Sqrt(Math.Pow(Alien.X - UserX, 2.0D) + Math.Pow(Alien.Y - UserY, 2.0D));
                            var speed = Math.Round(distancia / MovementSpeed * 1000);
                            var packet = "0|1|" + Alien.Id + "|" + UserX + "|" + UserY + "|" + speed;

                            foreach (var user in PlayersInRange)
                            {
                                Program.Users[user].Send(packet);
                            }
                        }
                      
                    }
                    else 
                    {
                        try
                        {
                            if (Alien.TypeId == 81)
                            {
                                var distancia = Math.Sqrt(Math.Pow(Alien.X - UserX, 2.0D) + Math.Pow(Alien.Y - UserY, 2.0D));
                                var speed = Math.Round(distancia / MovementSpeed * 1000);
                                var packet = "0|1|" + Alien.Id + "|" + UserX + "|" + UserY + "|" + speed;
                                Program.Users[id].Send(packet);

                            }
                            if (_ai == MOVE_TO_CORNER)
                            {
                                var distancia = Math.Sqrt(Math.Pow(Alien.X - RadX, 2.0D) + Math.Pow(Alien.Y - RadY, 2.0D));
                                var speed = Math.Round(distancia / MovementSpeed * 1000);
                                var packet = "0|1|" + Alien.Id + "|" + RadX + "|" + RadY + "|" + speed;
                                Program.Users[id].Send(packet);
                            }

                            else if (Alien.SelectedUserId == 0)
                            {
                                if (_ai == MOVE_TO_LOCKED)
                                {
                                    var distancia =
                                        Math.Sqrt(Math.Pow(Alien.X - UserX, 2.0D) + Math.Pow(Alien.Y - UserY, 2.0D));
                                    var speed = Math.Round(distancia/MovementSpeed*1000);
                                    var packet = "0|1|" + Alien.Id + "|" + UserX + "|" + UserY + "|" + speed;
                                    Program.Users[id].Send(packet);
                                }
                                else
                                {
                                    var distancia = Math.Sqrt(Math.Pow(Alien.X - X, 2.0D) + Math.Pow(Alien.Y - Y, 2.0D));
                                    var speed = Math.Round(distancia/MovementSpeed*1000);
                                    var packet = "0|1|" + Alien.Id + "|" + X + "|" + Y + "|" + speed;
                                    Program.Users[id].Send(packet);
                                }
                            }
                            else
                            {
                                var distancia = Math.Sqrt(Math.Pow(Alien.X - UserX, 2.0D) + Math.Pow(Alien.Y - UserY, 2.0D));
                                var speed = Math.Round(distancia / MovementSpeed * 1000);
                                var packet = "0|1|" + Alien.Id + "|" + UserX + "|" + UserY + "|" + speed;
                                Program.Users[id].Send(packet);
                            }
                        } catch(Exception)
                        {}
                    }
                }

        #endregion
    }
}
