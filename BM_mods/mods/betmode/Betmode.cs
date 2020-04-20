using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using lib = BM_RCON.BM_RCON_lib;

namespace BM_RCON.mods.betmode
{
    class Betmode
    {
        const int port = 42070;
        const string addr = "127.0.0.1";
        const string passwd = "admin";



        string[] colors = {
            "#ff0000",
            "#00ff00",
            "#0000ff",
            "#008080",
            "#800080",
            "#fff000",
            "#ffa500",
            "#10a5f5"
        };

        enum Color
        {
            red = 0,
            green,
            blue,
            teal,
            purple,
            yellow,
            orange,
            light_blue
        }

        private void sendRequest(lib.BM_RCON rcon, lib.RequestType requestType, string body)
        {
            Thread.Sleep(160);
            rcon.SendRequest(requestType, body);
        }

        private lib.RCON_Event receiveEvt(lib.BM_RCON rcon)
        {
            lib.RCON_Event evt = rcon.ReceiveEvent();
            return evt;
        }
        static int Main(string[] args)
        {
            /*
             * - you bet the minimum *number of people who will survive through the next wave without dying
             * - the next wave will spawn the same amount of bosses than the *number bet
             * - if the bet is won, every player will earn a *number of random vices
            */
             /*
              * the events to catch are:
              * - player_connect:
              *     - Check if an instance of Player corresponding to the player exists
              *     - If not, create one and add it to connected_players
              *     - Otherwise, move the corresponding Player from disconnected_players to connected_players
              * - player_disconnect:
              *     - Set the Player as dead
              *     - Update the Bet (if one) with the Player
              *     - Move the Player from connected_players to disconnected_players
              * - player_spawn:
              *     - Set the Player as alive
              * - player_death:
              *     - Set the Player as dead
              *     - Update the Bet (if one) with the Player
              * - survival_get_vice:
              *     - Get profile and new vice
              *     - Search for the Player with the corresponding profile
              *     - Update its vices with the new vice
              * - survival_use_vice:
              *     - Get profile and vice used
              *     - Search for the Player with the corresponding profile
              *     - Update its vices with vice used
              * - survival_new_wave:
              *     - Stop checking for vote in chat for next Bet
              *     - Set Vote of all connected_players to NOTHING
              *     - Set Players in Bet with connected_players
              *     - Decide if next Bet is accepted or not depending on Vote of Players
              *     - If accepted, set is_bet_flag_unlocked to true
              *     - If not accepted, set is_bet_flag_unlocked to false and set next Bet to null
              *     - Check if current Bet is won
              *     - If yes, for each Player in connected_players:
              *         - Get the vices from Bet
              *         - Update the Player's vices
              *         - Send request to update the player's vices
              *     - Replace current Bet by next Bet
              * - survival_flag_unlocked:
              *     - Check is_bet_flag_unlocked
              *     - If true, for each enemies in Bet:
              *         - Send request to spawn the enemy
              * - chat_message:
              *     - Check if next Bet exists
              *     - If yes, check if !vote <yes/no> has been written:
              *         - Get profile from Player
              *         - For the Player in connected_players with the same profile, set its Vote
              *         - Check if Players in connected_players all have a Vote to either YES or NO
              *         - If yes, check if next Bet is valid:
              *             - Set Vote of all connected_players to NOTHING
              *             - Set Players in Bet with connected_players
              *             - If valid, set is_bet_flag_unlocked to true
              *             - If not valid, set is_bet_flag_unlocked to false and set next Bet to null
              *     - If no, create new next Bet with <number> 
             */
            lib.ILogger logger = new lib.ConsoleLogger();
            logger.DisableDebug();
            try
            {
                Betmode betmode = new Betmode();
                betmode.Start(logger);
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                logger.LogError("Something went wrong in the main.");
            }

            // press 'Enter' to exit the console
            Console.Read();

            return 0;
        }

