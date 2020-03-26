using System;
using System.Collections.Generic;
using System.Text;

namespace BM_RCON.mods.betmode
{
    class Profile
    {
        string profileID;
        string storeID;

        public Profile(string profileID, string storeID)
        {
            this.profileID = profileID;
            this.storeID = storeID;
        }

        public string ProfileID
        {
            get
            {
                return this.profileID;
            }
        }

        public string StoreID
        {
            get
            {
                return this.storeID;
            }
        }

        public bool Equals(Profile profile)
        {
            return (this.profileID.Equals(profile.ProfileID)) && (this.storeID.Equals(profile.StoreID));
        }
    }
}
