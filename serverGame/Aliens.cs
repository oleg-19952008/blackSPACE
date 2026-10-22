using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Do.serverGame.AlienClasses;
using System.Threading;
 
using System.Data;
 
namespace Do.serverGame
{
    class Alien
    {
        private enum AlienId
        {
            Cubicon = 80,
            Protegit = 81
        }
        public uint Id { get; set; }
        public uint Shield { get; set; }
        public uint MaxShield { get; set; }
        public uint Hp { get; set; }
        public uint MaxHp { get; set; }
        public uint SelectedUserId = 0;
        private Thread a, b, c, d;
        public uint Ai { get; set; }
        public ushort TypeId { get; set; }
        public ushort MapId { get; set; }
        public AlienMovement Move;
        public AlienAttack Attack;
        public int RelCount = 0;
        public int LockCount = 0;
        public bool Destroyed = false;
        public uint OwnerId { get; set; }
        public uint Lock = 0;
        public int X { get; set; }
        public int Y { get; set; }
        private Dictionary<uint,int> _shooters=new Dictionary<uint, int>();
        public Alien(uint id, ushort typeId, ushort mapId, uint ownerId = 0, int count = 0)
        {

                switch (typeId)
                {
                case 50:
                    Attack = new AlienAttack(this);
                    Move = new AlienMovement(this);
                    Id = id;
                    TypeId = typeId;
                    MapId = mapId;
                    Shield = Program.Npcs[typeId].Shield;
                    Hp = Program.Npcs[typeId].Hp;
                    Ai = Program.Npcs[typeId].Ai;
                    switch (count)
                    {
                        case 0:
                            X = 6000;
                            Y = 4000;
                            break;
                        case 1:
                            X = 6000;
                            Y = 9000;
                            break;
                        case 2:
                            X = 16000;
                            Y = 4000;
                            break;
                        default:
                            X = 16000;
                            Y = 9000;
                            break;
                    }
                    Attack.Damage = 0;
                    Move.MovementSpeed = Program.Npcs[typeId].Speed;
                    Task.Factory.StartNew(Attack.StartCubi);
                    Task.Factory.StartNew(Release);
                    Task.Factory.StartNew(CheckUsers);

                    break;
                case 103:
                    Attack = new AlienAttack(this);
                    Move = new AlienMovement(this);
                    Id = id;
                    OwnerId = ownerId;
                    TypeId = typeId;
                    X = Program.Maps[mapId].Aliens[ownerId].X;
                    Y = Program.Maps[mapId].Aliens[ownerId].Y;
                    Move.UserX = X + Program.Random.Next(-150, 150);
                    Move.UserY = Y + Program.Random.Next(-150, 150);
                    MapId = mapId;
                    Attack.Damage = Program.Npcs[typeId].Damage;
                    Shield = Program.Npcs[typeId].Shield;
                    Hp = Program.Npcs[typeId].Hp;
                    Ai = Program.Npcs[typeId].Ai;
                    Move.MovementSpeed = Program.Npcs[typeId].Speed;
                    if (Program.Maps[MapId].Users.ContainsKey(Program.Maps[MapId].Aliens[ownerId].SelectedUserId))
                        SelectedUserId = Program.Maps[MapId].Aliens[ownerId].SelectedUserId;
                    Task.Factory.StartNew(Move.StartProt);
                    Task.Factory.StartNew(Attack.Start);
                    Task.Factory.StartNew(Release);
                    Task.Factory.StartNew(CheckUsers);

                    break;
                case 80:
                    Attack = new AlienAttack(this);
                    Move = new AlienMovement(this);
                    Id = id;
                    TypeId = typeId;
                    MapId = mapId;
                    Shield = Program.Npcs[typeId].Shield;
                    Hp = Program.Npcs[typeId].Hp;
                    Ai = Program.Npcs[typeId].Ai;
                    X = Program.Random.Next(1000, 19000);
                    Y = Program.Random.Next(1000, 11100);
                    Move.X = Program.Random.Next(1000, 19000);
                    Move.Y = Program.Random.Next(1000, 11100);
                    //switch (count)
                    //{
                    //    case 0:
                    //        X = 6000;
                    //        Y = 4000;
                    //        break;
                    //    case 1:
                    //        X = 6000;
                    //        Y = 9000;
                    //        break;
                    //    case 2:
                    //        X = 16000;
                    //        Y = 4000;
                    //        break;
                    //    default:
                    //        X = 16000;
                    //        Y = 9000;
                    //        break;
                    //}
                    Attack.Damage = 0;
                    Move.MovementSpeed = Program.Npcs[typeId].Speed;
                    Task.Factory.StartNew(Attack.StartCubi);
                    Task.Factory.StartNew(Release);
                    Task.Factory.StartNew(CheckUsers);
                    Task.Run(() => Move.Start()); 
                    break;
                case 116:
                    Attack = new AlienAttack(this);
                    Move = new AlienMovement(this);
                    Id = id;
                    TypeId = typeId;
                    MapId = mapId;
                    Shield = Program.Npcs[typeId].Shield;
                    Hp = Program.Npcs[typeId].Hp;
                    Ai = Program.Npcs[typeId].Ai;
                    X = Program.Random.Next(1000, 19000);
                    Y = Program.Random.Next(1000, 11100);
                    Move.X = Program.Random.Next(1000, 19000);
                    Move.Y = Program.Random.Next(1000, 11100);
                    //switch (count)
                    //{
                    //    case 0:
                    //        X = 6000;
                    //        Y = 4000;
                    //        break;
                    //    case 1:
                    //        X = 6000;
                    //        Y = 9000;
                    //        break;
                    //    case 2:
                    //        X = 16000;
                    //        Y = 4000;
                    //        break;
                    //    default:
                    //        X = 16000;
                    //        Y = 9000;
                    //        break;
                    //}
                    Attack.Damage = Program.Npcs[typeId].Damage;
                    Move.MovementSpeed = Program.Npcs[typeId].Speed;
                    Task.Factory.StartNew(Attack.StartCubi);
                    Task.Factory.StartNew(Release);
                    Task.Factory.StartNew(CheckUsers);
                    Task.Run(() => Move.Start()); Task.Factory.StartNew(Attack.Start);
                    break;
                case 81:
                    Attack = new AlienAttack(this);
                    Move = new AlienMovement(this);
                    Id = id;
                    OwnerId = ownerId;
                    TypeId = typeId;
                    X = Program.Maps[mapId].Aliens[ownerId].X;
                    Y = Program.Maps[mapId].Aliens[ownerId].Y;
                    Move.UserX = X + Program.Random.Next(-150, 150);
                    Move.UserY = Y + Program.Random.Next(-150, 150);
                    MapId = mapId;
                    Attack.Damage = Program.Npcs[typeId].Damage;
                    Shield = Program.Npcs[typeId].Shield;
                    Hp = Program.Npcs[typeId].Hp;
                    Ai = Program.Npcs[typeId].Ai;
                    Move.MovementSpeed = Program.Npcs[typeId].Speed;
                    if (Program.Maps[MapId].Users.ContainsKey(Program.Maps[MapId].Aliens[ownerId].SelectedUserId))
                        SelectedUserId = Program.Maps[MapId].Aliens[ownerId].SelectedUserId;
                    Task.Factory.StartNew(Move.StartProt);
                    Task.Factory.StartNew(Attack.Start);
                    Task.Factory.StartNew(Release);
                    Task.Factory.StartNew(CheckUsers);
                    break;
                case 117:
                    Attack = new AlienAttack(this);
                    Move = new AlienMovement(this);
                    Id = id;
                    OwnerId = ownerId;
                    TypeId = typeId;
                    X = Program.Maps[mapId].Aliens[ownerId].X;
                    Y = Program.Maps[mapId].Aliens[ownerId].Y;
                    Move.UserX = X + Program.Random.Next(-150, 150);
                    Move.UserY = Y + Program.Random.Next(-150, 150);
                    MapId = mapId;
                    Attack.Damage = Program.Npcs[typeId].Damage;
                    Shield = Program.Npcs[typeId].Shield;
                    Hp = Program.Npcs[typeId].Hp;
                    Ai = Program.Npcs[typeId].Ai;
                    Move.MovementSpeed = Program.Npcs[typeId].Speed;
                    if (Program.Maps[MapId].Users.ContainsKey(Program.Maps[MapId].Aliens[ownerId].SelectedUserId))
                        SelectedUserId = Program.Maps[MapId].Aliens[ownerId].SelectedUserId;
                    Task.Factory.StartNew(Move.StartProt);
                    Task.Factory.StartNew(Attack.Start);
                    Task.Factory.StartNew(Release);
                    Task.Factory.StartNew(CheckUsers);
                    break;
               
                   
                default:
                        Attack = new AlienAttack(this);
                        Move = new AlienMovement(this);
                        Id = id;
                        TypeId = typeId;
                        X = Program.Random.Next(1000, 19000);
                        Y = Program.Random.Next(1000, 11100);
                        Move.X = Program.Random.Next(1000, 19000);
                        Move.Y = Program.Random.Next(1000, 11100);
                        MapId = mapId;
                        Attack.Damage = Program.Npcs[typeId].Damage;
                        Shield = Program.Npcs[typeId].Shield;
                        Hp = Program.Npcs[typeId].Hp;
                        Ai = Program.Npcs[typeId].Ai;
                        Move.MovementSpeed = Program.Npcs[typeId].Speed;
                         Task.Run(()=>Move.Start());
                         Task.Factory.StartNew( Attack.Start);
                         Task.Factory.StartNew( Release);
                         Task.Factory.StartNew( CheckUsers);
                     
                        break;
                }
                MaxHp = Hp;
                MaxShield = Shield;
        }
        private async void Release()
        {
            try
            {
                while (true)
                {
                    if (!Program.Maps[MapId].Aliens.ContainsKey(Id))
                        return;

                    if (SelectedUserId != 0 && Program.Users.ContainsKey(SelectedUserId))
                    {
                        if (Distance.Get(Program.Users[SelectedUserId].Ship.x, X, Program.Users[SelectedUserId].Ship.y, Y) > 500)
                        {
                            RelCount++;
                        }
                        else
                        {
                            RelCount = 0;
                        }
                    }
                    if (RelCount > 4)
                    {
                        RelCount = 0;
                        SelectedUserId = 0;
                        Move.NewRandomCoord();
                    }
                    if (Ai < 2)
                    {
                        if (Lock != 0 && Program.Users.ContainsKey(Lock))
                        {
                            if (Distance.Get(Program.Users[Lock].Ship.x, X, Program.Users[Lock].Ship.y, Y) > 400)
                            {
                                LockCount++;
                            }
                            else
                            {
                                LockCount = 0;
                            }
                        }
                        if (LockCount > 4)
                        {
                            LockCount = 0;
                            Lock = 0;
                            Move.NewRandomCoord();
                        }
                    }
                    await Task.Delay(2000);
                }
            }
            catch (Exception) { }
        }
        private async void CheckUsers()
        {
           try{while(true)
		   {
               if (!Program.Maps[MapId].Aliens.ContainsKey(Id))
                   return;

               try
               {
                   foreach (var user in Move.PlayersInRange)
                   {
                       if (!Program.Users.ContainsKey(user) || Program.Users[user].MapId != MapId || Program.Users[user].DestroyedClass)
                           Move.PlayersInRange.Remove(user);
                   }
               }
               catch (Exception) { }
                if (Ai == 2 && SelectedUserId==0)
                {
                    foreach (var user in from user in Move.PlayersInRange let distance = Distance.Get(X, Program.Users[user].Ship.GetPosX(), Y, Program.Users[user].Ship.GetPosY()) where distance < 1100 && !Program.Users[user].Ship.InSecureArea && Program.Users[user].Ship.cloaked == 0 select user)
                    {
                        ReceivedAttack(user);
                        break;
                    }
                }
                 if (Ai < 2 && Lock==0)
                {
                    foreach (var user in from user in Move.PlayersInRange let distance = Distance.Get(X, Program.Users[user].Ship.GetPosX(), Y, Program.Users[user].Ship.GetPosY()) where distance < 900 && !Program.Users[user].Ship.InSecureArea && Program.Users[user].Ship.cloaked == 0 select user)
                    {
                        Lock = user;
                        break;
                    }
                }
		       try
		       {
                   foreach (var portal in Program.Maps[MapId].Portals)
                   {
                       int pX = portal.X, pY = portal.Y;
                       var distance = Convert.ToInt32(Distance.Get(pX, X, pY, Y));
                       if (distance >= 800 || Program.Maps[MapId].Users[SelectedUserId].Ship.selectedUserId == Id) continue;
                       SelectedUserId = 0;
                       Lock = 0;
                       RelCount = 0;
                       break;
                   }
               }
               catch (Exception) { }
		       await Task.Delay(3500);
           }
           }
           catch (Exception) { }
        }
        public void ReceivedAttack(uint userId)
        {
            if (Destroyed) return;
            
            try
            {
                if (TypeId == 80|| TypeId == 116)
                {
                    if (!_shooters.ContainsKey(userId))
                        _shooters.Add(userId, 0);
                    else
                    {
                        _shooters[userId]++;
                    }
                }
                if (SelectedUserId == 0)
                {
                    RelCount = 0;
                    Move.Tick = 0;
                    SelectedUserId = userId;
                    if (TypeId != 80 || Attack.Childs.Count == 0) return;
                    foreach (var kid in Attack.Childs)
                    {
                        Program.Maps[MapId].Aliens[kid].SelectedUserId = userId;
                    }
                }
                else
                {
                    if (Program.Users[SelectedUserId].Ship.isAttacking &&
                        Program.Users[SelectedUserId].Ship.selectedUserId == Id) return;
                    RelCount = 0;
                    SelectedUserId = userId;
                    Move.Tick = 0;
                    if (TypeId != 80 || Attack.Childs.Count == 0) return;
                    foreach (var kid in Attack.Childs)
                    {
                        Program.Maps[MapId].Aliens[kid].SelectedUserId = userId;
                    }
                }
            }
            catch (Exception e)
            {
            }
            if(!Program.Maps[MapId].Users.ContainsKey(SelectedUserId))
            {
                SelectedUserId = userId;
                Move.Tick = 0;
                RelCount = 0;
                if (TypeId != 80 || Attack.Childs.Count == 0) return;
                foreach (var kid in Attack.Childs)
                {
                    Program.Maps[MapId].Aliens[kid].SelectedUserId = userId;
                }
            }
        }

