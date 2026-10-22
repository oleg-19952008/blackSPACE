using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using MySQLManager.Database;
using MySQLManager.Database.Session_Details.Interfaces;

namespace Do.game.usersClass
{
   public class Clan
    {
        public int id;
        public Dictionary<uint, string[]> Diploma;

        public Clan(int id)
        {
            this.id = id;
        }
        public void AddDiplomacy()
        {
                this.Diploma = new Dictionary<uint, string[]>();
                using (var dbClient = Program.Manager.getQueryreactor())
                {
                    var r = (System.Data.DataTable)dbClient.query("SELECT clan2, pending, status FROM clan_diplomacy WHERE clan1=" + this.id);
                        foreach (DataRow row in r.Rows)
                        {
                            if (row["pending"].ToString() == "Accepted")
                            {
                                this.Diploma.Add(Convert.ToUInt32(row["clan2"]), (new string[] { row["status"].ToString() }));
                            }
                        }
                }
        }
    }
}
