using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeleportRequest
{
    public class Players
    {
        //0 is off, 1 is on for request, 2 is always teleportable
        public byte TPType = 1;
        public byte TPHereType = 1;
        public bool AcknowledgeA = false;
        public bool AcknowledgeSet = false;
    }
}
