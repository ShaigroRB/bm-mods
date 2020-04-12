using System;
using System.Collections.Generic;
using System.Text;

namespace BM_RCON.mods.betmode
{
    class Profile
    {
        string profileID;
        string storeID;
        string profile;

        public Profile(string profileID, string storeID)
        {
            this.profileID = profileID;
            this.storeID = storeID;
            this.profile = $"{{ \"ProfileID\": \"{profileID}\", \"StoreID\": \"{storeID}\" }}";
        }

        public Profile(string profile)
        {
            this.profile = profile;
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

        public string FullProfile
        {
            get
            {
                return this.profile;
            }
        }

        public bool Equals(Profile profile)
        {
            return this.FullProfile.Equals(profile.FullProfile);
        }

        public bool EqualsBotProfile()
        {
            return this.FullProfile.Equals("{ \"ProfileID\": \"\", \"StoreID\": \"-1\" }");
        }
    }
}
