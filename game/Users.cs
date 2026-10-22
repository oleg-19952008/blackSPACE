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
 * Основной partial-class User.
 * Здесь объявлены КЛЮЧЕВЫЕ члены класса, которые должны существовать всегда
 * и быть видны всем остальным partial-файлам (Users.*.cs), а также внешним
 * сборкам/файлам (Assembly/AmmoManager.cs, Assembly/Pet.cs).
 * Остальные члены (поля/методы) находятся в других partial-файлах
 * (Users.State.cs, Users.Sending.cs, Users.Misc.cs, Users.PvE.cs и т.д.),
 * которые компилируются как часть этого же класса.
 */
    partial class User
    {
        Socket _handler;
        private readonly byte[] _buffer = new byte[2048];
        private bool _userAuth;

        // --- Ключевые свойства игрока (видимы во всех partial-файлах и внешних файлах) ---
        public usersClass.Ship Ship;              // корабль игрока (инициализируется в Users.Network.cs)

        // Остальные члены класса объявлены в других partial-файлах:
        //   UserId, MapId, GodMode -> Users.State.cs
        //   Send      -> Users.Sending.cs
        //   EmpEffect, SmbDamage -> Users.Misc.cs
        //   exp_      -> Users.PvE.cs
    }
}