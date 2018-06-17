using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShellClient
{
    public class ShellClient
    {
        Socket Client;
        Socket AccSock;
        Thread ShellThread;
        Thread RecvThread;
        StreamWriter OutputStream;
        StreamReader InputStream;
        IPEndPoint RemoteEp;
        public ShellClient(string IP)
        {
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            RemoteEp = new IPEndPoint(IPAddress.Parse(IP), 12700);

        }

        public void Listen()
        {
            ThreadStart Ts = new ThreadStart(Recieve);
            RecvThread = new Thread(Ts);
            RecvThread.Start();
        }

        public void Connect()
        {
            Client.Connect(RemoteEp);
            Stream NetStream = new NetworkStream(Client);
            InputStream = new StreamReader(NetStream);
            OutputStream = new StreamWriter(NetStream);
            OutputStream.AutoFlush = true;
            string Pass = Console.ReadLine();
            OutputStream.WriteLine(Pass);
            Console.WriteLine(InputStream.ReadLine());

            ThreadStart Ts = new ThreadStart(Listen);
            ShellThread = new Thread(Ts);
            ShellThread.Start();
            try
            {
                while (true)
                {
                    string Command = Console.ReadLine();
                    OutputStream.WriteLine(Command);

                    if (Command.Equals("Stream Terminated"))
                    {
                        Drop();
                    }
                }
            }
            catch (Exception Ex) { }
        }

        public void Recieve()
        {
            try
            {
                string Buf = "";
                Console.WriteLine("\r\n");
                while((Buf = InputStream.ReadLine())!=null)
                {
                    Console.WriteLine(Buf + "\r");
                }
            }
            catch(Exception Ex)
            {
                Console.WriteLine(Ex.StackTrace);
            }
        }
        public void Drop()
        {
            try
            {
                RecvThread.Abort();
                RecvThread = null;
                return;
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
