using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Do.game;
using System.Threading;

namespace Do.serverGame
{
    class Map
    {
        public ushort _id { get; set; }
        public ushort Level { get; set; }
        public short _factionId { get; set; }
        public string _name { get; set; }
        public bool IsPvp { get; set; }
        public Dictionary<uint, User> Users;
        public List<Portal> Portals;
        public Dictionary<uint, Alien> Aliens;
        public Dictionary<uint, string[]> CargoBox;
        public Dictionary<int, string[]> Mines;
        public Dictionary<string, string[]> Fireworks;
        public Dictionary<uint, Box> BonusBox;
        public bool IsStarterMap = false;
        public int CubeCount = 0;
        public class Portal
        {
            public ushort Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int NewX { get; set; }
            public int NewY { get; set; }
            public ushort Map { get; set; }
        }

        public class Npcs
        {

            public ushort NpcId { get; set; }
            public int Count { get; set; }
        }

        public Map(UInt16 id, string name, string portals, string npcs, bool starterMap, short factionId, ushort leve)
        {

            _id = id;
            _name = name;
            _factionId = factionId;
            //Console.WriteLine(factionId);
            IsStarterMap = starterMap;
            Level = leve;
            Users = new Dictionary<uint, User>();
            var serializer = new JavaScriptSerializer();
            Portals = serializer.Deserialize<List<Portal>>(portals);
            CreateNpcs(npcs);
            CargoBox = new Dictionary<uint, string[]>();
            Mines = new Dictionary<int, string[]>();
            BonusBox = new Dictionary<uint, Box>();
            Fireworks = new Dictionary<string, string[]>();
            CreateBonusBox(65);
            if (id == 13 || id == 14 || id == 15 || id == 16)
                IsPvp = true;
            else
                IsPvp = false;
        }
        public async static void add_user(uint user)
        {
            if (!Map.Users2.ContainsKey(user))
            {
              Users2.Add(user, Program.Users[user]);
            }
            Map.Users2[user].SendUserShit();
            Map.Users2[user].sendStations();
            Map.Users2[user].Ship.lunchrsb = false;
  SendPortals2(user);
            //Map.Users2[user].SendMines(user);
            //Map.Users2[user].SendCargos(user);
            if (Map.Users2[user].Ship.teleported)
            {
                try
                {
                    await Task.Delay(100);

                    Map.Users2[user].Ship.teleport1 = false;
                    Map.Users2[user].Ship.buclemover = false;
                    Map.Users2[user].Ship.teleported = false;
                    Map.Users2[user].Ship.teleporting = 0;
             //   Map.Users2[user].    Move(user);
                }
                catch (Exception) { }
            }
            if (Map.Users2[user].Ship.teleported2)
            {
                try
                {
                    Map.Users2[user].Ship.buclemover = false;
                    await Task.Delay(371);
                    Map.Users2[user].Ship.teleport2 = false;

                    Map.Users2[user].Ship.teleported2 = false;
                    Map.Users2[user].Ship.teleporting2 = 0;
               //     Move(user);
                }
                catch (Exception) { }
            }
        }
        public void AddUser(uint userId)
        {
         
            if (!Users.ContainsKey(userId))
            {
                Users.Add(userId, Program.Users[userId]);
            }
            Users[userId].SendUserShit();
            Users[userId].sendStations();
            Users[userId].Ship.lunchrsb = false;
            SendPortals(userId);
            SendMines(userId);
            SendCargos(userId);
            if (Users[userId].Ship.teleported)
            {
                Ssss(userId);
            }
            if (Users[userId].Ship.teleported2)
            {
                Sss(userId);
            }
        }

        private async void Ssss(uint userId)
        {
            try
            {
                await Task.Delay(100);

                Users[userId].Ship.teleport1 = false;
                Users[userId].Ship.buclemover = false;
                Users[userId].Ship.teleported = false;
                Users[userId].Ship.teleporting = 0;
                Move(userId);
            }
            catch (Exception) { }
        }
        private async void Sss(uint userId)
        {
            try
            {
                Users[userId].Ship.buclemover = false;
                await Task.Delay(371);
                Users[userId].Ship.teleport2 = false;

                Users[userId].Ship.teleported2 = false;
                Users[userId].Ship.teleporting2 = 0;
                Move(userId);
            }
            catch (Exception) { }
        }

