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
 * Partial-class User: Поля состояния игрока, счётчик онлайна, конструктор, фоновый цикл UpdateShip_.
 */
partial class User
{
        private bool _socketClosed;
        private bool _isboss;
        private bool _canLoadHellstorm = true;
        public bool DestroyedClass = false;
        // Spaceball (стационарные боевые станции MMO/EIC/VRU).
        // Координаты НИЖЕ — это НЕ поле spaceball и НЕ ворота: это позиции самих
        // станций на картах 1-1 / 5-1 / 9-9 (фракционные NPC-станции). Они взяты
        // один-в-один из пакета "0|s|0|1|...|<x>|<y>", который сервер рассылает
        // клиенту в sendStations(), и точно такие же значения ({1000,1000,1},
        // {19800,1000,5}, {19600,11900,9}) лежали в _mmoXy/_eicXy/_vruXy в
        // исходном коде (до правок). Поле самого spaceball (карта 4-4, ворота
        // сверху/снизу/слева у края карты и мяч в центре) сервером не моделируется
        // — оно целиком на стороне клиента/движка, менять там нечего.
        // factionId 1 -> MMO (1-1), 2 -> EIC (5-1), 3 -> VRU (9-9).
        public static readonly int[][] SpaceballStations =
        {
            new int[] { 1, 1000, 1000, 1 },     // MMO  (1-1)
            new int[] { 2, 19800, 1000, 5 },    // EIC  (5-1)
            new int[] { 3, 19600, 11900, 9 },   // VRU  (9-9)
        };

        public static int[] FindSpaceballStation(int factionOrMap, bool byFaction)
        {
            foreach (var st in SpaceballStations)
                if (byFaction ? st[0] == factionOrMap : st[3] == factionOrMap)
                    return st;
            return null;
        }

