using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace TeleportRequest
{
    public static class Permissions
    {
        [Description("User can accept or deny a teleport request")]
        public static readonly string tpcon = "tprequest.consider";

        [Description("User can set how teleport request behaves")]
        public static readonly string tpset = "tprequest.set";

        [Description("User can teleport to anyone without requestiong")]
        public static readonly string tpsuper = "tprequest.super";
    }
}
