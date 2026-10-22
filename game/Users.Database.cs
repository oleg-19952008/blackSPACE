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
 * Partial-class User: Фоновая синхронизация с БД (SyncWithDatabase) и загрузка конфигов (LoadConfigs).
 */
partial class User
{
        private DateTime _lastDbSyncCheck = DateTime.MinValue;
        private string _dbInventoryHash = null;      // снимок строки инвентаря (lootid:Q)
        private string _dbEquipmentHash = null;      // снимок снаряжения (конфиги + дроиды + нано)
        private int _dbUri = int.MinValue, _dbCredits = int.MinValue;
        private int _dbExp = int.MinValue, _dbHonor = int.MinValue, _dbLevel = int.MinValue;
        private const int DbSyncIntervalMs = 30000;

        public static int changed_ = 0;

        private static string Md5Hex(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private void SyncWithDatabase()
        {
            // Вызывается из UpdateShip_ (фоновый поток игрока). Не чаще раза в 30 секунд.
            var now = DateTime.Now;
            if ((now - _lastDbSyncCheck).TotalMilliseconds < DbSyncIntervalMs) return;
            _lastDbSyncCheck = now;

            try
            {
                using (var dbClient = Program.Manager.getQueryreactor())
                {
                    bool inventoryChanged = false;
                    bool equipmentChanged = false;
                    bool profileChanged = false;

                    // ---- 1. Инвентарь: боеприпасы (ammunition_%) ----
                    // Сравниваем хеш ВСЕХ строк боеприпасов: покупка в магазине меняет Q,
                    // а новая покупка добавляет строку — любое изменение ловится сразу.
                    var res = (DataTable)dbClient.query("SELECT lootid,Q FROM server_1_player_all_items WHERE server_1_player_all_items.playerID=" + UserId + " AND server_1_player_all_items.lootid LIKE 'ammunition_%'");
                    var parts = new List<string>();
                    foreach (DataRow row in res.Rows)
                        parts.Add(row["lootid"].ToString() + ":" + row["Q"].ToString());
                    parts.Sort(StringComparer.Ordinal);
                    var invHash = Md5Hex(string.Join(",", parts.ToArray()));
                    if (_dbInventoryHash == null)
                    {
                        // Первый пропуск после логина: LoadConfigs()/вход уже загрузили
                        // актуальные значения — просто запоминаем снимок, ничего не трогаем.
                        _dbInventoryHash = invHash;
                    }
                    else if (_dbInventoryHash != invHash)
                    {
                        _dbInventoryHash = invHash;
                        inventoryChanged = true;
                    }

                    // ---- 2. Снаряжение: ангарные конфиги, дроиды, привязки, нанотех ----
                    var eqParts = new List<string>();
                    foreach (uint cfgNum in new uint[] { 1, 2 })
                    {
                        var eq = (DataTable)dbClient.query("SELECT lasers,generators,heavy_guns,extras FROM server_1_hangar_config_ship WHERE playerID=" + UserId + " AND configNum=" + cfgNum);
                        foreach (DataRow row in eq.Rows)
                            eqParts.Add("cfg" + cfgNum + "=" + row["lasers"] + "/" + row["generators"] + "/" + row["heavy_guns"] + "/" + row["extras"]);
                    }
                    var dr = (DataTable)dbClient.query("SELECT drone_kind,drone_level FROM server_1_player_drones WHERE playerID=" + UserId + " ORDER BY id");
                    foreach (DataRow row in dr.Rows)
                        eqParts.Add("d=" + row["drone_kind"] + ":" + row["drone_level"]);
                    var cd = (DataTable)dbClient.query("SELECT * FROM server_1_hangar_config_drones WHERE playerID=" + UserId + " ORDER BY configNum");
                    foreach (DataRow row in cd.Rows)
                    {
                        var rp = new List<string>();
                        foreach (DataColumn c in cd.Columns) rp.Add(Convert.ToString(row[c]));
                        eqParts.Add("cd=" + string.Join(":", rp.ToArray()));
                    }
                    var nt = (DataTable)dbClient.query("SELECT slot_one,slot_two,slot_three FROM server_1_player_nanotech WHERE playerId=" + UserId);
                    foreach (DataRow row in nt.Rows)
                        eqParts.Add("nt=" + row["slot_one"] + "/" + row["slot_two"] + "/" + row["slot_three"]);
                    eqParts.Sort(StringComparer.Ordinal);
                    var eqHash = Md5Hex(string.Join("|", eqParts.ToArray()));
                    if (_dbEquipmentHash == null)
                    {
                        _dbEquipmentHash = eqHash;
                    }
                    else if (_dbEquipmentHash != eqHash)
                    {
                        _dbEquipmentHash = eqHash;
                        equipmentChanged = true;
                    }

                    // ---- 3. Профиль: ресурсы/опыт/уровень (БД — источник истины) ----
                    var pdata = (DataTable)dbClient.query("SELECT uri,credits,exp,honor,level FROM server_1_players WHERE playerID=" + UserId);
                    if (pdata.Rows.Count == 1)
                    {
                        var r0 = pdata.Rows[0];
                        int dbUri = Convert.ToInt32(r0["uri"]);
                        int dbCredits = Convert.ToInt32(r0["credits"]);
                        int dbExp = Convert.ToInt32(r0["exp"]);
                        int dbHonor = Convert.ToInt32(r0["honor"]);
                        int dbLevel = Convert.ToInt32(r0["level"]);
                        if (dbUri != _dbUri || dbCredits != _dbCredits ||
                            dbExp != _dbExp || dbHonor != _dbHonor ||
                            dbLevel != _dbLevel)
                        {
                            profileChanged = true;
                            _dbUri = dbUri; _dbCredits = dbCredits;
                            _dbExp = dbExp; _dbHonor = dbHonor;
                            _dbLevel = dbLevel;
                            Ship.uri = dbUri;
                            Ship.credits = dbCredits;
                            Ship.exp = dbExp;
                            Ship.honor = dbHonor;
                            Ship.level = dbLevel;
                        }
                    }

                    // ---- 4. Применение изменений ----
                    if (equipmentChanged || inventoryChanged)
                    {
                        // Полная перезагрузка снаряжения/дроидов/боеприпасов из БД — тот же
                        // путь, что при входе (пушки, щиты, процессоры, хайтеки, дроиды).
                        LoadConfigs();
                    }

                    if (inventoryChanged || equipmentChanged || profileChanged)
                    {
                        // Рассылка клиенту тех же пакетов, что при входе (SendUserShit):
                        // UpdateShip() шлёт профиль HwkU|I (ресурсы/характеристики корабля).
                        UpdateShip();
                        // Боеприпасы: лазеры (0|B|), ракеты/шахты/мин (0|RL|R|, 0|3|)
                        Send("0|B|" + Ship.x1 + "|" + Ship.x2 + "|" + Ship.x3 + "|" + Ship.UCB100 + "|" + Ship.sab + "|" + Ship.rsb);
                        Send("0|RL|R|" + Ship.HSTRM01 + "|" + Ship.UBR100 + "|" + Ship.ECO10);
                        SendSpAmmo();
                        // Слоты REPCPU/ISICPU/AROCPU (реботы и процессоры из ангара)
                        Send("0|A|ITM|0|0|0|0|" + Ship.RepBot + "|0|0|" + Program.ToEnum(Ship.SMBCPU) + "|" + Program.ToEnum(Ship.ISHCPU) + "|0|0|" + Program.ToEnum(Ship.AROCKET) + "|0|0|0|0|0");
                        // Нанотехнологии (ELAA/ECIA/RPMA/SBUA/BRBA)
                        Send("0|TX|S|" + ((Ship.ELAA != 0) ? "1" : "0") + "|" + Ship.ELAA + "|0|" + ((Ship.ECIA != 0) ? "1" : "0") + "|" + Ship.ECIA + "|0|" + ((Ship.RPMA != 0) ? "1" : "0") + "|" + Ship.RPMA + "|0|" + ((Ship.SBUA != 0) ? "1" : "0") + "|" + Ship.SBUA + "|0|" + ((Ship.BRBA != 0) ? "1" : "0") + "|" + Ship.BRBA + "|0");
                        // Дройды (например купленный zeus): этому игроку и всем в радиусе.
                        Send("0|n|d|" + UserId + "|" + Ship.DronePacket);
                        try
                        {
                            foreach (var pair in Program.Maps[MapId].Users)
                            {
                                if (pair.Key == UserId) continue;
                                try { pair.Value.Send("0|n|d|" + UserId + "|" + Ship.DronePacket); }
                                catch { }
                            }
                        }
                        catch { }

                        Out.WriteLine("DB sync: changes detected for user " + UserId +
                                      " (inventory=" + inventoryChanged + ", equipment=" + equipmentChanged +
                                      ", profile=" + profileChanged + ")", "User.DbSync", ConsoleColor.Cyan);
                    }
                }
            }
            catch (Exception e)
            {
                // Фоновая синхронизация не должна ронять цикл UpdateShip_.
                Out.WriteLine("SyncWithDatabase failed for user " + UserId + ": " + e.Message, "User.DbSync", ConsoleColor.DarkYellow);
            }
        }

        public void LoadConfigs()
        {
            try
            {
                Ship.DronePacket = Program.LoadDrones(UserId);
                var eq1 = Program.LoadEq(UserId, 1);
                var eq2 = Program.LoadEq(UserId, 2);

                uint DamageC1 = 0, ShieldC1 = 0, SpeedC1 = 0, DronesC1 = 0, DamageC2 = 0, ShieldC2 = 0, SpeedC2 = 0, DronesC2 = 0;
                var C1E = "";
                var C2E = "";
                string[] C1Lasers, C2Lasers, C1Gens, C2Gens, C1Ex, C2Ex;
                if (eq1["lasers"].ToString() != "")
                {
                    var C1L = eq1["lasers"].ToString();
                    if (C1L.Contains('|'))
                    {
                        C1Lasers = C1L.Split('|');

                        foreach (var it in from item in C1Lasers where item != "" select Program.LoadItem(UserId, item).Split('|') into it where it[0] == "lsr" select it)
                        {

                            DamageC1 += Convert.ToUInt32(it[1]);
                            Ship.lasers1++;
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, C1L).Split('|');
                        if (it[0] == "lsr")
                        {
                            DamageC1 += Convert.ToUInt32(it[1]);
                            Ship.lasers1++;
                        }
                    }
                }
                if (eq1["generators"].ToString() != "")
                {
                    var C1G = eq1["generators"].ToString();
                    if (C1G.Contains('|'))
                    {
                        C1Gens = C1G.Split('|');

                        foreach (var it in from item in C1Gens where item != "" select Program.LoadItem(UserId, item).Split('|'))
                        {
                            if (it[0] == "spd")
                                SpeedC1 += Convert.ToUInt32(it[1]);
                            if (it[0] == "shd")
                                ShieldC1 += Convert.ToUInt32(it[1]);
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, C1G).Split('|');
                        if (it[0] == "spd")
                            SpeedC1 += Convert.ToUInt32(it[1]);
                        if (it[0] == "shd")
                            ShieldC1 += Convert.ToUInt32(it[1]);
                    }
                }
                if (eq2["lasers"].ToString() != "")
                {
                    var C2L = eq2["lasers"].ToString();
                    if (C2L.Contains('|'))
                    {
                        C2Lasers = C2L.Split('|');
                        foreach (var it in from item in C2Lasers where item != "" select Program.LoadItem(UserId, item).Split('|') into it where it[0] == "lsr" select it)
                        {
                            DamageC2 += Convert.ToUInt32(it[1]);
                            Ship.lasers2++;
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, C2L).Split('|');
                        if (it[0] == "lsr")
                        {
                            DamageC2 += Convert.ToUInt32(it[1]);
                            Ship.lasers2++;
                        }
                    }
                }
                if (eq2["generators"].ToString() != "")
                {
                    var c2G = eq2["generators"].ToString();

                    if (c2G.Contains('|'))
                    {
                        C2Gens = c2G.Split('|');
                        foreach (var it in from item in C2Gens where item != "" select Program.LoadItem(UserId, item).Split('|'))
                        {
                            if (it[0] == "spd")
                                SpeedC2 += Convert.ToUInt32(it[1]);
                            else if (it[0] == "shd")
                                ShieldC2 += Convert.ToUInt32(it[1]);
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, c2G).Split('|');
                        if (it[0] == "spd")
                            SpeedC1 += Convert.ToUInt32(it[1]);
                        if (it[0] == "shd")
                            ShieldC1 += Convert.ToUInt32(it[1]);
                    }
                }
                if (eq2["heavy_guns"].ToString() != "")
                {
                    var it = Program.LoadItem(UserId, eq2["heavy_guns"].ToString()).Split('|');
                    if (it[0] == "equipment_weapon_rocketlauncher_hst-2")
                    {
                        Ship.HSTRM2 = 2;
                    }
                    else if (it[0] == "equipment_weapon_rocketlauncher_hst-1")
                    {
                        Ship.HSTRM2 = 1;
                    }
                }
                if (eq1["heavy_guns"].ToString() != "")
                {
                    var it = Program.LoadItem(UserId, eq1["heavy_guns"].ToString()).Split('|');
                    if (it[0] == "equipment_weapon_rocketlauncher_hst-2")
                    {
                        Ship.HSTRM = 2;
                        Ship.HSTRM1 = 2;
                    }
                    else if (it[0] == "equipment_weapon_rocketlauncher_hst-1")
                    {
                        Ship.HSTRM = 1;
                        Ship.HSTRM1 = 1;
                    }
                }
                if (eq2["extras"].ToString() != "")
                {
                    C2E = eq2["extras"].ToString();
                    if (C2E.Contains('|'))
                    {
                        C2Ex = C2E.Split('|');
                        foreach (var it in from item in C2Ex where item != "" select Program.LoadItem(UserId, item).Split('|'))
                        {
                            if (it[0] == "equipment_extra_repbot_rep-1")
                            {
                                Ship.RepBot2 = 1;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-2")
                            {
                                Ship.RepBot2 = 2;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-3")
                            {
                                Ship.RepBot2 = 3;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-4")
                            {
                                Ship.RepBot2 = 4;
                            }
                            else if (it[0] == "equipment_extra_cpu_ish-01")
                            {
                                Ship.ISHCPU2 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_smb-01")
                            {
                                Ship.SMBCPU2 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_arol-x")
                            {
                                Ship.AROCKET2 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_nc-rrb-x")
                            {
                                Ship.ARepBot2 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_rllb-x")
                            {
                                Ship.AHSTRM2 = true;
                            }
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, C2E).Split('|');
                        if (it[0] == "equipment_extra_repbot_rep-1")
                        {
                            Ship.RepBot2 = 1;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-2")
                        {
                            Ship.RepBot2 = 2;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-3")
                        {
                            Ship.RepBot2 = 3;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-4")
                        {
                            Ship.RepBot2 = 4;
                        }
                        else if (it[0] == "equipment_extra_cpu_ish-01")
                        {
                            Ship.ISHCPU2 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_smb-01")
                        {
                            Ship.SMBCPU2 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_arol-x")
                        {
                            Ship.AROCKET2 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_nc-rrb-x")
                        {
                            Ship.ARepBot2 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_rllb-x")
                        {
                            Ship.AHSTRM2 = true;
                        }

                    }
                }
                if (eq1["extras"].ToString() != "")
                {
                    C1E = eq1["extras"].ToString();
                    if (C1E.Contains('|'))
                    {
                        C1Ex = C1E.Split('|');
                        foreach (var it in from item in C1Ex where item != "" select Program.LoadItem(UserId, item).Split('|'))
                        {
                            if (it[0] == "equipment_extra_repbot_rep-1")
                            {
                                Ship.RepBot1 = 1;
                                Ship.RepBot = 1;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-2")
                            {
                                Ship.RepBot1 = 2;
                                Ship.RepBot = 2;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-3")
                            {
                                Ship.RepBot1 = 3;
                                Ship.RepBot = 3;
                            }
                            else if (it[0] == "equipment_extra_repbot_rep-4")
                            {
                                Ship.RepBot1 = 4;
                                Ship.RepBot = 4;
                            }
                            else if (it[0] == "equipment_extra_cpu_ish-01")
                            {
                                Ship.ISHCPU = true;
                                Ship.ISHCPU1 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_smb-01")
                            {
                                Ship.SMBCPU = true;
                                Ship.SMBCPU1 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_arol-x")
                            {
                                Ship.AROCKET = true;
                                Ship.AROCKET1 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_nc-rrb-x")
                            {
                                Ship.ARepBot = true;
                                Ship.ARepBot1 = true;
                            }
                            else if (it[0] == "equipment_extra_cpu_rllb-x")
                            {
                                Ship.AHSTRM = true;
                                Ship.AHSTRM1 = true;
                            }
                        }
                    }
                    else
                    {
                        var it = Program.LoadItem(UserId, C1E).Split('|');
                        if (it[0] == "equipment_extra_repbot_rep-1")
                        {
                            Ship.RepBot1 = 1;
                            Ship.RepBot = 1;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-2")
                        {
                            Ship.RepBot1 = 2;
                            Ship.RepBot = 2;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-3")
                        {
                            Ship.RepBot1 = 3;
                            Ship.RepBot = 3;
                        }
                        else if (it[0] == "equipment_extra_repbot_rep-4")
                        {
                            Ship.RepBot1 = 4;
                            Ship.RepBot = 4;
                        }
                        else if (it[0] == "equipment_extra_cpu_ish-01")
                        {
                            Ship.ISHCPU = true;
                            Ship.ISHCPU1 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_smb-01")
                        {
                            Ship.SMBCPU = true;
                            Ship.SMBCPU1 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_arol-x")
                        {
                            Ship.AROCKET = true;
                            Ship.AROCKET1 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_nc-rrb-x")
                        {
                            Ship.ARepBot = true;
                            Ship.ARepBot1 = true;
                        }
                        else if (it[0] == "equipment_extra_cpu_rllb-x")
                        {
                            Ship.AHSTRM = true;
                            Ship.AHSTRM1 = true;
                        }
                        //else if (it[0] == "equipment_extra_cpu_nc-rrb-x")
                        //{
                        //    Ship.ARepBot = true;
                        //    Ship.ARepBot1 = true;
                        //}
                    }

                }
                var A = Program.LoadDroneEq(UserId).Split('|');
                DamageC1 += Convert.ToUInt32(A[0]);
                ShieldC1 += Convert.ToUInt32(A[1]);
                DamageC2 += Convert.ToUInt32(A[2]);
                ShieldC2 += Convert.ToUInt32(A[3]);
                Ship.lasers = Ship.lasers1;

                using (var dbClient = Program.Manager.getQueryreactor())
                {
                    var res = (DataTable)dbClient.query("SELECT lootid,Q FROM server_1_player_all_items WHERE server_1_player_all_items.playerID=" + UserId + " AND server_1_player_all_items.lootid LIKE 'ammunition_%'");
                    foreach (DataRow row in res.Rows)
                    {
                        if (row["lootid"].ToString() == "ammunition_rocket_plt-2021")
                            Ship.PLT2021 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocket_plt-3030")
                            Ship.PLT3030 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocket_plt-2026")
                            Ship.PLT2026 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocket_r-310")
                            Ship.R310 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_specialammo_emp-01")
                            Ship.EMPA = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocketlauncher_eco-10")
                            Ship.ECO10 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocketlauncher_ubr-100")
                            Ship.UBR100 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_rocketlauncher_hstrm-01")
                            Ship.HSTRM01 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_lcb-10")
                            Ship.x1 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_mcb-25")
                            Ship.x2 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_mcb-50")
                            Ship.x3 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_ucb-100")
                            Ship.UCB100 = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_rsb-75")
                            Ship.rsb = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_laser_sab-50")
                            Ship.sab = int.Parse(row["Q"].ToString());
                        if (row["lootid"].ToString() == "ammunition_mine_acm-01")
                            Ship.ACM01 = int.Parse(row["Q"].ToString());
                    }
                    var slotone = "none";
                    var slottwo = "none";
                    var slotthree = "none";
                    res = (DataTable)dbClient.query("SELECT slot_one,slot_three,slot_two FROM server_1_player_nanotech WHERE playerId = '" + UserId + "'");
                    slotone = res.Rows[0]["slot_one"].ToString();
                    slottwo = res.Rows[0]["slot_two"].ToString();
                    slotthree = res.Rows[0]["slot_three"].ToString();

                    if (slotone != "none" && slotone != "locked")
                    {
                        var techone = slotone.Split('|');

                        if (techone[0] == "SRPM")
                            Ship.RPMA = Convert.ToInt32(techone[1]);
                        if (techone[0] == "SBRB")
                            Ship.BRBA = Convert.ToInt32(techone[1]);
                        if (techone[0] == "SSBU")
                            Ship.SBUA = Convert.ToInt32(techone[1]);
                        if (techone[0] == "SECI")
                            Ship.ECIA = Convert.ToInt32(techone[1]);
                        if (techone[0] == "SELA")
                            Ship.ELAA = Convert.ToInt32(techone[1]);
                    }
                    if (slottwo != "none" && slottwo != "locked")
                    {
                        var techtwo = slottwo.Split('|');

                        if (techtwo[0] == "SRPM")
                            Ship.RPMA = Convert.ToInt32(techtwo[1]);
                        if (techtwo[0] == "SBRB")
                            Ship.BRBA = Convert.ToInt32(techtwo[1]);
                        if (techtwo[0] == "SSBU")
                            Ship.SBUA = Convert.ToInt32(techtwo[1]);
                        if (techtwo[0] == "SECI")
                            Ship.ECIA = Convert.ToInt32(techtwo[1]);
                        if (techtwo[0] == "SELA")
                            Ship.ELAA = Convert.ToInt32(techtwo[1]);
                    }
                    if (slotthree != "none" && slotthree != "locked")
                    {
                        var techthree = slotthree.Split('|');

                        if (techthree[0] == "SRPM")
                            Ship.RPMA = Convert.ToInt32(techthree[1]);
                        if (techthree[0] == "SBRB")
                            Ship.BRBA = Convert.ToInt32(techthree[1]);
                        if (techthree[0] == "SSBU")
                            Ship.SBUA = Convert.ToInt32(techthree[1]);
                        if (techthree[0] == "SECI")
                            Ship.ECIA = Convert.ToInt32(techthree[1]);
                        if (techthree[0] == "SELA")
                            Ship.ELAA = Convert.ToInt32(techthree[1]);
                    }
                }
                if (Ship.ACM01 != 0)
                {
                    double Ammo = Ship.ACM01 / 10;
                    Ship.ISHA = (int)Math.Truncate(Ammo);
                    Ship.SMBA = (int)Math.Truncate(Ammo);
                }
                Ship.mapId = MapId;
                Ship.ParseConfigs(DamageC1, ShieldC1, SpeedC1, DronesC1, DamageC2, ShieldC2, SpeedC2, DronesC2);

            }
            catch { }
        }

}
}
