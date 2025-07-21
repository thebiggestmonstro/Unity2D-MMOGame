using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game.Room;

namespace Server.Game.Object
{
    public class Arrow : Projectile
    {
        public GameObject Owner { get; set; }

        long _nextMoveTick = 0;

        public override void Update()
        {
            if (Data == null || Data.projectile == null || Owner == null || Room == null)
                return;

            if (_nextMoveTick >= Environment.TickCount64)
                return;

            long tick = (long)(1000 / Data.projectile.speed);
            _nextMoveTick = Environment.TickCount64 + tick;

            Vector2Int destPos = GetFrontCellPos();

            // 화살이 이동할 수 있는 경우
            if (Room.Map.CanGo(destPos))
            {
                CellPos = destPos;

                S_Move movePacket = new S_Move();
                movePacket.ObjectId = Id;
                movePacket.PosInfo = PosInfo;
                Room.Broadcast(movePacket);

                Console.WriteLine("Move Arrow !");
            }
            // 화살이 이동할 수 없는 경우
            else
            {
                GameObject target = Room.Map.Find(destPos);
                if (target != null)
                {
                    target.OnDamaged(this, Data.damage + Owner.Stat.Attack);
                }

                // 화살이 피격된 후 소멸
                Room.LeaveGame(Id);
            }
        }
    }
}
