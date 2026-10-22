//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Do.game.usersClass
//{
//    public class Ship
//    {
//        public List<string> C1Lasers = new List<string>();
//        public List<string> C2Lasers = new List<string>();
//        public List<string> C1Gens = new List<string>();
//        public List<string> C2Gens = new List<string>();

//        public uint Id, cloaked, HP, maxHP, maxHP1, maxHP2, speed, shield, cloaks, mapId = 0, rocketdamage = 600, drones, drones1, drones2, maxShield, damage, selectedUserId, rank, clanID, dmgbooster, shieldbooster, hpbooster;
//        public string Name, DronePacket;
//        public uint damage1, premium, dead, damage2, damage22, shield22, speed22, maxShield22, maxShield2, maxShield1, shield2, shield1, speed2, speed1;
//        public bool InSecureArea = false, isAttacking = false;
//        public ushort factionId = 0, shieldAbsorb;
//        public bool exited = false, selected1 = false, buclersb = false, lunchrsb = false, buclelaser = false, usetelep = false, useconf = false, sockclos = false, teleported = false, teleport1 = false, teleported2 = false, teleport2 = false, buclemover = false, bucleregen = false, bucleexit = false, nomove = false, AvisedO = false, isLogout = false, destroyed = false, CanUseSolace = true, CanUseSpectrum = true, CanUseSentinel = true, CanUseRocket = true, CanUseVenom = true, CanUseDiminisher = true, InRange = false;
//        public int ELAA, ECIA, SBUA, BRBA, RPMA, exp, honor, level, UCB100, x1, x2, x3, sab, rsb, lasers, lasers1, lasers2, PLT2026, PLT3030, PLT2021, ACM01, DDM01, EMPM01, EMPA, SMBA, ISHA, SABM01, PLD8, DCR250, WIZ, R310, HSTRM01, ECO10, UBR100;
//        public int MovingTask, tickCount = 0;
//        public int destinationx;
//        public int destinationy;
//        public int logout2;
//        public int logout3;
//        public int ExitCount;
//        public int robw;
//        public int teleporting;
//        public int teleporting2;
//        public int regen;
//        public int REP3;
//        public int malus;
//        public int x, y;
//        public int RepBot1 = 0;
//        public int RepBot2 = 0;
//        public int RepBot = 0;

//        public bool ARepBot = false;
//        public bool ARepBot1 = false;
//        public bool ARepBot2 = false;

//        public int HSTRM = 0;
//        public int HSTRM1 = 0;
//        public int HSTRM2 = 0;
//        public int MaxHellstorm = 0;

//        public bool SMBCPU = false;
//        public bool SMBCPU1 = false;
//        public bool SMBCPU2 = false;

//        public bool ISHCPU = false;
//        public bool ISHCPU1 = false;
//        public bool ISHCPU2 = false;

//        public bool AROCKET = false;
//        public bool AROCKET1 = false;
//        public bool AROCKET2 = false;

//        public bool AHSTRM = false;
//        public bool AHSTRM1 = false;
//        public bool AHSTRM2 = false;

//        public bool CanBeLocked { get; set; }
//        public bool CanUseEMP { get; set; }
//        public bool CanUseRockets { get; set; }
//        public bool Spectrum { get; set; }
//        public bool PetActivated { get; set; }
//        public bool Diminisher { get; set; }
//        public bool Venom { get; set; }
//        public bool Fortress { get; set; }
//        public bool Singularity { get; set; }
//        public bool delayedrep { get; set; }
//        public bool CanPutMine { get; set; }
//        public bool CanUseRSB { get; set; }
//        public bool CanLoadHellstorm { get; set; }
//        public bool ShieldLeech { get; set; }
//        public bool repactivate { get; set; }
//        public bool shdactivate { get; set; }
//        public bool dmgsmb { get; set; }
//        public bool realattack { get; set; }
//        public bool isMoving { get; set; }
//        public bool CanUseBRB { get; set; }
//        public bool CanUseSBU { get; set; }
//        public bool CanUseELA { get; set; }
//        public bool CanUseISH { get; set; }
//        public bool CanUseSMB { get; set; }
//        public bool CanRep { get; set; }
//        public bool checkrep { get; set; }
//        public bool CanUseAbility { get; set; }
//        public bool CanSwitchConfig { get; set; }
//        public bool ISH { get; set; }
//        public bool INV { get; set; }
//        public bool ELA { get; set; }
//        public bool Radiation { get; set; }
//        public String RangeX { get; set; }
//        public String RangeY { get; set; }
//        public string tagName { get; set; }

