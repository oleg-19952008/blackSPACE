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
 * Поля состояния и конструктор - в Users.State.cs, логика разнесена по файлам Users.*.cs.
 */
    class User
    {
        Socket _handler;
        private readonly byte[] _buffer = new byte[2048];
        private bool _userAuth;

    }
}