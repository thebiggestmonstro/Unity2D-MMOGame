using Google.Protobuf.Protocol;
using Server.DB;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class Player : GameObject
	{
		public int PlayerDbId { get; set; }
		public ClientSession Session { get; set; }

		// Inventory는 GameRoom에서 건들게 되므로 따로 lock을 설정하지는 않음
		public Inventory Inven { get; private set; } = new Inventory();

		public Player()
		{
			ObjectType = GameObjectType.Player;
		}

		public override void OnDamaged(GameObject attacker, int damage)
		{
            base.OnDamaged(attacker, damage);
		}

		public override void OnDead(GameObject attacker)
		{
			base.OnDead(attacker);
		}

        public void OnLeaveGame()
		{
			//DbTransaction.SavePlayerStatus_AllInOne(this, Room);
			DbTransaction.SavePlayerStatus_Step1(this, Room);
        }
	}
}
