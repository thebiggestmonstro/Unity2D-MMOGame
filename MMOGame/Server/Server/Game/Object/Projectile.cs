using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;

namespace Server.Game.Object
{
    public class Projectile : GameObject
    {
        // 투사체를 생성한 스킬을 저장
        public Data.Skill Data { get; set; }

        public Projectile()
        {
            ObjectType = GameObjectType.Projectile;
        }

        public virtual void Update()
        { 
        
        }
    }
}
