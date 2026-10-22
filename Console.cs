using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Do
{
    class Console
    {
        public static ConsoleColor defColor = ConsoleColor.Green;

        public static void WriteLine(string message, bool pad = true)
        {
            ForegroundColor(defColor);
            write(message, 5);
            if (pad)
                System.Console.WriteLine(" " + message); //padding :P
            else
                System.Console.WriteLine(message);
        }

        public static void Write(string message)
        {
            ForegroundColor(defColor);
            System.Console.WriteLine(message);
            write(message, 5);
        }

        public static void WriteError(string message, string error = "", bool pad = true)
        {
            ForegroundColor(ConsoleColor.Red);
            writetoerr(message + "\n" + error);
            if (pad)
            {
                System.Console.WriteLine(" " + message); //padding :P
                System.Console.WriteLine(" " + error);
            }
            else
            {
                System.Console.WriteLine(message);
                System.Console.WriteLine(error);
            }
        }

        public static void WriteWarning(string message, bool pad = true)
        {
            ForegroundColor(ConsoleColor.Yellow);
            write(message, 5);
            if (pad)
                System.Console.WriteLine(" " + message); //padding :P
            else
                System.Console.WriteLine(message);
        }

        public static void WritePacket(string packet)
        {
            ForegroundColor(ConsoleColor.White);
            System.Console.Write(packet);
            writetopacket(packet);
        }

        public static void ForegroundColor(ConsoleColor color)
        {
            System.Console.ForegroundColor = color;
        }

        public static void BackgroundColor(ConsoleColor color)
        {
            System.Console.BackgroundColor = color;
        }

        #region Log system from my first emulator based in azure
        public static void write(string content, int to, string alienid = null, ulong exp = 0, ulong hon = 0, ulong cred = 0, int uri = 0)
        {
            if (Program.Settings["logs"] == "false")
                return;
            using (StreamWriter w = File.AppendText("logs/[" + System.DateTime.Now.ToString("dd-MM-yyyy") + "] Log.log"))
            {
                switch (to)
                {
                    case 1: UserLogged(content, w); break;
                    case 2: break;
                    case 3: UserKilled(content, w); break;
                    case 4: AlienKilled(content, alienid, exp, hon, cred, uri, w); break;
                    case 5: Log(content, w); break;
                }
            }
        }

        public static void writetopacket(string content)
        {
            using (StreamWriter w = File.AppendText("logs/[" + System.DateTime.Now.ToString("dd-MM-yyyy") + "] Packets.log"))
            {
                LogPacket(content, w);
            }
        }

        public static void writetoerr(string content)
        {
            using (StreamWriter w = File.AppendText("logs/[" + System.DateTime.Now.ToString("dd-MM-yyyy") + "] Errors.log"))
            {
                LogError(content, w);
            }
        }

        public static void UserLogged(string id, TextWriter w)
        {

            w.Write("\r[{0}] User ID {1} logged in the server", DateTime.Now.ToLongTimeString(), id);
        }

        public static void LogPacket(string packet, TextWriter w)
        {

            w.Write("\r[{0}] Packet: {1}", DateTime.Now.ToLongTimeString(), packet);
        }

        public static void UserKilled(string id, TextWriter w)
        {

            w.Write("\r[{0}] User ID {1} has been destroyed", DateTime.Now.ToLongTimeString(), id);
        }

        public static void AlienKilled(string id, string alienID, ulong exp, ulong hon, ulong cred, int uri, TextWriter w)
        {
            w.Write("\r[{0}] User ID {1} destroyed Alien ID {2}\n User ID {1} recieved:\t\n" + exp + " Experience points.\t\n" + hon + " Honor points.\t\n" + cred + " Credits.\t\n" + uri + " Uridium", DateTime.Now.ToLongTimeString(), id, alienID);
        }

        public static void Log(string log, TextWriter w)
        {
            w.WriteLine("\r[{0}] " + log, DateTime.Now.ToLongTimeString());
        }

        public static void LogError(string err, TextWriter w)
        {
            w.Write("\r[{0}] " + err, System.DateTime.Now.ToString("dd-MM-yyyy"));
        }
        #endregion
    }
}