        public bool GodMode = false;
        public int LastPing = 0;
        // Состояние модуля дрона на стороне клиента (см. Pet.Activate / пакет "0|PET|U").
        public bool ClientPetInitialized = false;
        public bool _petInitSent = false;
        public int Blk = 0;
        public int GodModeDelay = 300;
        private bool _repairRequest;
        private bool _isRepairing;
        private bool _cancelARocket = true;
        public uint UserId, AttackId = 0;
        public ushort MapId = 1;
        public ushort LaserId = 1;
        public ushort RocketId = 1;
        public ushort Precision = 0;
        public ushort LoadedHellstorm = 0;
        public ushort HellstormId = 8;
        public string Ip;
        public int CurrentConfig = 1;
        public DateTime Config = new DateTime(2013, 3, 1, 7, 0, 0);
        public bool EnergyLeechActive = false;
        private DateTime _sbu = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _brb = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _ela = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _rpm = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _eci = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _ish = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _emp = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _smb = new DateTime(2013, 3, 1, 7, 0, 0);
        private DateTime _mine = new DateTime(2013, 3, 1, 7, 0, 0);
        /*
                private DateTime Fwx = new DateTime(2013, 3, 1, 7, 0, 0);
        */
        public uint CreatedFireworks = 0;
        public List<uint> InvitationNames = new List<uint>();
        public List<uint> GroupNames = new List<uint>();
        public List<uint> PlayersInRange = new List<uint>();
        public List<uint> LaserCooldownList = new List<uint>();
        private int _damageTaken;
        public static Dictionary<uint, User> Users;
        // Счетчик онлайна изменяется из потоков обработки сокетов и фоновых циклов,
        // поэтому операции инкремента/декремента должны быть атомарными (Interlocked).
        private static int _online = 0;
        public static int online
        {
            get { return System.Threading.Interlocked.CompareExchange(ref _online, 0, 0); }
            set { System.Threading.Interlocked.Exchange(ref _online, value); }
        }
        public static void IncrementOnline()
        {
            System.Threading.Interlocked.Increment(ref _online);
        }
        // Флаг "этот пользователь учтён в счётчике онлайна" (1 — учтён, 0 — снят).
        // Гарантирует, что декремент выполняется ровно один раз на сессию.
        private int _onlineCounted = 0;
        public void MarkOnlineCounted()
        {
            System.Threading.Interlocked.Exchange(ref _onlineCounted, 1);
        }
        // Потокобезопасный декремент: никогда не уходит в отрицательные значения.
        public static void DecrementOnline()
        {
            int current;
            do
            {
                current = System.Threading.Interlocked.CompareExchange(ref _online, 0, 0);
                if (current <= 0) return;
            }
            while (System.Threading.Interlocked.CompareExchange(ref _online, current - 1, current) != current);
        }
        public Clan Clan;
        // Ship объявлен в главном файле Users.cs (partial User)
        public Setting Settings;
        public SettingsAssembly Setting;
        public Pet Pet;
    public   static Socket __handler;
        public User(Socket handler)
        {
            __handler = handler;
            _handler = handler;

            Ip = handler.RemoteEndPoint.ToString().Split(':')[0];

            handler.BeginReceive(_buffer, 0, _buffer.Length, 0, ReadCallback, this);
        }
        public static int User_id___ = 0;
        public async void UpdateShip_()
        {
            // БЫЛО: while(true) без задержки и без проверки выхода — бесконечный
            // "fire-and-forget" цикл в async void, который продолжал слать пакеты
            // уже отключённому игроку (спам в лог/сокет после дисконнекта).
            // СТАЛО: цикл останавливается при закрытии сокета/уничтожении класса.
            // ВАЖНО: здесь отправляются только speed/бустеры/снаряжение — НИКАКИХ
            // пакетов HP (HPT/HL/N/RDY|I), чтобы не перетирать актуальное HP корабля
            // на клиенте (см. регрессию "корабль без хп").
            Send("0|A|BS|0/0/" + Ship.dmgbooster + "/" + Ship.shieldbooster + "/0/0/0/" + Ship.hpbooster);
            try
            {
                while (!_socketClosed && !DestroyedClass && !_destroyed)
                {
                    // Фоновая сверка с БД каждые 30 сек: покупка в магазине/другой клиент
                    // обновили строки в базе — подтянуть изменения ресурсов и вооружения.
                    SyncWithDatabase();
                    Send("0|A|v|" + Ship.speed);
                    //if (Ship.rank == 21)
                    //{
                    //    Send("0|A|STD|Update ammo");
                    //}
                    //if (Ship.isMoving) {
                    //    return;
                    //}
                    //else
                    //{
                    //    UpdateShip();
                    //}
                    //if (Ship.isAttacking)
                    //{

                    //}
                    //else
                    //{
                    //    await Task.Delay(201000);
                    // UpdateShip();
                    //}
                    //       UpdateLaser();

                    //    LoadConfigs();
                    //Ship.x1 = Program.loadAmmos(UserId, "ammunition_laser_lcb-10");
                    //Ship.x2 = Program.loadAmmos(UserId, "ammunition_laser_mcb-25");
                    //Ship.x3 = Program.loadAmmos(UserId, "ammunition_laser_mcb-50");
                    //Ship.UCB100 = Program.loadAmmos(UserId, "ammunition_laser_ucb-100");
                    //Ship.rsb = Program.loadAmmos(UserId, "ammunition_laser_rsb-75");
                    //Ship.sab = Program.loadAmmos(UserId, "ammunition_laser_sab-50");
                    Send("0|B|" + Ship.x1 + "|" + Ship.x2 + "|" + Ship.x3 + "|" + Ship.UCB100 + "|" + Ship.sab + "|" + Ship.rsb );
                    CheckLevel();
                    Send("0|A|ITM|0|0|0|0|" + Ship.RepBot + "|0|0|" + Program.ToEnum(Ship.SMBCPU) + "|" + Program.ToEnum(Ship.ISHCPU) + "|0|0|" + Program.ToEnum(Ship.AROCKET) + "|0|0|0|0|0");
                    Send("0|TX|S|" + ((Ship.ELAA != 0) ? "1" : "0") + "|" + Ship.ELAA + "|0|" + ((Ship.ECIA != 0) ? "1" : "0") + "|" + Ship.ECIA + "|0|" + ((Ship.RPMA != 0) ? "1" : "0") + "|" + Ship.RPMA + "|0|" + ((Ship.SBUA != 0) ? "1" : "0") + "|" + Ship.SBUA + "|0|" + ((Ship.BRBA != 0) ? "1" : "0") + "|" + Ship.BRBA + "|0");
                    Send("0|ps|blk|0");
                    Send("0|3|" + Ship.R310 + "|" + Ship.PLT2026 + "|" + Ship.PLT2021 + "|" + Ship.PLT3030 + "|" + Ship.PLD8 + "|" + Ship.DCR250 + "|" + Ship.WIZ + "|" + Ship.ACM01 + "|" + Ship.SMBA + "|" + Ship.ISHA + "|" + Ship.EMPA + "|" + Ship.EMPM01 + "|" + Ship.SABM01 + "|" + Ship.DDM01);

                    switch (Ship.Id)
                    {
                        case 63:
                            Send("0|SD|S|1|1|0");
                            break;
                        case 64:
                            Send("0|SD|S|2|1|0");
                            break;
                        case 65:
                            Send("0|SD|S|3|1|0");
                            break;
                        case 66:
                            Send("0|SD|S|4|1|0");
                            break;
                        case 67:
                            Send("0|SD|S|5|1|0");
                            break;
                        case 200:
                            Send("0|SD|S|5|1|0");
                            break;
                        case 98:
                            Send("0|SD|S|1|1|0");
                            Send("0|SD|S|2|1|0");
                            Send("0|SD|S|3|1|0");
                            Send("0|SD|S|4|1|0");
                            Send("0|SD|S|5|1|0");
                            Send("0|SD|S|6|1|0");
                            break;
                        default:
                            
                            //var file_prem = System.IO.File.ReadAllLines(Environment.CurrentDirectory + @"\prem.txt");
                            //foreach (var user__ in file_prem)
                            //{
                            //    if (UserId == Convert.ToInt32(user__))
                            //    {
                            //        Send("0|n|t|" + UserId + "|Premium Gamer");

                            //    }
                            //}

                            //Send("0|SD|S|4|1|0");

                            break;
                    }
                    await Task.Delay(5000);
                }
            }
            catch { }
        }
}
}
