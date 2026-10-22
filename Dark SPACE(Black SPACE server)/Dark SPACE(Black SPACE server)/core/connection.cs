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
    public class StateObject
    {
        public Socket WorkSocket = null;
        public  const int BufferSize = 2048;
        public byte[] Buffer = new byte[BufferSize];
        public StringBuilder Sb = new StringBuilder();
    }
    


  public  class AsynchronousSocketListener
    {
        // Thread signal.
        public static ManualResetEvent AllDone = new ManualResetEvent(false);     
 
        public static void StartListening()
        {
            new Thread(new Program().RUN).Start();
            var localEndPoint = new IPEndPoint(IPAddress.Any, 8080);
         
            var listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

          
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
                Out.WriteLine("An application is already listening the port 80." + Environment.NewLine + "The emulator will exit!");
            }
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            AllDone.Set();      
                    new Do.game.User(((Socket)ar.AsyncState).EndAccept(ar));
        }
    }
}