        public void Start(lib.ILogger logger)
        {
            // init variables
            lib.BM_RCON rcon = new lib.BM_RCON(addr, port, passwd, logger);
            lib.RCON_Event latest_evt;
            bool ongoing_game;
            lib.EventType latest_evt_type;
            dynamic json_obj;

            // current bet and next bet
            Bet[] bets = new Bet[2];
            int current_bet = 0;
            int next_bet = 1;

            Player[] connected_players = new Player[20];
            Player[] disconnected_players = new Player[200];

            // start doing stuff
            int amout_of_games = 0;

            rcon.Connect();

            // enable mutators before anything else
            sendRequest(rcon, lib.RequestType.command, "enablemutators");

            while (amout_of_games < 10)
            {

                ongoing_game = true;
                while (ongoing_game)
                {
                    latest_evt = receiveEvt(rcon);
                    latest_evt_type = (lib.EventType)latest_evt.EventID;
                    json_obj = latest_evt.JsonAsObj;

                    switch (latest_evt_type)
                    {
                        case lib.EventType.match_end:
                            logger.LogInfo("End of the game");
                            ongoing_game = false;
                            break;

                        case lib.EventType.rcon_ping:
                            sendRequest(rcon, lib.RequestType.ping, "pong");
                            break;

                        case lib.EventType.rcon_disconnect:
                            rcon.Connect();
                            break;

                        case lib.EventType.player_connect:
                            {
                                Profile profile_connect = createProfile((string)json_obj.Profile, (string)json_obj.Store);
                                int index = indexPlayerGivenProfile(disconnected_players, profile_connect);
                                int null_index = indexFirstNull(connected_players);
                                // if player exists (already joined the ongoing game before)
                                if (index != -1)
                                {
                                    if (null_index == -1)
                                    {
                                        logger.LogError("PROBLEM: more than 20 players in server should be impossible.");
                                        ongoing_game = false;
                                        amout_of_games = 10;
                                    }
                                    else
                                    {
                                        disconnected_players[index].Connected();
                                        connected_players[null_index] = disconnected_players[index];
                                        disconnected_players[index] = null;
                                    }
                                }
                                // if first time player joined the ongoing game
                                else
                                {
                                    Player player = new Player((string)json_obj.PlayerName, profile_connect);
                                    connected_players[null_index] = player;
                                }

                                //display all connected players
                                printPlayers(connected_players, true, logger);
                            }
                            break;

                        case lib.EventType.player_disconnect:
                            {
                                Profile profile_disconnect = createProfile(json_obj.Profile.ToString());
                                int index = indexPlayerGivenProfile(connected_players, profile_disconnect);
                                int null_index = indexFirstNull(disconnected_players);

                                Player player = connected_players[index];

                                player.Disconnected();
                                player.IsAlive = false;

                                if (bets[current_bet] != null)
                                {
                                    bets[current_bet].UpdateDeadPlayer(player);
                                }

                                disconnected_players[null_index] = connected_players[index];
                                connected_players[index] = null;

                                // display all disconnected players
                                printPlayers(disconnected_players, false, logger);
                            }
                            break;

                        case lib.EventType.player_spawn:
                            {
                                Profile profile = createProfile(json_obj.Profile.ToString());
                                if (profile.EqualsBotProfile())
                                {
                                    break;
                                }
                                int index = indexPlayerGivenProfile(connected_players, profile);
                                if (index == -1)
                                {
                                    /* FIXME */
                                    // create Player & Profile from a player already in the server
                                    // when rcon client connected
                                    break;
                                }
                                Player player = connected_players[index];

                                player.IsAlive = true;
                                logger.LogInfo($"{player.Name} spawned. ({player.IsAlive})");
                            }
                            break;

                        case lib.EventType.player_death:
                            {
                                Profile profile = createProfile(json_obj.VictimProfile.ToString());
                                if (profile.EqualsBotProfile())
                                {
                                    break;
                                }
                                int index = indexPlayerGivenProfile(connected_players, profile);
                                if (index == -1)
                                {
                                    /* FIXME */
                                    // create Player & Profile from a player already in the server
                                    // when rcon client connected
                                    break;
                                }
                                Player player = connected_players[index];

                                player.IsAlive = false;
                                logger.LogInfo($"{player.Name} just died. ({player.IsAlive})");

                                if (bets[current_bet] != null)
                                {
                                    bets[current_bet].UpdateDeadPlayer(player);
                                }
                            }
                            break;

                        case lib.EventType.survival_get_vice:
                            {
                                Profile profile = createProfile(json_obj.Profile.ToString());
                                if (profile.EqualsBotProfile())
                                {
                                    break;
                                }
                                int index = indexPlayerGivenProfile(connected_players, profile);
                                if (index == -1)
                                {
                                    /* FIXME */
                                    // create Player & Profile from a player already in the server
                                    // when rcon client connected
                                    break;
                                }
                                lib.ViceID viceID = (lib.ViceID)json_obj.ViceID;
                                Player player = connected_players[index];

                                player.VicesAdded(viceID, 1);
                                logger.LogInfo($"{player.Name} got a {viceID.ToString()}");
                            }
                            break;

                        case lib.EventType.survival_use_vice:
                            {
                                Profile profile = createProfile(json_obj.Profile.ToString());
                                if (profile.EqualsBotProfile())
                                {
                                    break;
                                }
                                int index = indexPlayerGivenProfile(connected_players, profile);
                                if (index == -1)
                                {
                                    /* FIXME */
                                    // create Player & Profile from a player already in the server
                                    // when rcon client connected
                                    break;
                                }
                                lib.ViceID viceID = (lib.ViceID)json_obj.ViceID;
                                Player player = connected_players[index];

                                player.ViceUsed(viceID);
                                logger.LogInfo($"{player.Name} used a {viceID.ToString()}");
                            }
                            break;

                        case lib.EventType.survival_new_wave:
                            {
                                logger.Log("[FIXME] survival_new_wave");
                            }
                            break;

                        case lib.EventType.survival_flag_unlocked:
                            {
                                logger.Log("[FIXME] survival_flag_unlocked");
                            }
                            break;

                        case lib.EventType.chat_message:
                            {
                                logger.Log("[FIXME] !vote & !votestate & !help");
                                
                                // chat message sent by the server
                                if (json_obj.PlayerID == -1)
                                {
                                    break;
                                }

                                string strVoteCmd = "!vote ";

                                bool nextBetExists = !(bets[next_bet] == null);
                                string msg = json_obj.Message;
                                string playerName = json_obj.Name;


                                if (!isBetCommand(bets, connected_players, playerName, msg, rcon))
                                {
                                    logger.Log("[FIXME] !vote check");
                                    sendMsgToAll(rcon, "[FIXME] !vote check", Color.purple);
                                }
                                
                            }
                            break;
                    }
                }
                amout_of_games++;
            }

            rcon.Disconnect();
        }

