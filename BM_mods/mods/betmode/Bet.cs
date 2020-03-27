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
        int nb_players;

        public Bet(int bet)
        {
            // not a constant because we don't know yet no other enemies will be added
            int total_nb_enemies = Enum.GetNames(typeof(lib.EnemyID)).Length;
            // there is 40 vices and it won't change
            int total_nb_vices = 40;

            this.bet = bet;
            this.enemies = new int[total_nb_enemies];
            this.vices = new int[total_nb_vices];
            // maximum of players in a server is 20 people
            this.players_in_bet = new Player[20];

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
                this.enemies[vice] += 1;
            }
        }

        public void SetPlayersInBet(Player[] players, int nb_players)
        {
            Array.Copy(players, this.players_in_bet, nb_players);
            this.nb_players = nb_players;
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
            int nb_players = this.nb_players;
            int nb_players_alive = 0;

            for (int i = 0; !is_bet_won && i < nb_players; i++)
            {
                if (this.players_in_bet[i].IsAlive)
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
            int nb_players = this.nb_players;
            bool is_player_found = false;
            for (int i = 0; !is_player_found && i < nb_players; i++)
            {
                if (this.players_in_bet[i].SameProfileAs(player))
                {
                    this.players_in_bet[i].IsAlive = false;
                    is_player_found = true;
                }
            }
        }

        public int[] Enemies
        {
            get;
        }

        public int[] Vices
        {
            get;
        }
    }
}
