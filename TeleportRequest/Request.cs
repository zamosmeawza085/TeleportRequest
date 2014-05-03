using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeleportRequest
{
    public class Request
    {
        public bool Direction; //False means TP request, True means TPhere request
        public byte ReceiverID;
        public int Timeout;
    }
}
