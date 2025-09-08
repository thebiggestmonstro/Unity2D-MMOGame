using Google.Protobuf.Protocol;
using Server.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class Player : GameObject
	{
		public int PlayerDbId { get; set; }
		public ClientSession Session { get; set; }

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

        // 플레이어가 퇴장하는 시점에서 플레이어의 변경사항을 DB에 저장 
        public void OnLeaveGame()
		{
            using (AppDbContext db = new AppDbContext())
            {
				PlayerDb playerDb = new PlayerDb();
				playerDb.PlayerDbId = PlayerDbId;
                playerDb.Hp = Stat.Hp;

				db.Entry(playerDb).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
				db.Entry(playerDb).Property(nameof(PlayerDb.Hp)).IsModified = true;
				db.SaveChanges();
            }
        }

		// Player 코드의 남은 문제
		// 1) 서버가 다운되면 저장되지 않은 정보는 날아감
		// 2) 플레이어가 퇴장하자마자 DB에 저장하므로 단계적인 처리가 이뤄지지 않음
	}
}
