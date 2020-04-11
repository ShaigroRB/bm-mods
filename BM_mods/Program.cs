using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// alias for namespace
using lib = BM_RCON.BM_RCON_lib;
using RequestType = BM_RCON.BM_RCON_lib.RequestType;


namespace BM_RCON.mods
{
    class Program
    {
        // change default parameters if needed
        const int port = 42070;
        const string addr = "127.0.0.1";
        const string passwd = "admin";

        private static void sendRequest(lib.BM_RCON rcon, RequestType requestType, string body, lib.ILogger logger)
        {
            Thread.Sleep(160);
            rcon.SendRequest(requestType, body);
            logger.Log("");
        }

        private static lib.RCON_Event receiveEvt(lib.BM_RCON rcon, lib.ILogger logger)
        {
            lib.RCON_Event evt = rcon.ReceiveEvent();
            logger.Log("");
            return evt;
        }

        static int Main(string[] args)
        {
            lib.ILogger logger = new lib.ConsoleLogger();
            try
            {
                /*
                 * Example on how to use the BM rcon lib
                 * The program reads from the chat.
                 * If in the chat is written "!bigtext Hello",
                 * it will call the "eventtext" command with "Hello" as string in red.
                 * If a player taunts, the program will stops.
                */
                string body = passwd;
                string bigtext_cmd = "!bigtext";
                // init rcon object with address, port and password
                lib.BM_RCON rcon_obj = new lib.BM_RCON(addr, port, body, logger);
                // connect the rcon client to addr:port with body
                rcon_obj.Connect();
                logger.Log("");

                // enable mutators on server if not enabled
                // sendRequest(rcon_obj, RequestType.command, "enablemutators");

                lib.RCON_Event evt;
                while (true)
                {
                    // receive the latest event
                    evt = rcon_obj.ReceiveEvent();
                    logger.Log("");

                    // check if somebody type something in the chat
                    if (evt.EventID == (short)lib.EventType.chat_message)
                    {
                        // get the message
                        String msg = evt.JsonAsObj.Message.ToString();

                        // check if "!bigtext" is in the message
                        int index = msg.IndexOf(bigtext_cmd);
                        if (index != -1)
                        {
                            // get the text to display
                            string bigtext = msg.Substring(index + bigtext_cmd.Length);
                            // send a request which is the command "eventtext" with its parameters
                            sendRequest(rcon_obj, RequestType.command, $"eventtext \"{bigtext}\" \"255\"", logger);
                        }
                    }

                    // if the server ping the rcon client,
                    // ping it back to keep the connection alive
                    if (evt.EventID == (short)lib.EventType.rcon_ping)
                    {
                        sendRequest(rcon_obj, RequestType.ping, "I pinged", logger);
                    }
                    // if a player taunts, stop the loop
                    if (evt.EventID == (short)lib.EventType.player_taunt)
                    {
                        break;
                    }
                }

                // avoid sending to oblivion the last request made
                Thread.Sleep(160);

                // disconnect the rcon client
                rcon_obj.Disconnect();


            }
            // if something goes wrong, you will end up here
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                logger.LogError("Something went wrong in the main.");
            }

            // press 'Enter' to exit the console
            Console.Read();
            return 0;
        }
    }
}