        public void RemoveUser(uint userId)
        {
            try
            {
                Users.Remove(userId);
                Parallel.ForEach(Users, pair => pair.Value.Send("f|R|" + userId));
            }
            catch (Exception) { }
        }
        public void RemoveDaughters(uint kidId)
        {
            try
            {
                var players = Aliens[kidId].Move.PlayersInRange;
                Aliens[kidId].Destroyed = true;
                Aliens.Remove(kidId);
                foreach (var pair in players)
                {

                    Users[pair].Send("f|R|" + kidId);

                }
                //GC.Collect();
            }
            catch (Exception) { }
        }
        private void CreateNpcs(string npcs)
        {
            try
            {
                if (string.IsNullOrEmpty(npcs)) return;
                var serializer = new JavaScriptSerializer();
                var npclist = serializer.Deserialize<List<Npcs>>(npcs);
                Aliens = new Dictionary<uint, Alien>();

                uint created = 0;

                foreach (var npc in npclist)
                {
                    var creating = 0;
                    while (creating < npc.Count)
                    {
                        creating++;
                        created++;

                        Aliens.Add(created, new Alien(created, npc.NpcId, _id, 0, CubeCount));
                        if (npc.NpcId == 80)
                            CubeCount++;
                    }
                }
                Out.WriteLine(Aliens.Count + " npcs added to " + _name + "!");
            }
            catch (Exception)
            {
                Out.WriteLine(" npcs failed adding to " + _name + "!");
            }
        }

        public async void AddMine(int id, int x, int y, int type)
        {
            await Task.Delay(2000);
            // ReSharper disable SpecifyACultureInStringConversionExplicitly
            Mines.Add(id, (new[] { x.ToString(), y.ToString(), type.ToString() }));
            // ReSharper restore SpecifyACultureInStringConversionExplicitly
        }
        #region Fireworks
        public void AddFireworkBox(string hash, int x, int y, int userid)
        {

            // ReSharper disable SpecifyACultureInStringConversionExplicitly
            Fireworks.Add(hash, (new[] { x.ToString(), y.ToString(), userid.ToString() }));
            // ReSharper restore SpecifyACultureInStringConversionExplicitly
        }
        public void RemoveFireworkBox(string userId)
        {

            foreach (var firework in Fireworks)
            {
                if (firework.Value[2] != userId) continue;
                var packet = "0|n|FWI|" + firework.Key;
                foreach (var user in Program.Maps[_id].Users)
                {
                    user.Value.Send(packet);
                }
                Fireworks.Remove(firework.Key);
            }
        }
        #endregion
        private void CreateBonusBox(int count)
        {
            uint created = 0;

            while (created < count)
            {
                ++created;
                BonusBox.Add(created, new Box(created, _id));
            }
        }

        private void SendMines(uint userId)
        {
            Parallel.ForEach(Mines, pair => Users[userId].Send("0|L|" + pair.Key + "|" + pair.Value[2] + "|" + pair.Value[0] + "|" + pair.Value[1]));

        }

        private void SendCargos(uint userId)
        {
            Parallel.ForEach(CargoBox, pair => Users[userId].Send("0|c|sb" + pair.Key + "|1|" + pair.Value[0] + "|" + pair.Value[1]));
        }
      
