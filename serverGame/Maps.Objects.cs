using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Do.serverGame
{
    /// <summary>
    /// Объекты карты: NPC (Aliens), грузовые и бонус-ящики, мины и фейерверки.
    /// </summary>
    partial class Map
    {
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

        private void CreateBonusBox(int count)
        {
            uint created = 0;

            while (created < count)
            {
                ++created;
                BonusBox.Add(created, new Box(created, _id));
            }
        }

        public async void AddMine(int id, int x, int y, int type)
        {
            await Task.Delay(MineSpawnDelayMs);
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
            // Сначала собираем ключи удаляемых фейерверков, чтобы не модифицировать
            // словарь во время итерации (исходный код мог бросать InvalidOperationException).
            var toRemove = Fireworks.Where(firework => firework.Value[2] == userId)
                                    .Select(firework => firework.Key)
                                    .ToList();

            foreach (var hash in toRemove)
            {
                var packet = "0|n|FWI|" + hash;
                foreach (var user in Program.Maps[_id].Users)
                {
                    user.Value.Send(packet);
                }
                Fireworks.Remove(hash);
            }
        }
        #endregion

        private void SendMines(uint userId)
        {
            Parallel.ForEach(Mines, pair => Users[userId].Send("0|L|" + pair.Key + "|" + pair.Value[2] + "|" + pair.Value[0] + "|" + pair.Value[1]));
        }

        private void SendCargos(uint userId)
        {
            Parallel.ForEach(CargoBox, pair => Users[userId].Send("0|c|sb" + pair.Key + "|1|" + pair.Value[0] + "|" + pair.Value[1]));
        }
    }
}
