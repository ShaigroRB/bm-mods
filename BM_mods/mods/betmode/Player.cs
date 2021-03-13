using System;
using System.Collections.Generic;
using System.Text;
using lib = BM_RCON.BM_RCON_lib;

namespace BM_RCON.mods.betmode
{
    class Player
    {
        string name;
        Profile profile;
        // type of vice is the index in the list
        int[] vices;
        bool is_connected;
        bool is_alive;
        VoteState vote;

        public Player(string name, Profile profile)
        {
            this.name = name;
            this.profile = profile;
            this.vices = new int[40];
            // when a player is created, it means the player just connected
            this.is_connected = true;
            this.is_alive = false;
            this.vote = VoteState.NOTHING;
        }

        public Profile Profile
        {
            get
            {
                return this.profile;
            }
        }

        public int[] Vices
        {
            get
            {
                return this.vices;
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.is_connected;
            }
            
            set
            {
                this.is_connected = value;
            }
        }

        public bool IsAlive
        {
            get
            {
                return this.is_alive;
            }
            
            set
            {
                this.is_alive = value;
            }
        }

        public VoteState Vote
        {
            get
            {
                return this.vote;
            }

            set
            {
                this.vote = value;
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public void ViceUsed(lib.Vice vice)
        {
            this.vices[(short)vice] -= 1;
        }

        public void VicesAdded(lib.Vice vice, int amount)
        {
            this.vices[(short)vice] += amount;
        }

        public void Connected()
        {
            this.is_connected = true;
            this.vote = VoteState.NOTHING;
        }

        public void Disconnected()
        {
            this.is_connected = false;
            this.vote = VoteState.OFFLINE;
        }

        public bool SameProfileAs(Player obj)
        {
            return this.profile.Equals(obj.Profile);
        }

        public bool SameProfileAs(Profile profile)
        {
            return this.profile.Equals(profile);
        }
    }
}
