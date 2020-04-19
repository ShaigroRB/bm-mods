using System;
using System.Collections.Generic;
using System.Text;
using lib = BM_RCON.BM_RCON_lib;

namespace BM_RCON.mods.betmode
{
    class Bet
    {
        int bet;
        /*
         * In early waves, enemies will only be bosses.
         * In later waves, enemies will be bosses and minions.
         * I may add sub-types later on.
        */
        int[] enemies;
        int[] vices;
        // players in bet are the connected players when the flag unlocks for the next wave
        Player[] players_in_bet;
        // players voting for the bet are the connected players before the bet is validated
        Player[] players_voting;

        public Bet(int bet, Player[] players_voting)
        {
            // not a constant because we don't know yet no other enemies will be added
            int total_nb_enemies = Enum.GetNames(typeof(lib.EnemyID)).Length;
            // there is 40 vices and it won't change
            int total_nb_vices = 40;

            this.bet = bet;
            this.nb_players_voting = nb_players_voting;
            this.enemies = new int[total_nb_enemies];
            this.vices = new int[total_nb_vices];
            // maximum of players in a server is 20 people
            this.players_in_bet = new Player[20];
            this.players_voting = new Player[20];

            // update players voting
            Array.Copy(players_voting, this.players_voting, players_voting.Length);

            randomizeBosses();
            randomizeVices();
        }

        private void randomizeBosses()
        {
            int first_boss = (int)lib.EnemyID.indigo;
            int last_boss = (int)lib.EnemyID.roxxy;

            Random rnd = new Random();
            int boss = 0;

            for (int i = 0; i < this.bet; i++)
            {
                boss = rnd.Next(first_boss, last_boss);
                this.enemies[boss] += 1;
            }
        }

        private void randomizeVices()
        {
            int first_vice = (int)lib.ViceID.lager;
            int last_vice = (int)lib.ViceID.water;

            Random rnd = new Random();
            int vice = 0;

            for (int i = 0; i < this.bet; i++)
            {
                vice = rnd.Next(first_vice, last_vice);
                this.vices[vice] += 1;
            }
        }

        public void SetPlayersInBet(Player[] players)
        {
            Array.Copy(players, this.players_in_bet, players.Length);
        }

        private void clearAllArrays()
        {
            Array.Clear(this.enemies, 0, this.enemies.Length);
            Array.Clear(this.vices, 0, 40);
            Array.Clear(this.players_in_bet, 0, 20);
        }

        public void UpdateBet(int nb_bet)
        {
            this.bet = nb_bet;
            clearAllArrays();

            randomizeVices();
            randomizeBosses();
        }

        public bool IsBetWon()
        {
            bool is_bet_won = false;
            int nb_players = this.players_in_bet.Length;
            int nb_players_alive = 0;

            for (int i = 0; !is_bet_won && i < nb_players; i++)
            {
                Player currentPlayer = this.players_in_bet[i];
                if (currentPlayer != null && currentPlayer.IsAlive)
                {
                    nb_players_alive++;
                    if (nb_players_alive >= this.bet)
                    {
                        is_bet_won = true;
                    }
                }
            }
            return is_bet_won;
        }

        public void UpdateDeadPlayer(Player player)
        {
            int nb_players = this.players_in_bet.Length;
            bool is_player_found = false;
            for (int i = 0; !is_player_found && i < nb_players; i++)
            {
                Player currentPlayer = this.players_in_bet[i];
                if (currentPlayer != null && currentPlayer.SameProfileAs(player))
                {
                    currentPlayer.IsAlive = false;
                    is_player_found = true;
                }
            }
        }

        public int[] Enemies
        {
            get
            {
                return this.enemies;
            }
        }

        public int[] Vices
        {
            get
            {
                return this.vices;
            }
        }

        /// <summary>
        /// Set player vote for the bet and return if the bet is accepted
        /// </summary>
        /// <param name="player_voting">The player voting</param>
        /// <returns>
        /// If at least one player did not vote, returns null
        /// Otherwise, returns true if bet accepted or false if bet rejected
        /// </returns>
        public bool? SetPlayerVote(Player player_voting)
        {
            foreach (Player player in this.players_voting)
            {
                if (player.SameProfileAs(player_voting))
                {
                    player.Vote = player_voting.Vote;
                    break;
                }
            }
            return isBetValidated();
        }

        private bool? isBetValidated()
        {
            bool? isBetAccepted = null;
            // votes: index based on enum VoteState
            int[] votes = new int[5];

            foreach (Player player in players_voting)
            {
                if (player != null)
                {
                    votes[(int)player.Vote]++;
                }
            }

            if (votes[(int)VoteState.NOTHING] == 0)
            {
                isBetAccepted = votes[(int)VoteState.YES] > votes[(int)VoteState.NO];
            }

            return isBetAccepted;
        }
    }
}
