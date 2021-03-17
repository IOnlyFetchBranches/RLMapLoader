using System;
using System.Collections.Generic;
using System.Text;

namespace RLMapLoader.Components.Core.Constants
{
    public static class GlobalConstants
    {
        public static string G_PROJ_NAME = "rlmaploader";
        public static int MASTER_DB_WAIT_COUNT = 20; //specifies how many 100ms interations sync module will wait in the case of a non-loaded DB
    }
}
