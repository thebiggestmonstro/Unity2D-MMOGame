using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;

namespace Server.Game.Object
{
    public class Arrow : Projectile
    {
        public GameObject Owner { get; set; }
    }
}
