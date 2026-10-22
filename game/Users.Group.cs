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
 * Partial-class User: Группа: приглашения, лидер, следование, пинги; Wizz-ракета.
 */
partial class User
{
        public void Follow(uint FollowMember)
        {
            if (!GroupNames.Contains(FollowMember))
            {
                return;
            }
            if (MapId != Program.Users[FollowMember].MapId)
            {
                Send("0|A|STD|Player is not in this map.");
                return;
            }
            var x = Program.Users[FollowMember].Ship.x;
            var y = Program.Users[FollowMember].Ship.y;
            Ship.destinationx = x;
            Ship.destinationy = y;
            Ship.isMoving = true;
            var distanta = Math.Sqrt(Math.Pow(Ship.GetPosX() - x, 2) + Math.Pow(Ship.GetPosY() - y, 2));
            var Time = Convert.ToInt32(distanta / Ship.speed) * 1000;
            Ship.MovingTask = 0;
            Send("0|A|RS|-1");
            Send("0|A|RS|-1");
            Send("0|1|" + UserId + "|" + x + "|" + y + "|" + Time);
        }
        public void Ping(string[] type)
        {
            switch (type[2])
            {
                case "pos":
                    var x = type[3];
                    var y = type[4];
                    foreach (var Pair in GroupNames)
                    {
                        Program.Users[Pair].Send("0|ps|png|" + x + "|" + y);
                    }
                    break;
                case "usr":
                    var PngUserID = Convert.ToUInt32(type[3]);
                    if (!GroupNames.Contains(PngUserID))
                    {
                        return;
                    }
                    if (MapId != Program.Users[PngUserID].MapId)
                    {
                        Send("0|A|STD|Player is not in this map.");
                        return;
                    }

                    if (Program.Users.ContainsKey(PngUserID))
                    {
                        x = Program.Users[PngUserID].Ship.GetPosX().ToString();
                        y = Program.Users[PngUserID].Ship.GetPosY().ToString();
                        foreach (var Pair in GroupNames)
                        {
                            Program.Users[Pair].Send("0|ps|png|" + x + "|" + y);
                        }

                    }
                    break;
            }
        }
        public void AcceptInvitation(uint Id)
        {
            try
            {
                _isboss = false;
                Program.Users[Id].InvitationNames.Remove(UserId);
                Send("0|ps|inv|del|none|" + Id);
                Program.Users[Id].Send("0|ps|inv|del|none|" + Id + "|" + UserId);
                if (Program.Users[Id].GroupNames.Count == 0)
                {
                    Program.Users[Id].GroupNames.Add(Id);
                    Program.Users[Id].GroupNames.Add(UserId);
                    GroupNames.Add(UserId);
                    GroupNames.Add(Id);
                }
                else
                {
                    foreach (var Users in Program.Users[Id].GroupNames)
                    {
                        GroupNames.Add(Users);
                    }
                    foreach (var Users in GroupNames)
                    {
                        Program.Users[Users].GroupNames.Add(UserId);
                    }
                    GroupNames.Add(UserId);
                }
                // ?|?|?|caninvite|?|1ºname|?|1ºhp|1ºmaxhp|1ºshield|1ºmaxshield|1ºmapId|?|?|?|?|?|underattack|?|1ºselectedId|?|shipId
                foreach (var Users in Program.Users[Id].GroupNames)
                {
                    string CreateGroup = null;
                    if (Program.Users[Users]._isboss)
                    {
                        CreateGroup += Program.Users[Users].Ship.Name + "|" + Users + "|" + Program.Users[Users].Ship.HP + "|" + Program.Users[Users].Ship.maxHP + "|" + Program.Users[Users].Ship.shield + "|" + Program.Users[Users].Ship.maxShield + "|" + Program.Users[Users].MapId + "|0|0|0|0|" + Convert.ToUInt32(Program.Users[Users].Ship.cloaked) + "|0|0|" + Program.Users[Users].Ship.selectedUserId + "|0|" + Program.Users[Users].Ship.Id + "|0|1|";
                    }
                    foreach (var Pair in Program.Users[Id].GroupNames)
                    {
                        if (Pair != Users)
                        {
                            CreateGroup += Program.Users[Pair].Ship.Name + "|" + Pair + "|" + Program.Users[Pair].Ship.HP + "|" + Program.Users[Pair].Ship.maxHP + "|" + Program.Users[Pair].Ship.shield + "|" + Program.Users[Pair].Ship.maxShield + "|" + Program.Users[Pair].MapId + "|0|0|0|0|" + Convert.ToUInt32(Program.Users[Pair].Ship.cloaked) + "|0|0|" + Program.Users[Pair].Ship.selectedUserId + "|0|" + Program.Users[Pair].Ship.Id + "|0|1|";
                        }
                    }
                    Program.Users[Users].Send("0|ps|init|grp|1|1|1|1|1|" + CreateGroup);
                }
            }
            catch
            { }
        }
        public void RejectInvitation(uint Id)
        {
            Program.Users[Id].Send("0|ps|inv|del|rj|" + Id + "|" + UserId);
            Send("0|ps|inv|del|none|" + Id);
            Program.Users[Id].InvitationNames.Remove(UserId);

        }
        public void LeaveGroup()
        {
            try
            {
                Send("0|ps|lp|lv|" + UserId);
                _isboss = false;
                var Remove = new List<uint>();
                foreach (var Pair in GroupNames)
                {
                    Program.Users[Pair].Send("0|ps|lp|lv|" + UserId);
                    Remove.Add(Pair);
                }
                foreach (var Pair in Remove)
                {
                    Program.Users[Pair].GroupNames.Remove(UserId);
                }
                GroupNames.Clear();
            }
            catch
            { }
        }