//        public int credits, uri = 0, droneslv = 0, repdelay = 5000, resolution;
//        public void ParseConfigs(uint DamageC1, uint ShieldC1, uint SpeedC1, uint DronesC1, uint DamageC2, uint ShieldC2, uint SpeedC2, uint DronesC2)
//        {
//            shield = ShieldC1;
//            shield22 = ShieldC2;
//            maxShield = ShieldC1;
//            maxShield22 = ShieldC2;
//            damage = DamageC1;
//            damage22 = DamageC2;
//            if (Id == 8 || Id == 16 || Id == 17 || Id == 18 || Id == 58 || Id == 60)
//            {
//                HP = 180000;
//                maxHP = HP;
//                maxHP2 = maxHP;
//                speed = 360 + SpeedC1;
//                speed22 = 360 + SpeedC2;
//            }
//            else if (Id == 10 || Id == 52 || Id == 17 || Id == 53 || Id == 19 || Id == 54 || Id == 56 || Id == 57 || Id == 59 || Id == 61 || Id == 62 || Id == 63 || Id == 64 || Id == 65 || Id == 66 || Id == 67 || Id == 68 || Id == 86 || Id == 87 || Id == 88 || Id == 109 || Id == 110)
//            {
//                HP = 256000;
//                maxHP = HP;
//                maxHP2 = maxHP;
//                speed = 300 + SpeedC1;
//                speed22 = 300 + SpeedC2;
//            }
//            else if (Id == 47)
//            {
//                HP = 360000;
//                maxHP = HP;
//                maxHP2 = maxHP;
//                speed = 310 + SpeedC1;
//                speed22 = 310 + SpeedC2;
//                damage += damage / 10;
//                shield += shield / 5;
//            }
//            else if (Id == 1)
//            {
//                HP = 4000;
//                maxHP = HP;
//                maxHP2 = maxHP;
//                speed = 320 + SpeedC1;
//                speed22 = 320 + SpeedC2;
//            }
//            else if (Id == 2)
//            {
//                HP = 8000;
//                maxHP = HP;
//                maxHP2 = maxHP;
//                speed22 = 340 + SpeedC2;
//                speed = 340 + SpeedC1;
//            }
//            else if (Id == 3)
//            {
//                HP = 64000;
//                maxHP = HP;
//                maxHP2 = maxHP;
//                speed = 360 + SpeedC1;
//                speed22 = 360 + SpeedC2;
//                damage += damage / 10;
//                dmgbooster += 15;
//                shield += shield / 20;
//                shieldbooster += 30;
//            }
//            else if (Id == 5)
//            {
//                HP = 16000;
//                maxHP = HP;
//                maxHP2 = maxHP;
//                speed = 300 + SpeedC1;
//                speed22 = 300 + SpeedC2;
//            }
//            else if (Id == 4)
//            {
//                HP = 12000;
//                maxHP2 = HP;
//                maxHP = HP;
//                speed = 280 + SpeedC1;
//                speed22 = 280 + SpeedC2;
//            }
//            else if (Id == 6)
//            {
//                HP = 64000;
//                maxHP2 = HP;
//                maxHP = HP;
//                speed = 360 + SpeedC1;
//                speed22 = 360 + SpeedC2;
//            }
//            else if (Id == 7)
//            {
//                HP = 120000;
//                maxHP2 = HP;
//                maxHP = HP;
//                speed22 = 340 + SpeedC2;
//                speed = 340 + SpeedC1;
//            }
//            else if (Id == 9)
//            {
//                HP = 160000;
//                maxHP2 = HP;
//                maxHP = HP;
//                speed = 260 + SpeedC1;
//                speed22 = 260 + SpeedC2;
//            }
//            else if (Id == 98 && rank == 21)
//            {
//                HP = 25000000;
//                maxHP2 = HP;
//                maxHP = HP;
//                speed = 500 + SpeedC1;
//                speed22 = 500 + SpeedC1;
//            }

//            else
//            {
//                HP = 5;
//                maxHP2 = HP;
//                maxHP = HP;
//                speed = 10;
//                speed22 = 10;
//            }
//            drones = DronesC1;
//            drones1 = DronesC1;
//            drones2 = DronesC2;
//            if (drones2 == 2)
//            {
//                maxHP2 = HP + HP / 5;
//                //    this.hpbooster += 10;
//                shieldbooster += 10;
//            }
//            if (drones == 2)
//            {
//                HP += HP / 5;
//                maxHP = HP;
//                hpbooster += 10;
//                shieldbooster += 10;
//            }

//            //< ship comment = "Skill Design 1 - Solace" type = "63" 
//            //< ship comment = "Skill Design 2 - Diminsher" type = "64"
//            //< ship comment = "Skill Design 3 - Spectrum" type = "65" 
//            //< ship comment = "Skill Design 4 - Sentinel" type = "66"
//            //< ship comment = "Skill Design 5 - Venom" type = "67"
//            if (Id == 56 || Id == 64 || Id == 67 || Id == 58 || Id == 18)
//            //enforcer - diminisher - venom - revenge - lightning
//            {
//                damage += damage / 2;
//                damage22 += damage22 / 2;
//                dmgbooster += 15;
//            }
//            if (Id == 59 || Id == 63 || Id == 65 || Id == 66 || Id == 86 || Id == 60)
//            //enforcer - diminisher - venom - revenge - lightning
//            {
//                shield = shield + (shield / 3);
//                shield22 = shield2 + (shield2 / 3);
//                maxShield = maxShield + (maxShield / 3);
//                maxShield22 = maxShield2 + (maxShield2 / 3);
//                //shield += shield / 3;
//                //maxShield += maxShield / 3;
//                //maxShield22 += maxShield2 / 3;
//                //shield22 += shield2 / 3;
//                shieldbooster += 30;
//            }
//            if (Id == 109)
//            //saturn
//            {
//                HP += HP / 5;
//                maxHP += maxHP / 5;
//                maxHP2 += maxHP2 / 5;
//                hpbooster += 20;
//            }
//            if (Id == 110)
//            //centaur
//            {
//                HP += HP / 10;
//                maxHP += maxHP / 10;
//                maxHP2 += maxHP2 / 10;