        private void _MoveShip(uint userId)
        {
           
            try
            {
                var teleporting = Users[userId].Ship.teleporting;
                var teleporting2 = Users[userId].Ship.teleporting2;
                var logout3 = Users[userId].Ship.logout3;
        
                var packetSend = "";
                if (Users[userId].Ship.x == Users[userId].Ship.destinationx && Users[userId].Ship.y == Users[userId].Ship.destinationy || teleporting == 1 || teleporting2 == 1 || logout3 == 1)
                {
                    Users[userId].Ship.isMoving = false;
                    Users[userId].Ship.tickCount = 0;
                }
                else if (Users[userId].DestroyedClass || !Users.ContainsKey(userId))
                {
                    return;
                }
                else
                {
                    Users[userId].Ship.isMoving = true;
                    var newPosX = Users[userId].Ship.destinationx;
                    var newPosY = Users[userId].Ship.destinationy;
                    var oldx = Users[userId].Ship.x;
                    var oldy = Users[userId].Ship.y;
                    var distancia = Math.Sqrt(((oldx - newPosX) * (oldx - newPosX)) + ((oldy - newPosY) * (oldy - newPosY)));
                    var speed2 = Math.Round(Convert.ToDouble(Users[userId].Ship.speed * 0.36));
                    var moveBitch = Math.Atan2(newPosY - oldy, newPosX - oldx);
                    var speed = Math.Round(distancia / Users[userId].Ship.speed * 1000);
                    packetSend = "0|1|" + userId + "|" + newPosX + "|" + newPosY + "|" + speed;

                    if (distancia >= Users[userId].Ship.speed)
                    {
                        Users[userId].Ship.x += (int)Math.Round(speed2 * Math.Cos(moveBitch));
                        Users[userId].Ship.y += (int)Math.Round(speed2 * Math.Sin(moveBitch));
                        //this.moviendo = true;
                    }
                    else
                    {
                        Users[userId].Ship.x = newPosX;
                        Users[userId].Ship.y = newPosY;
                        Users[userId].Ship.isMoving = false;
                    }
                }

                int nX = Users[userId].Ship.x, nY = Users[userId].Ship.y;

                try
                {
                    foreach (var pair in Aliens)
                    {
                        var distance = Convert.ToInt32(Distance.Get(pair.Value.X, nX, pair.Value.Y, nY));
                        if (distance < 2000)
                        {
                            if (pair.Value.Move.PlayersInRange.Contains(userId) || pair.Value.Destroyed) continue;
                            pair.Value.Move.PlayersInRange.Add(userId);
                            Users[userId].Send("f|C|" + pair.Value.Id + "|" + Program.Npcs[pair.Value.TypeId].Id + "|1|" + "" + "|" + Program.Npcs[pair.Value.TypeId].Name + "|" + pair.Value.X + "|" + pair.Value.Y + "||-1|0|0|0|0");
                            pair.Value.Move.SendMovement(userId);
                            if (pair.Value.TypeId == 80)
                                Users[userId].Send("0|n|BOOOM|" + pair.Value.Id);
                        }
                        else if (Users[userId].Ship.selectedUserId != pair.Value.Id &&
                                 pair.Value.Move.PlayersInRange.Contains(userId))
                        {
                            Users[userId].Send("f|R|" + pair.Value.Id);
                            pair.Value.Move.PlayersInRange.Remove(userId);
                        }
                    }
                }
                catch (Exception) { }

                foreach (var pair in Users)
                {

                    try
                    {
                        foreach (var user in Users[userId].PlayersInRange)
                        {
                            if (!Program.Users.ContainsKey(user) || Program.Users[user].MapId != _id || Users[user].DestroyedClass)
                                Users[userId].PlayersInRange.Remove(user);
                        }
                    }
                    catch (Exception) { }

                    if (pair.Key == userId) continue;
                    var distance = Convert.ToInt32(Distance.Get(pair.Value.Ship.GetPosX(), nX, pair.Value.Ship.GetPosY(), nY));
                    if (distance < 2000)
                    {
                        if (Users[userId].Ship.MovingTask == 0)
                        {
                            Users[pair.Key].Send(packetSend);
                        }

                        if (Users[userId].PlayersInRange.Contains(pair.Key)) continue;
                        Users[userId].PlayersInRange.Add(pair.Key);
                        Users[pair.Key].Send("f|C|" + userId + "|" + Users[userId].Ship.Id + "|4|" + Users[userId].Ship.tagName + "|" + Users[userId].Ship.Name + "|" + Users[userId].Ship.x + "|" + Users[userId].Ship.y + "|" + Users[userId].Ship.factionId + "|0|" + Users[userId].Ship.rank + "|0|0|7||0|" + Users[userId].Ship.cloaked);
                        Users[userId].Send("f|C|" + pair.Value.UserId + "|" + pair.Value.Ship.Id + "|4|" + pair.Value.Ship.tagName + "|" + pair.Value.Ship.Name + "|" + pair.Value.Ship.x + "|" + pair.Value.Ship.y + "|" + pair.Value.Ship.factionId + "|0|" + pair.Value.Ship.rank + "|0|0|7||0|" + pair.Value.Ship.cloaked);
                        Users[pair.Key].Send(packetSend);
                        Users[userId].Send("0|n|d|" + pair.Key + "|" + Users[pair.Key].Ship.DronePacket);
                        Users[pair.Key].Send("0|n|d|" + userId + "|" + Users[userId].Ship.DronePacket);
                    }
                    else if (pair.Key != userId)
                    {
                        if (Users[userId].Ship.selectedUserId != pair.Key && !(_factionId != pair.Value.Ship.factionId && IsStarterMap))
                        {
                            if (Users[userId].PlayersInRange.Contains(pair.Key))
                            {
                                Users[userId].PlayersInRange.Remove(pair.Key);
                                Users[userId].Send("f|R|" + pair.Key);
                            }
                        }
                        if (Users[pair.Key].Ship.selectedUserId == userId ||
                            _factionId != Users[userId].Ship.factionId && IsStarterMap) continue;
                        if (!Users[userId].PlayersInRange.Contains(pair.Key)) continue;
                        Users[userId].PlayersInRange.Remove(pair.Key);
                        Program.Users[pair.Key].Send("f|R|" + userId);
                    }
                }

                Users[userId].Ship.MovingTask = 1;
                nX = Users[userId].Ship.GetPosX();
                nY = Users[userId].Ship.GetPosY();
                var inSecureArea = false;
                var radiation = false;
                if (_id == 13 || _id == 14 || _id == 15 || _id == 16)
                {
                }
                else
                {
                    foreach (var portal in Portals)
                    {
                        int pX = portal.X, pY = portal.Y;
                        var distance = Convert.ToInt32(Distance.Get(pX, nX, pY, nY));
                        if (distance >= 800 || Users[userId].Ship.isAttacking) continue;
                        inSecureArea = true;
                        break;
                    }
                }
                var distanceeee = (nX < 0 || nY < 0 || nX > 10400 || nY > 6400);
                var distanceXx = (nX < 0 || nY < 0 || nX > 41600 || nY > 25600);
                var distanceXxxx = (nX < 0 || nY < 0 || nX > 20800 || nY > 12800);
                {
                    if (distanceXx && _id == 16 || distanceeee && _id == 255 || distanceXx && _id == 29 || distanceXxxx && _id == 28 || distanceXxxx && _id == 27 || distanceXxxx && _id == 123 || distanceXxxx && _id == 26 || distanceXxxx && _id == 25 || distanceXxxx && _id == 24 || distanceXxxx && _id == 23 || distanceXxxx && _id == 22 || distanceXxxx && _id == 21 || distanceXxxx && _id == 20 || distanceXxxx && _id == 19 || distanceXxxx && _id == 18 || distanceXxxx && _id == 17 || distanceXxxx && _id == 15 || distanceXxxx && _id == 14 || distanceXxxx && _id == 13 || distanceXxxx && _id == 12 || distanceXxxx && _id == 11 || distanceXxxx && _id == 10 || distanceXxxx && _id == 9 || distanceXxxx && _id == 8 || distanceXxxx && _id == 7 || distanceXxxx && _id == 6 || distanceXxxx && _id == 5 || distanceXxxx && _id == 4 || distanceXxxx && _id == 3 || distanceXxxx && _id == 2 || distanceXxxx && _id == 1)
                    {
                        radiation = true;
                    }


                }
                if (Users[userId].Ship.factionId == 1 && _id == 1)
                {
                    var distance = Convert.ToInt32(Distance.Get(1000, nX, 1000, nY));
                    if (distance < 1000 && !Users[userId].Ship.isAttacking)
                    {
                        inSecureArea = true;
                    }
                }
                else if (Users[userId].Ship.factionId == 2 && _id == 5)
                {
                    var distance = Convert.ToInt32(Distance.Get(19800, nX, 1000, nY));
                    if (distance < 1000 && !Users[userId].Ship.isAttacking)
                    {
                        inSecureArea = true;
                    }
                }
                else if (Users[userId].Ship.factionId == 3 && _id == 9)
                {
                    var distance = Convert.ToInt32(Distance.Get(19600, nX, 11900, nY));
                    if (distance < 1000 && !Users[userId].Ship.isAttacking)
                    {
                        inSecureArea = true;
                    }
                }
                if (inSecureArea != Users[userId].Ship.InSecureArea ||
                                radiation != Users[userId].Ship.Radiation)
                    Users[userId].Send("0|D|10400|6400|" + Program.ToEnum(inSecureArea) + "|1|1|" +
                                       Program.ToEnum(radiation) + "|1|0");

                Users[userId].Ship.InSecureArea = inSecureArea;
                Users[userId].Ship.Radiation = radiation;

                foreach (var mine in Mines)
                {
                    int pX = Convert.ToInt32(mine.Value[0]), pY = Convert.ToInt32(mine.Value[1]);

                    if (Distance.Get(nX, pX, nY, pY) > 86) continue;
                    foreach (var player in Users)
                    {
                        player.Value.Send("0|LM|ST|MIN|" + mine.Key);
                    }
                    Mines.Remove(mine.Key);
                    switch (mine.Value[2])
                    {
                        case "1":
                            var acm = Users[userId].Ship.GetShipHP() / 5;
                            Users[userId].Ship.HP -= acm;
                            Users[userId].Send("0|Y|" + userId + "|" + userId + "|R|" + Users[userId].Ship.GetShipHP() + "|" + Users[userId].Ship.shield + "|" + acm + "|100|100");
                            Users[userId].Send("0|A|HPT|" + Users[userId].Ship.GetShipHP() + "|" + Users[userId].Ship.GetShipMaxHP() + "|" + userId);
                            break;
                        case "2":
                            Users[userId].Ship.cloaked = 0;
                            foreach (var ship in Users.Values)
                            {
                                ship.Send("0|n|INV|" + userId + "|0");
                            }
                            break;
                        case "3":
                            Users[userId].Ship.shield -= Users[userId].Ship.shield / 2;
                            Users[userId].Send("0|A|SHD|" + Users[userId].Ship.shield + "|" + Users[userId].Ship.maxShield + "|" + userId);
                            break;
                        case "4":
                            var ddm = Users[userId].Ship.GetShipMaxHP() / 5;
                            //  Users[userId].Ship.GetShipHP() -= ddm;
                            if (Users[userId].Ship.HP - ddm < 1)
                            {
                                Users[userId].Ship.HP = 0;
                                Users.Remove(userId);
                                foreach (var ship in Users.Values)
                                {
                                    ship.Send("0|K|" + userId);
                                }
                                Users[userId].discconectUser(1);

                            }
                            else
                            {
                                Users[userId].Ship.HP -= ddm;
                            }
                            Users[userId].Send("0|Y|" + userId + "|" + userId + "|R|" + Users[userId].Ship.GetShipHP() + "|" + Users[userId].Ship.shield + "|" + ddm + "|100|100");
                            Users[userId].Send("0|A|HPT|" + Users[userId].Ship.GetShipHP() + "|" + Users[userId].Ship.GetShipMaxHP() + "|" + userId);
                            break;
                    }
                }
            }
            catch (Exception e) { }
        }

