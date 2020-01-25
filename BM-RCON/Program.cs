﻿using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// alias for namespace
using BM_RCON_lib = BM_RCON.BM_RCON_lib;
using RequestType = BM_RCON.BM_RCON_lib.RequestType;


namespace BM_RCON
{
    class Program
    {
        // change default parameters if needed
        const int port = 42070;
        const string addr = "127.0.0.1";
        const string passwd = "admin";

        static int Main(string[] args)
        {
            try
            {
                bool test_lib = true;

                // Test connection

                // type password
                /* 
                 String body = String.Empty;
                 Console.OutputEncoding = Encoding.UTF8;
                 Console.Write("Type the password of rcon server: ");
                 body = Console.ReadLine();
                 //*/
                string body = passwd;
                // init rcon object
                BM_RCON_lib.BM_RCON rcon_obj = new BM_RCON_lib.BM_RCON(addr, port, body);
                if (test_lib)
                {
                    rcon_obj.Connect();

                    Thread.Sleep(160);
                    rcon_obj.ReceiveEvent();

                    Thread.Sleep(160);
                    rcon_obj.SendRequest(RequestType.command, "enablemutators");
                    Thread.Sleep(160); // 1
                    rcon_obj.SendRequest(RequestType.command, "echo \"Success! (1)\" \"255\"");
                    Thread.Sleep(160); // 2
                    rcon_obj.SendRequest(RequestType.command, "echo \"Success! (2)\" \"255\"");
                    Thread.Sleep(160); // 3
                    rcon_obj.SendRequest(RequestType.command, "echo \"Success! (3)\" \"255\"");
                    Thread.Sleep(160); // ping
                    rcon_obj.SendRequest(RequestType.ping, "It's okay.");
                    Thread.Sleep(160); // 4
                    rcon_obj.SendRequest(RequestType.command, "echo \"Success! (4)\" \"255\"");
                    Thread.Sleep(160); // 5
                    rcon_obj.SendRequest(RequestType.command, "echo \"Success! (5)\" \"255\"");
                    Thread.Sleep(160); // last request before disconnecting

                    rcon_obj.Disconnect();
                }

                if (!test_lib)
                {

                    byte[] finalPacket = rcon_obj.CreatePacket(BM_RCON_lib.RequestType.login, body);
                    TcpClient client = new TcpClient(addr, port);
                    NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = 4000;

                    stream.Write(finalPacket, 0, finalPacket.Length);

                    Console.WriteLine("Packet with password sent.");
                    Console.WriteLine("Final packet: " + System.Text.Encoding.UTF8.GetString(finalPacket));


                    string msg = " confetti";

                    while (true)
                    {
                        Thread.Sleep(160);
                        byte[] packet_received = new byte[client.ReceiveBufferSize];
                        if (client.ReceiveBufferSize > 0)
                        {
                            int tmp = stream.Read(packet_received, 0, packet_received.Length);
                            msg = System.Text.Encoding.UTF8.GetString(packet_received, 0, tmp);
                            Console.WriteLine("Packet received: {0}", msg);
                        }
                    }
                    Thread.Sleep(160);
                    msg = "test \"sdf\" \"sdfdsf\"";

                    byte[] test = rcon_obj.CreatePacket(BM_RCON_lib.RequestType.command, msg);
                    stream.Write(test, 0, test.Length);
                    Console.WriteLine("Command {0} sent.", msg);

                    stream.Close();
                    client.Close();
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("Hello there!");
            }

            Console.Read();
            return 0;
        }
    }
}