//                hpbooster += 10;
//            }
//            if (rank == 21)
//            //admin
//            {
//                HP += HP / 25;
//                maxHP += maxHP / 5;
//                maxHP2 += maxHP2 / 5;
//                hpbooster += 50;
//                shieldbooster += 50;
//                dmgbooster += 50;
//                cloaks = 999;


//            }

//            damage2 = damage;
//            damage1 = damage;
//            shield2 = shield;
//            maxShield2 = maxShield;
//            speed2 = speed22;
//            shield1 = shield;
//            shield2 = shield22;
//            maxShield1 = maxShield;
//            maxShield2 = maxShield22;
//            speed1 = speed;
//            speed2 = speed22;
//            damage1 = damage;
//            damage2 = damage22;
//            maxHP1 = maxHP;

//            if (premium == 1)
//            {
//                shield += shield / 4;
//                shield2 += shield2 / 4;
//                shield22 += shield22 / 4;
//                maxShield += shield / 4;
//                maxShield2 += shield2 / 4;
//                maxShield22 += shield22 / 4;
//                damage1 += damage1 / 10;
//                damage2 += damage2 / 10;
//                damage += damage / 10;
//                damage22 += damage22 / 10;
//                maxHP1 += maxHP1 / 10;
//                maxHP += maxHP / 10;
//                HP += HP / 10;
//                maxHP2 += HP / 10;
//                dmgbooster = 10;
//                shieldbooster = 25;
//                hpbooster = 10;
//                cloaks = 999;
//            }
//            HP = maxHP;
//            shield = maxShield;

//        }
//        public Ship(uint ID, string Name, ushort factionId, int x, int y, uint rank, uint clanID, string tagName, int exp, int honor, int level, int uri, int credits, uint prem, int res = 2)
//        {
//            resolution = res;
//            RangeX = "";
//            RangeY = "";
//            this.rank = rank;
//            premium = prem;
//            shieldAbsorb = 92;
//            this.exp = exp;
//            Id = ID;
//            this.level = level;
//            this.Name = Name.Replace('|', 'I');
//            this.x = x;
//            this.honor = honor;
//            this.y = y;
//            DronePacket = "";
//            UCB100 = 0;
//            x1 = 0;
//            x2 = 0;
//            x3 = 0;
//            rsb = 0;
//            sab = 0;
//            BRBA = 0;
//            SBUA = 0;
//            RPMA = 0;
//            ELAA = 0;
//            ECIA = 0;
//            this.factionId = factionId;
//            Radiation = false;
//            this.clanID = clanID;
//            this.tagName = tagName.Replace('|', 'I');
//            this.credits = credits;
//            this.uri = uri;
//            CanUseBRB = true;
//            CanUseAbility = true;
//            CanUseRockets = true;
//            Spectrum = false;
//            CanLoadHellstorm = true;
//            Diminisher = false;
//            ShieldLeech = false;
//            Venom = false;
//            Fortress = false;
//            Singularity = false;
//            isMoving = false;
//            repdelay = 5000;
//            CanUseSBU = true;
//            CanUseEMP = true;
//            CanBeLocked = true;
//            delayedrep = false;
//            repactivate = false;
//            shdactivate = false;
//            CanUseELA = true;
//            CanPutMine = true;
//            ELA = false;
//            CanUseISH = true;
//            realattack = false;
//            CanUseSMB = true;
//            CanUseRSB = true;
//            checkrep = true;
//            CanRep = true;
//            ISH = false;
//            INV = false;
//            CanSwitchConfig = true;
//            PetActivated = false;
//            R310 = 0;
//            PLT2026 = 0;
//            PLT2021 = 0;
//            PLT3030 = 0;
//            ACM01 = 0;
//            EMPM01 = 0;
//            SABM01 = 0;
//            DDM01 = 0;
//            ISHA = 0;
//            SMBA = 0;
//            EMPA = 0;
//            PLD8 = 0;
//            DCR250 = 0;
//            WIZ = 0;
//            ECO10 = 0;
//            UBR100 = 0;
//            MovingTask = 0;
//            HSTRM01 = 0;
//            cloaked = 0;
//            cloaks = 5;
//            dead = 0;
//            dmgbooster = 0;
//            shieldbooster = 0;
//            hpbooster = 0;
//        }
//        public ushort GetFactionID()
//        {
//            return factionId;
//        }
//        public int GetPosX()
//        {
//            return x;
//        }
//        public int GetPosY()
//        {
//            return y;
//        }
//        public uint GetShipMaxHP()
//        {
//            return maxHP;
//        }
//        public uint GetShipHP()
//        {
//            return HP;
//        }

//    }
//}
