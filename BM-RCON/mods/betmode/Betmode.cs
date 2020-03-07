using System;
using System.Collections.Generic;
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

        private void sendRequest(lib.BM_RCON rcon, lib.RequestType requestType, string body)
        {
            Thread.Sleep(160);
            rcon.SendRequest(requestType, body);
            Console.WriteLine("");
        }

        private lib.RCON_Event receiveEvt(lib.BM_RCON rcon)
        {
            lib.RCON_Event evt = rcon.ReceiveEvent();
            Console.WriteLine("");
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
            try
            {
                Betmode betmode = new Betmode();
                betmode.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("Something went wrong in the main.");
            }

            // press 'Enter' to exit the console
            Console.Read();

            return 0;
        }

        public void Start()
        {
            // init variables
            lib.BM_RCON rcon = new lib.BM_RCON(addr, port, passwd);
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
                            Console.WriteLine("End of the game");
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
                                        Console.WriteLine("PROBLEM: more than 20 players in server should be impossible.");
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
                            }
                            break;

                        case lib.EventType.player_disconnect:
                            { 
                                Profile profile_disconnect = createProfile(json_obj.Profile);
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

        private Profile createProfile(dynamic profile)
        {
            return createProfile((string)profile.ProfileID, (string)profile.StoreID);
        }

        private Profile createProfile(string profileID, string storeID)
        {
            return new Profile(profileID, storeID);
        }
    }
}
