﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShellClient
{
    class Program
    {
        
        static void Main(string[] args)
        {
            ShellClient shell = new ShellClient(/*args[0]*/ "192.168.17.1");
            shell.Connect();
        }
    }
}