        public void Move(uint userId)
        {

            try
            {
                Task.Factory.StartNew(async () =>
                {
                    if (!Users.ContainsKey(userId)) return;
                    if (Users[userId].Ship.buclemover) return;
                    Users[userId].Ship.buclemover = true;
                    while (Users.ContainsKey(userId))
                    {

                        _MoveShip(userId);
                        await Task.Delay(365);
                    }
                });
            }


            catch (Exception) { }


        }
        public static int count_on_map = 1;
        public void UsePortal(uint userId)
        {
            try
            {
                if (Users[userId].Ship.teleporting != 0) return;
                int uX = Users[userId].Ship.GetPosX(), uY = Users[userId].Ship.GetPosY();
                foreach (var portal in Portals)
                {
                    int pX = portal.X, pY = portal.Y;

                    var distancia = Convert.ToInt32(Distance.Get(pX, uX, pY, uY));
                    if (distancia >= 850) continue;
                    if (IsPvp)
                    {
                        if (Users[userId].Ship.regen < 4)
                        {
                            Users[userId].Send("0|A|STM|jump_cpu_failed_attack2");
                            return;
                        }
                    }
                    if (Users[userId].Ship.level < Program.Maps[portal.Map].Level && Users[userId].Ship.rank != 21)
                    {
                        Users[userId].Send("0|k|" + Program.Maps[portal.Map].Level);
                        return;
                    }

                
                    if (portal.Map==30|| portal.Map == 31)
                    {
                        map_id_= portal.Map;
                        if(count_on_map >= 3)
                        {
                            Users[userId].Send("0|A|STM|jumpgate_failed_no_gate");
                            Users[userId].Ship.teleporting = 0;
                            return;
                        }
                        //   new Thread(Users[userId].map__).Start();
                      //  Users[userId].Send("0|A|STD|mapid" + portal.Map);
                        count_on_map++;
                        userId_ = userId;
                        if (limit == false)
                        {
                            new Thread(s).Start();
                        }
                        //var s = new Do.game.User(User.hand);
                        //new Thread( s.s   ).Start();
                    }
                  
                        Users[userId].Ship.teleporting = 1;
              
                    Users[userId].Send("0|U|" + portal.Map + "|" + portal.Id + "");
                    Accion(userId);
                    return;
                }
                Users[userId].Send("0|A|STM|jumpgate_failed_no_gate");
            }
            catch (Exception)
            {
            }
        }
        public static bool limit = false;
        public static ushort map_id_;
        public static uint new_map_id = 0;
        public static bool return_attack = false;
        public static int user_on_map = 1;
        public static int a = 10;
        public static string ss = "t";
        public static uint userId_;
        public void s()
        {
            try
            {
                limit = true;
                var userId = userId_;
                var selected = Users[userId].Ship.selectedUserId;

                //  InSecureArea_local = true;


             //   Users[userId].Send("0|A|STD|Users on map -" + count_on_map);
                if (count_on_map == 1 || count_on_map == 2)
                {
               //     Users[userId].Send("0|A|STD|Users on map -" + count_on_map);

                    //if (Program.Maps[map_id_].Users[selected].Ship.InSecureArea)
                    //{
                    //    Users[userId].Send("0|P");
                    //    Program.Maps[map_id_].Users[userId].Send("0|P");
                    //    Users[userId].Ship.InRange = false;
                    //    Users[userId].Ship.isAttacking = false;
                    //    Users[userId].Send("0|A|STD|цифры=" + user_on_map);
                    //}
                    //else
                    //{

                        //for (; a >= 1; a--)
                        //{
                        //    foreach (var player in Program.Maps[map_id_].Users)
                        //    {
                        //        Users[userId].Send("0|A|STD|цифры___=" + count_on_map);
                        //        Thread.Sleep(1000);
                        //        Users[userId].Send("0|P"); player.Value.Send("0|A|STD|Time to fight - " + a);

                        //    }
                        //    if (a == 1 || return_attack == true)
                        //    {
                        //        return_attack = true;
                        //        Users[userId].Ship.isAttacking = true;

                        //        Program.Maps[map_id_].Users[userId].Ship.InSecureArea = false;
                        //    }
                        //}
                    }
              //  }
                //  if(user_on_map<2)
                //{
                ////    Send("ERR|2"); // EXTREME LOL
                //    discconectUser(2);
                //}
            }
            catch (Exception e)
            {
                Out.WriteLine(e.StackTrace.ToString());
            }

        }
        private async void Accion(uint userId)
        {
            try
            {
               
                if (Users[userId].Ship.teleport1) return;
                Users[userId].Ship.teleport1 = true;
                int uX = Users[userId].Ship.GetPosX(), uY = Users[userId].Ship.GetPosY();

                foreach (var portal in Portals)
                {
                    int pX = portal.X, pY = portal.Y;

                    var distancia = Convert.ToInt32(Distance.Get(pX, uX, pY, uY));
                    if (distancia >= 850) continue;
                    await Task.Delay(2800);
                    if (!Users.ContainsKey(userId)) continue;
                    Users[userId].Ship.InRange = false;

                    Users[userId].Ship.isAttacking = false;
                    foreach (
                        var player in
                            Users.Where(player => player.Value.Ship.selectedUserId == Users[userId].UserId))
                    {
                        player.Value.Ship.selectedUserId = 0;
                        player.Value.Ship.isAttacking = false;
                        player.Value.Send("SELECT|-1");
                        player.Value.Send("0|N|-1");
                        player.Value.Ship.selected1 = false;
                    }
                    Users[userId].Ship.selected1 = false;
                    Users[userId].Ship.x = portal.NewX;
                    Users[userId].Ship.y = portal.NewY;
                    Users[userId].Ship.destinationx = portal.NewX;
                    Users[userId].Ship.destinationy = portal.NewY;
                    Users[userId].PlayersInRange = new List<uint>();
                    Users[userId].PlayersInRange.Add(userId);
                    Users[userId].MapId = Convert.ToUInt16(portal.Map);
                    new_map_id=Convert.ToUInt16(portal.Map);
                    Users[userId].Send("0|z");
                    Users[userId].Ship.selectedUserId = 0;
                    Users[userId].Ship.isAttacking = false;
                    Users[userId].Send("0|N|-1");
                    Users[userId].Ship.tickCount = 0;
                    Users[userId].Ship.teleported = true;
                    try
                    {
                        foreach (var alien in Aliens)
                        {
                            if (alien.Value.Move.PlayersInRange.Contains(userId))
                                alien.Value.Move.PlayersInRange.Remove(userId);
                        }
                    }
                    catch (Exception) { }
                    foreach (
                        var player in
                            Program.Maps[Users[userId].MapId].Users.Where(
                                player => player.Value.Ship.selectedUserId == Users[userId].UserId))
                    {

                        try
                        {
                            player.Value.Ship.selectedUserId = 0;
                            player.Value.Ship.isAttacking = false;
                            player.Value.Send("SELECT|-1");
                            player.Value.Send("0|N|-1");
                        }
                        catch (KeyNotFoundException) { }
                    }


                    RemoveUser(userId);
                    Program.Maps[portal.Map].AddUser(userId);
                }
            }
            catch (Exception)
            {
            }
        }
        public static void SendPortals2(uint userId)
        {
            foreach (var portal in prt)
            {
                Users2[userId].Send("C|p|" + portal.Id + "|1|1|" + portal.X + "|" + portal.Y + "|1|0");            
            }
        }
        public static List<Portal> prt;
        public static Dictionary<uint, User> Users2;
        public void SendPortals(uint userId)
        {
            foreach (var portal in Portals)
            {
                Users[userId].Send("C|p|" + portal.Id + "|1|1|" + portal.X + "|" + portal.Y + "|1|0");
                prt = Portals;
                Users2 = Users;
            }
        }
    }
}