        public void Regenerate()
        {
            if (TypeId == 80 ||TypeId == 81||TypeId == 126 || TypeId == 116)
            {
                RegenCubi();
            }
            else
            {
                RegenOther();
            }
        }
        private async void RegenCubi()
        {
            try { 
                if(TypeId==81|| TypeId==116)
                {
                    Program.UpdatePlayer(SelectedUserId, TypeId);
                    Move.PlayersInRange.Clear();
                    Shield = MaxShield;
                    Hp =MaxHp;
                    Destroyed = false;
                    SelectedUserId = Program.Maps[MapId].Aliens[OwnerId].SelectedUserId;
                    X = Program.Maps[MapId].Aliens[OwnerId].X;
                    Y = Program.Maps[MapId].Aliens[OwnerId].Y;
                    return;
                }
           
            var totalShoots = _shooters.Where(bitch => Program.Maps[MapId].Users.ContainsKey(bitch.Key)).Sum(bitch => bitch.Value);

            foreach (var bitch in _shooters)
                {
                    if (!Program.Maps[MapId].Users.ContainsKey(bitch.Key)) continue;
                    var rewuri = (int)Math.Round(Convert.ToDouble(Program.Npcs[TypeId].Uri / totalShoots * bitch.Value));
                    var rewcre = (int)Math.Round(Convert.ToDouble(Program.Npcs[TypeId].Cre / totalShoots * bitch.Value));
                    var rewexp = (int)Math.Round(Convert.ToDouble(Program.Npcs[TypeId].Exp / totalShoots * bitch.Value));
                    var rewhonor = (int)Math.Round(Convert.ToDouble(Program.Npcs[TypeId].Hon / totalShoots * bitch.Value));
                    Program.UpdatePlayer(bitch.Key,0,rewexp,rewuri,rewcre,rewhonor);
                  
                }
          
            _shooters = new Dictionary<uint, int>();
            SelectedUserId = 0;
            Move.PlayersInRange = new List<uint>();
            await Task.Delay(300000);
            Shield = Program.Npcs[TypeId].Shield;
            Hp = Program.Npcs[TypeId].Hp;
            Destroyed = false;
            }
            catch (Exception) { }
        }
    
        
        private async void RegenOther()
        {
            if (SelectedUserId != 0 && Program.Users.ContainsKey(SelectedUserId) && Destroyed)
            {
                Program.UpdatePlayer(SelectedUserId, TypeId);
            }
            Move.PlayersInRange.Clear();
            X = Program.Random.Next(1000, 19000);
            Move.NewRandomCoord();
            Y = Program.Random.Next(1000, 11100);
            Shield = MaxShield;
            Hp = MaxHp;
            await Task.Delay(4000);
                  Destroyed = false;
            SelectedUserId = 0;
            Lock = 0;
        }
    }
}
