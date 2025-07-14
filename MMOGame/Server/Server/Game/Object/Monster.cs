using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;

namespace Server.Game.Object
{
    public class Monster : GameObject
    {
        public Monster() 
        {
            ObjectType = GameObjectType.Monster;
        }
    }
}
