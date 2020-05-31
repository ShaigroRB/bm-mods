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
            bool ongoing_game = true;
            lib.EventType latest_evt_type;
            dynamic json_obj;

            // current bet and next bet
            Bet[] bets = new Bet[2];
            int current_bet = 0;
            int next_bet = 1;

            Player[] connected_players = new Player[20];
            Player[] disconnected_players = new Player[200];

            int nb_connected_players = 0;
            int nb_disconnected_players = 0;

            // is there a bet ? (starts when flag_unlocked is received)
            bool is_bet_flag_unlocked = false;

            // start doing stuff
            int amount_of_games = 0;

            rcon.Connect();

            // enable mutators before anything else
            sendRequest(rcon, lib.RequestType.command, "enablemutators");

            while (amount_of_games < 10)
            {
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

                                nb_connected_players++;
                                nb_disconnected_players--;
                                // if player exists (already joined the ongoing game before)
                                if (index != -1)
                                {
                                    if (null_index == -1)
                                    {
                                        logger.LogError("PROBLEM: more than 20 players in server should be impossible.");
                                        ongoing_game = false;
                                        amount_of_games = 10;
                                    }
                                    else
                                    {
                                        disconnected_players[index].Connected();
                                        connected_players[null_index] = disconnected_players[index];
                                        disconnected_players[index] = disconnected_players[nb_disconnected_players];
                                        disconnected_players[nb_disconnected_players] = null;
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

                                nb_connected_players--;
                                nb_disconnected_players++;

                                Player player = connected_players[index];

                                player.Disconnected();
                                player.IsAlive = false;

                                if (bets[current_bet] != null)
                                {
                                    // TODO: update vote of player
                                    bets[current_bet].UpdateDeadPlayer(player);
                                }

                                disconnected_players[null_index] = connected_players[index];
                                connected_players[index] = connected_players[nb_connected_players];
                                connected_players[nb_connected_players] = null;

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
                                logger.Log("[FIXME] !votestate & !help");

                                // chat message sent by the server
                                if (json_obj.PlayerID == -1)
                                {
                                    break;
                                }

                                string msg = json_obj.Message;
                                string playerName = json_obj.Name;

                                // FIXME: check for !bet written at the start of the sentence
                                // TODO: separate !bet command from !vote command
                                bool is_bet_cmd = isBetCommand(bets, connected_players, nb_connected_players, playerName, msg, rcon);
                                if (is_bet_cmd)
                                {
                                    break;
                                }
                                bool is_vote_cmd;
                                (is_vote_cmd, is_bet_flag_unlocked) = isVoteCommand(bets, connected_players, playerName, msg, rcon);

                                if (is_vote_cmd)
                                {
                                    break;
                                }
                                // !votestate command
                                // !help command

                            }
                            break;
                    }
                }
                amount_of_games++;
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

        private int indexPlayerGivenName(Player[] players, string playerName)
        {
            int index = -1;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null)
                {
                    break;
                }
                else
                {
                    if (players[i].Name == playerName)
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

        private bool isBetCommand(Bet[] bets, Player[] players, int nbPlayersConnected,
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
                    if (betNumber > nbPlayersConnected)
                    {
                        betNumber = nbPlayersConnected;
                    }
                    bets[nextBetIndex] = new Bet(betNumber, players);
                    sendMsgToAll(rcon,
                        "A bet has been made. " +
                        $"{betNumber} is the number of people that need to survive to win the bet. " +
                        "Type !vote yes/no/dunno to vote for the bet.",
                        Color.teal);
                }
                else
                {
                    sendPrivateMsg(rcon, playerName, "!bet command is used like this: !bet <positive number>", Color.orange);
                    return isBetCommand;
                }
            }
            return isBetCommand;
        }

        private void setAllPlayersVotes(Player[] players, VoteState vote)
        {
            foreach (Player player in players)
            {
                if (player != null)
                {
                    player.Vote = vote;
                }
            }
        }

        private (bool, bool) isVoteCommand(Bet[] bets, Player[] players,
                                    string playerName, string msg, lib.BM_RCON rcon)
        {
            string strVoteCmd = "!vote ";
            int next_bet = 1;
            Bet nextBet = bets[next_bet];
            bool is_bet_flag_unlocked = false;

            int indexVoteMsg = msg.IndexOf(strVoteCmd);
            bool isVoteCommand = indexVoteMsg != -1;
            if (isVoteCommand)
            {

                if (nextBet == null)
                {
                    sendPrivateMsg(rcon, playerName, "No bet exists. Make one with !bet <positive number>", Color.orange);
                    return (isVoteCommand, is_bet_flag_unlocked);
                }

                string[] yeses = { "yes", "y" };
                string[] noes = { "no", "n" };
                string[] neutral = { "neutral", "dunno", "d" };

                string vote = msg.Substring(indexVoteMsg + strVoteCmd.Length);
                bool[] voteTypes = {
                    yeses.Contains(vote),
                    noes.Contains(vote),
                    neutral.Contains(vote)
                };

                int index = Array.IndexOf(voteTypes, true);
                if (index == -1)
                {
                    sendPrivateMsg(rcon, playerName, "Only vote with !vote <yes/no/dunno>", Color.orange);
                    return (isVoteCommand, is_bet_flag_unlocked);
                }

                int indexPlayer = indexPlayerGivenName(players, playerName);
                Player player = players[indexPlayer];

                player.Vote = (VoteState)index;

                bool? isBetValidated = nextBet.SetPlayerVote(player);
                // if bet == null, everyone did not vote yet
                if (isBetValidated != null)
                {
                    // whether the vote is accepted or not, reinitialize every vote
                    setAllPlayersVotes(players, VoteState.NOTHING);

                    if ((bool)isBetValidated)
                    {
                        nextBet.SetPlayersInBet(players);
                        is_bet_flag_unlocked = true;
                    }
                    else
                    {
                        is_bet_flag_unlocked = false;
                        bets[next_bet] = null;
                    }
                }
            }
            return (isVoteCommand, is_bet_flag_unlocked);
        }
    }
}
