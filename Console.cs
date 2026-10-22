using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Do
{
    /// <summary>
    /// Класс вывода сообщений в консоль сервера и записи логов на диск.
    /// </summary>
    class Console
    {
        /// <summary>
        /// Цвет сообщений по умолчанию.
        /// </summary>
        public static ConsoleColor defColor = ConsoleColor.Green;

        /// <summary>
        /// Выводит сообщение в консоль с цветом по умолчанию и записывает его в общий лог.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="pad">Добавлять отступ (padding) перед сообщением.</param>
        public static void WriteLine(string message, bool pad = true)
        {
            ForegroundColor(defColor);
            write(message, 5);
            if (pad)
                System.Console.WriteLine(" " + message); //padding :P
            else
                System.Console.WriteLine(message);
        }

        /// <summary>
        /// Выводит сообщение в консоль и записывает его в файл общего лога.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        public static void Write(string message)
        {
            ForegroundColor(defColor);
            System.Console.WriteLine(message);
            write(message, 5);
        }

        /// <summary>
        /// Выводит сообщение об ошибке красным цветом и записывает его в файл ошибок.
        /// </summary>
        /// <param name="message">Описание ошибки.</param>
        /// <param name="error">Текст исключения или деталей ошибки.</param>
        /// <param name="pad">Добавлять отступ (padding) перед сообщением.</param>
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

        /// <summary>
        /// Выводит предупреждение жёлтым цветом и записывает его в общий лог.
        /// </summary>
        /// <param name="message">Текст предупреждения.</param>
        /// <param name="pad">Добавлять отступ (padding) перед сообщением.</param>
        public static void WriteWarning(string message, bool pad = true)
        {
            ForegroundColor(ConsoleColor.Yellow);
            write(message, 5);
            if (pad)
                System.Console.WriteLine(" " + message); //padding :P
            else
                System.Console.WriteLine(message);
        }

        /// <summary>
        /// Выводит содержимое сетевого пакета в консоль и записывает его в лог пакетов.
        /// </summary>
        /// <param name="packet">Текстовое представление пакета.</param>
        public static void WritePacket(string packet)
        {
            ForegroundColor(ConsoleColor.White);
            System.Console.Write(packet);
            writetopacket(packet);
        }

        /// <summary>
        /// Устанавливает цвет текста консоли.
        /// </summary>
        /// <param name="color">Новый цвет текста.</param>
        public static void ForegroundColor(ConsoleColor color)
        {
            System.Console.ForegroundColor = color;
        }

        /// <summary>
        /// Устанавливает цвет фона консоли.
        /// </summary>
        /// <param name="color">Новый цвет фона.</param>
        public static void BackgroundColor(ConsoleColor color)
        {
            System.Console.BackgroundColor = color;
        }

        #region Log system from my first emulator based in azure
        /// <summary>
        /// Записывает сообщение в суточный файл лога в зависимости от типа записи.
        /// </summary>
        /// <param name="content">Текст сообщения.</param>
        /// <param name="to">Тип записи: 1 — вход пользователя, 3 — гибель пользователя, 4 — убитый инопланетянин, 5 — обычное сообщение.</param>
        /// <param name="alienid">ID инопланетянина (для типа 4).</param>
        /// <param name="exp">Полученный опыт (для типа 4).</param>
        /// <param name="hon">Полученная честь (для типа 4).</param>
        /// <param name="cred">Полученные кредиты (для типа 4).</param>
        /// <param name="uri">Полученный уридий (для типа 4).</param>
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

        /// <summary>
        /// Записывает содержимое пакета в суточный файл лога пакетов.
        /// </summary>
        /// <param name="content">Текстовое представление пакета.</param>
        public static void writetopacket(string content)
        {
            using (StreamWriter w = File.AppendText("logs/[" + System.DateTime.Now.ToString("dd-MM-yyyy") + "] Packets.log"))
            {
                LogPacket(content, w);
            }
        }

        /// <summary>
        /// Записывает сообщение об ошибке в суточный файл лога ошибок.
        /// </summary>
        /// <param name="content">Текст ошибки.</param>
        public static void writetoerr(string content)
        {
            using (StreamWriter w = File.AppendText("logs/[" + System.DateTime.Now.ToString("dd-MM-yyyy") + "] Errors.log"))
            {
                LogError(content, w);
            }
        }

        /// <summary>
        /// Формирует запись о входе пользователя в лог.
        /// </summary>
        /// <param name="id">ID пользователя.</param>
        /// <param name="w">Поток записи лога.</param>
        public static void UserLogged(string id, TextWriter w)
        {

            w.Write("\r[{0}] User ID {1} logged in the server", DateTime.Now.ToLongTimeString(), id);
        }

        /// <summary>
        /// Формирует запись о полученном пакете в лог.
        /// </summary>
        /// <param name="packet">Текстовое представление пакета.</param>
        /// <param name="w">Поток записи лога.</param>
        public static void LogPacket(string packet, TextWriter w)
        {

            w.Write("\r[{0}] Packet: {1}", DateTime.Now.ToLongTimeString(), packet);
        }

        /// <summary>
        /// Формирует запись о гибели пользователя в лог.
        /// </summary>
        /// <param name="id">ID пользователя.</param>
        /// <param name="w">Поток записи лога.</param>
        public static void UserKilled(string id, TextWriter w)
        {

            w.Write("\r[{0}] User ID {1} has been destroyed", DateTime.Now.ToLongTimeString(), id);
        }

        /// <summary>
        /// Формирует запись об уничтоженном инопланетянине и полученной награде в лог.
        /// </summary>
        /// <param name="id">ID пользователя, уничтожившего цели.</param>
        /// <param name="alienID">ID уничтоженного инопланетянина.</param>
        /// <param name="exp">Полученный опыт.</param>
        /// <param name="hon">Полученная честь.</param>
        /// <param name="cred">Полученные кредиты.</param>
        /// <param name="uri">Полученный уридий.</param>
        /// <param name="w">Поток записи лога.</param>
        public static void AlienKilled(string id, string alienID, ulong exp, ulong hon, ulong cred, int uri, TextWriter w)
        {
            w.Write("\r[{0}] User ID {1} destroyed Alien ID {2}\n User ID {1} recieved:\t\n" + exp + " Experience points.\t\n" + hon + " Honor points.\t\n" + cred + " Credits.\t\n" + uri + " Uridium", DateTime.Now.ToLongTimeString(), id, alienID);
        }

        /// <summary>
        /// Формирует обычную текстовую запись в лог.
        /// </summary>
        /// <param name="log">Текст сообщения.</param>
        /// <param name="w">Поток записи лога.</param>
        public static void Log(string log, TextWriter w)
        {
            w.WriteLine("\r[{0}] " + log, DateTime.Now.ToLongTimeString());
        }

        /// <summary>
        /// Формирует запись об ошибке в лог.
        /// </summary>
        /// <param name="err">Текст ошибки.</param>
        /// <param name="w">Поток записи лога.</param>
        public static void LogError(string err, TextWriter w)
        {
            w.Write("\r[{0}] " + err, System.DateTime.Now.ToString("dd-MM-yyyy"));
        }
        #endregion
    }
}