        public void ChangeLeader(uint NewLeader)
        {
            if (!GroupNames.Contains(NewLeader))
            {
                return;
            }
            _isboss = false;
            Program.Users[NewLeader]._isboss = true;
        }

        public void InviteByName(string name)
        {
            try
            {
                var Id = Program.Users.FirstOrDefault(x => x.Value.Ship.Name == name).Key;
                if (name == Ship.Name || !Program.Users.ContainsKey(Id) || Program.Users[Id].GroupNames.Count != 0)
                    return;
                if (Program.Users[Id].Blk == 0 && !InvitationNames.Contains(Id))
                {
                    Program.Users[Id].Send("0|ps|inv|new|" + UserId + "|" + Ship.Name + "|" + Ship.Id + "|" + Id);
                    Send("0|ps|inv|new|" + UserId + "|" + name + "|" + Program.Users[Id].Ship.Id + "|" + Id + "|" + name + "|" + Program.Users[Id].Ship.Id);
                }
                else
                {
                    Send("0|ps|inv|blk");
                }
                if (GroupNames.Count == 0)
                {
                    _isboss = true;
                }
            }
            catch (Exception)
            { }
        }
        public void RemoveInvitation(uint Id)
        {
            try
            {
                Program.Users[Id].Send("0|ps|inv|del|rv|" + UserId);
                Send("0|ps|inv|del|rv|" + UserId + "|" + Id);
                InvitationNames.Remove(Id);
            }
            catch
            { }
        }
        public async void WizzRocket(uint _selecteduid)
        {
            var _1ship = Program.Users[_selecteduid].Ship.Id;
            var NewShip = new Random();
            var Newship1 = NewShip.Next(59, 69);
            Program.Users[_selecteduid].Ship.Id = Convert.ToUInt32(Newship1);
            Program.Users[_selecteduid].UpdateShip();
            //this.Ship.CanUseWizz = false;
            await Task.Delay(8000);
            Program.Users[_selecteduid].Ship.Id = _1ship;
            Program.Users[_selecteduid].UpdateShip();
            //this.Ship.CanUseWizz = true;
        }
        public async void UseWizz(int ms)
        {
            try
            {

                await Task.Delay(30000);
                //this.Ship.CanUseWizz = true;
            }
            catch (Exception)
            {
            }
        }
}
}
