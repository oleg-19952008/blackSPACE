using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Do.game;

namespace Do.serverGame
{
    /// <summary>
    /// Ядро карты: поля состояния, константы и конструктор.
    /// Остальная функциональность разнесена по partial-файлам:
    /// Maps.Users.cs    — вход/выход игроков с карты;
    /// Maps.Movement.cs — тик движения кораблей, видимость, мины, радиация/безопасные зоны;
    /// Maps.Portals.cs  — порталы и телепортация между картами;
    /// Maps.Objects.cs  — NPC, грузовые ящики, бонус-ящики, мины и фейерверки.
    /// </summary>
    partial class Map
    {
        // --- Идентификация карты ---
        public ushort _id { get; set; }
        public ushort Level { get; set; }
        public short _factionId { get; set; }
        public string _name { get; set; }
        public bool IsPvp { get; set; }
        public bool IsStarterMap = false;
        public int CubeCount = 0;

        // --- Коллекции объектов карты ---
        public Dictionary<uint, User> Users;
        public List<Portal> Portals;
        public Dictionary<uint, Alien> Aliens;
        public Dictionary<uint, string[]> CargoBox;
        public Dictionary<int, string[]> Mines;
        public Dictionary<string, string[]> Fireworks;
        public Dictionary<uint, Box> BonusBox;

        // --- Константы геймплея ---
        private const double ViewRange = 2000;              // радиус видимости (пиксели)
        private const double PortalUseRange = 850;          // радиус срабатывания портала
        private const double SecureAreaRange = 800;         // радиус безопасной зоны у портала
        private const double FactionSecureRadius = 1000;    // радиус стартовой безопасной зоны фракции
        private const double MineTriggerRange = 86;         // радиус срабатывания мины
        private const int MovementTickMs = 365;             // период тика движения корабля
        private const int TeleportDelayMs = 100;            // задержка после первого телепорта
        private const int BucleTeleportDelayMs = 371;       // задержка после второго телепорта
        private const int PortalAnimationDelayMs = 2800;    // длительность анимации перехода
        private const int MineSpawnDelayMs = 2000;          // задержка установки мины
        private const int BonusBoxCount = 65;               // количество бонус-ящиков на карте

        // Карты PvP-зоны (стартовые боевые 13-16)
        private static readonly HashSet<ushort> PvpMaps = new HashSet<ushort>
            { 13, 14, 15, 16 };

        // Граница мира по умолчанию: 20800 x 12800
        private const int DefaultWorldWidth = 20800;
        private const int DefaultWorldHeight = 12800;

        // Карты с обычной границей мира 20800x12800
        private static readonly HashSet<ushort> StandardBorderMaps = new HashSet<ushort>
            { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 123 };

        // Индивидуальные границы мира для отдельных карт: mapId -> (width, height)
        private static readonly Dictionary<ushort, int[]> CustomWorldBounds =
            new Dictionary<ushort, int[]>
            {
                { 16, new[] { 41600, 25600 } },
                { 255, new[] { 10400, 6400 } },
                { 29, new[] { 41600, 25600 } },
            };

        // Стартовые безопасные зоны фракций: factionId -> (mapId, centerX, centerY).
        // Совпадает с исходным набором условий factionId/mapId из _MoveShip.
        private static readonly Dictionary<short, Dictionary<ushort, int[]>> FactionSecureZones =
            new Dictionary<short, Dictionary<ushort, int[]>>
            {
                { 1, new Dictionary<ushort, int[]> { { 1, new[] { 1000, 1000 } } } },
                { 2, new Dictionary<ushort, int[]> { { 5, new[] { 19800, 1000 } } } },
                { 3, new Dictionary<ushort, int[]> { { 9, new[] { 19600, 11900 } } } },
            };

        /// <summary>
        /// Проверка выхода корабля за границы мира карты (используется для расчёта радиации).
        /// </summary>
        private bool IsOutsideWorldBounds(int x, int y)
        {
            int[] bounds;
            if (CustomWorldBounds.TryGetValue(_id, out bounds))
            {
                return x < 0 || y < 0 || x > bounds[0] || y > bounds[1];
            }
            if (StandardBorderMaps.Contains(_id))
            {
                return x < 0 || y < 0 || x > DefaultWorldWidth || y > DefaultWorldHeight;
            }
            return false;
        }

        /// <summary>
        /// Проверка попадания корабля в стартовую безопасную зону его фракции.
        /// </summary>
        private bool IsInFactionSecureZone(short factionId, int x, int y)
        {
            Dictionary<ushort, int[]> zones;
            if (!FactionSecureZones.TryGetValue(factionId, out zones)) return false;
            int[] center;
            if (!zones.TryGetValue(_id, out center)) return false;
            return Distance.Get(center[0], x, center[1], y) < FactionSecureRadius;
        }

        // --- Внутренние типы ---
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
            CreateBonusBox(BonusBoxCount);
            IsPvp = PvpMaps.Contains(id);
        }
    }
}
