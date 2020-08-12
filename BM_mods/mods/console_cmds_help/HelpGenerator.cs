using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using lib = BM_RCON.BM_RCON_lib;
using RequestType = BM_RCON.BM_RCON_lib.RequestType;
using System.Threading;

namespace BM_RCON.mods.console_cmds_help
{
    class HelpGenerator
    {
        private string filename;
        private List<string> cmds;
        private List<string> cmds_help;
        private lib.BM_RCON rcon;
        private lib.ILogger logger;

        public HelpGenerator(lib.BM_RCON rcon, lib.ILogger logger)
        {
            this.rcon = rcon;
            this.logger = logger;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            this.filename = Path.Combine(Directory.GetCurrentDirectory(), @"mods\console_cmds_help\commands.txt");
            cmds = new List<string>();
            cmds_help = new List<string>();
        }

        public void SetFilename(string filename)
        {
            this.filename = filename;
        }

        public void ParseFile()
        {
            string[] lines = File.ReadAllLines(filename);
            cmds = new List<string>(lines);
        }

        public void GetHelpForCmds()
        {
            logger.LogInfo("Getting help for commands.");

            foreach (string cmd in cmds)
            {
                string correctCmd = getStrBeforeFirstChar(cmd, ' ');
                if (correctCmd == string.Empty && cmd != null)
                {
                    correctCmd = cmd;
                }

                sendRequest(rcon, RequestType.command, "help \"" + correctCmd + "\"");

                lib.RCON_Event evt = receiveEvt(rcon);
                lib.EventType latestEvt = (lib.EventType)evt.EventID;
                bool notCommandEntered = true;
                while (notCommandEntered)
                {
                    switch (latestEvt)
                    {
                        case lib.EventType.command_entered:
                            dynamic objJson = evt.JsonAsObj;
                            string returnText = objJson.ReturnText;
                            cmds_help.Add(returnText);
                            notCommandEntered = false;
                            break;
                        case lib.EventType.rcon_ping:
                            sendRequest(rcon, RequestType.ping, "hello");
                           logger.LogInfo("(" + DateTime.Now + "): ping");
                            break;
                        default:
                            break;
                    }
                    if (notCommandEntered)
                    {
                        evt = receiveEvt(rcon);
                        latestEvt = (lib.EventType)evt.EventID;
                    }
                }
            }
        }

        private string strToMarkdownHelpStr(string str)
        {
            string helpDelim = "): ";
            string trimmedHelp = str.Substring(str.IndexOf(helpDelim) + helpDelim.Length);

            int indexOfCheats = trimmedHelp.IndexOf("[Requires cheats]");
            if (indexOfCheats > 0)
            {
                trimmedHelp = trimmedHelp.Substring(0, indexOfCheats) + "**[Requires cheats]**";
            }
            return trimmedHelp;
        }

        public void GenerateMarkdown(string filename)
        {
            logger.LogInfo("Generating markdown for commands' help.");
            for (int i = 0; i < cmds_help.Count; i++)
            {
                cmds_help[i] = strToMarkdownHelpStr(cmds_help[i]);
            }

            using (StreamWriter outputFile = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), filename)))
            {
                outputFile.WriteLine("| Console commands | Help description |");
                outputFile.WriteLine("| --- | --- |");
                for (int i = 0; i < cmds.Count; i++)
                {
                    outputFile.WriteLine("| " + cmds[i] + " | " + cmds_help[i] + " |");
                }
            }
        }

        private string getStrBeforeFirstChar(string fullStr, char character)
        {
            if (!string.IsNullOrEmpty(fullStr))
            {
                int charIndex = fullStr.IndexOf(character);
                if (charIndex > 0)
                {
                    return fullStr.Substring(0, charIndex);
                }
            }
            return string.Empty;
        }
        private static void sendRequest(lib.BM_RCON rcon, RequestType requestType, string body)
        {
            Thread.Sleep(160);
            rcon.SendRequest(requestType, body);
        }

        private static lib.RCON_Event receiveEvt(lib.BM_RCON rcon)
        {
            lib.RCON_Event evt = rcon.ReceiveEvent();
            return evt;
        }
    }
}
