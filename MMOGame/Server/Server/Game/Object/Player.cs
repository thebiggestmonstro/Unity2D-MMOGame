using Google.Protobuf.Protocol;
using Server.Game.Room;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Object
{
    public class Player : GameObject
    {
        public ClientSession Session { get; set; }

        public Player()
        {
            ObjectType = GameObjectType.Player;
            Speed = 10.0f;
        }

        public override void OnDamaged(GameObject attacker, int damage)
        {
            // 실제 스탯에 영향을 주는 로직은 아직 X
            Console.WriteLine($"{this.Info.Name} got {damage} damaged !");
        }
    }
}
