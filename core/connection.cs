using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Do.Core
{
    /// <summary>
    /// Хранит состояние асинхронного приёма данных для одного сокета.
    /// </summary>
    public class StateObject
    {
        /// <summary>
        /// Рабочий сокет соединения.
        /// </summary>
        public Socket WorkSocket = null;
        /// <summary>
        /// Размер буфера приёма данных в байтах.
        /// </summary>
        public  const int BufferSize = 2048;
        /// <summary>
        /// Буфер для входящих байтов.
        /// </summary>
        public byte[] Buffer = new byte[BufferSize];
        /// <summary>
        /// Накопитель принятых данных в виде строки.
        /// </summary>
        public StringBuilder Sb = new StringBuilder();
    }
    


  /// <summary>
  /// Асинхронный слушатель TCP-сокетов: принимает входящие подключения клиентов игры
  /// и создаёт для каждого соединения объект пользователя.
  /// </summary>
  public  class AsynchronousSocketListener
    {
        // Thread signal.
        /// <summary>
        /// Событие для синхронизации потока прослушивания с моментом принятия соединения.
        /// </summary>
        public static ManualResetEvent AllDone = new ManualResetEvent(false);     
 
        /// <summary>
        /// Запускает прослушивание порта 8080, инициализирует игровой цикл (RUN)
        /// и в бесконечном цикле принимает входящие соединения.
        /// </summary>
        public static void StartListening()
        {
           
            var localEndPoint = new IPEndPoint(IPAddress.Any, 8080);
         
            var listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            new Thread(new Program().RUN).Start();

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(1000);
                while (true)
                {
                    // Set the event to nonsignaled state.
                    AllDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    listener.BeginAccept(new AsyncCallback(AcceptCallback),listener);

                    // Wait until a connection is made before continuing.
                    AllDone.WaitOne();
                }

            }
            catch (Exception)
            {
                Out.WriteLine("An application is already listening the port 8080." + Environment.NewLine + "The emulator will exit!");
            }
        }

        /// <summary>
        /// Колбэк принятия входящего соединения: создаёт объект пользователя (игры) для сокета клиента.
        /// </summary>
        /// <param name="ar">Результат асинхронной операции Accept.</param>
        public static void AcceptCallback(IAsyncResult ar)
        {
            AllDone.Set();      
                    new Do.game.User(((Socket)ar.AsyncState).EndAccept(ar));
        }
    }
}
