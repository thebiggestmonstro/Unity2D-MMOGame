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
            if (Owner == null || Room == null)
                return;

            if (_nextMoveTick >= Environment.TickCount64)
                return;

            _nextMoveTick = Environment.TickCount64 + 50;

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
                    // 피격 판정 로직은 아직 X
                }

                // 화살이 피격된 후 소멸
                Room.LeaveGame(Id);
            }
        }
    }
}