        // private methods
        private int indexPlayerGivenProfile(Player[] players, Profile profile)
        {
            /*
             * Given a list of players and a profile as string,
             * Return the index of the player corresponding to the profile if found,
             * otherwise return -1
             */
            int index = -1;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null)
                {
                    break;
                }
                else
                {
                    if (players[i].SameProfileAs(profile))
                    {
                        index = i;
                        break;
                    }
                }
            }
            return index;
        }

        private int indexFirstNull(Player[] list)
        {
            int index = -1;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == null)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        private Profile createProfile(string full_profile)
        {
            return new Profile(full_profile);
        }

        private Profile createProfile(string profileID, string storeID)
        {
            return new Profile(profileID, storeID);
        }

        private void printPlayers(Player[] players, bool are_connected, lib.ILogger logger)
        {
            string connected = are_connected ? "CONNECTED" : "DISCONNECTED";
            logger.LogInfo($"The {connected} players are:");

            foreach (var player in players)
            {
                if (player == null)
                {
                    logger.Log("");
                    return;
                }
                logger.LogInfo($"{player.Name}, ");
            }
        }

        private bool isStringANumber(string msg)
        {
            return msg != "" && msg.All(char.IsDigit);
        }

        private void sendPrivateMsg(lib.BM_RCON rcon, Player player, string msg, Color color)
        {
            sendRequest(rcon, lib.RequestType.command, $"pm \"{player.Name}\" \"{msg}\" \"{colors[(int)color]}\"");
        }
        private void sendPrivateMsg(lib.BM_RCON rcon, string name, string msg, Color color)
        {
            sendRequest(rcon, lib.RequestType.command, $"pm \"{name}\" \"{msg}\" \"{colors[(int)color]}\"");
        }

        private int countNbPlayers(Player[] players)
        {
            int count = 0;
            foreach (Player player in players)
            {
                if (player != null)
                {
                    count++;
                }
            }
            return count;
        }

        private void sendMsgToAll(lib.BM_RCON rcon, string msg, Color color)
        {
            sendRequest(rcon, lib.RequestType.command, $"rawsay \"{msg}\" \"{colors[(int)color]}\"");
        }

        private void displayBetVotingState(lib.BM_RCON rcon, string playerName, Bet bet)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("To accept/reject a bet, everyone needs to vote. ");
            stringBuilder.Append("Votes: ");

            var voteStates = Enum.GetValues(typeof(VoteState));
            int[] votes = bet.BetVotingState();

            foreach (VoteState voteState in voteStates)
            {
                stringBuilder.Append($"{voteState}: {votes[(int)voteState]}, ");
            }

            sendPrivateMsg(rcon, playerName, stringBuilder.ToString(), Color.light_blue);
        }

        private bool isBetCommand(Bet[] bets,Player[] players,
                                    string playerName, string msg, lib.BM_RCON rcon)
        {
            string strBetCmd = "!bet ";
            int nextBetIndex = 1;
            bool nextBetExists = bets[nextBetIndex] != null;

            int indexBetMsg = msg.IndexOf(strBetCmd);
            bool isBetCommand = (indexBetMsg != -1);

            if (isBetCommand)
            {
                string potentialBetNumber = msg.Substring(indexBetMsg + strBetCmd.Length);
                if (isStringANumber(potentialBetNumber))
                {
                    if (nextBetExists)
                    {
                        sendPrivateMsg(rcon, playerName,
                            "A bet already exists. Bet's voting state will be sent to you.",
                            Color.orange);
                        displayBetVotingState(rcon, playerName, bets[nextBetIndex]);
                        return isBetCommand;
                    }
                    int betNumber = Int32.Parse(potentialBetNumber);
                    if (betNumber <= 0 || betNumber > 20)
                    {
                        sendPrivateMsg(rcon, playerName, "The bet should be between 1 and 20.", Color.orange);
                        return isBetCommand;
                    }
                    // if next bet does not exist and bet valid
                    int nbPlayersConnected = countNbPlayers(players);
                    if (betNumber > nbPlayersConnected)
                    {
                        betNumber = nbPlayersConnected;
                    }
                    bets[nextBetIndex] = new Bet(betNumber, players);
                    sendMsgToAll(rcon,
                        "A bet has been made. " +
                        $"{betNumber} is the number of people that need to survive to win the bet. " +
                        "Vote with !vote yes/no to accept the bet.",
                        Color.teal);
                }
                else
                {
                    sendPrivateMsg(rcon, playerName, "!bet command is used like this: !bet <number>", Color.orange);
                    return isBetCommand;
                }
            }
            return isBetCommand;
        }
    }
}
