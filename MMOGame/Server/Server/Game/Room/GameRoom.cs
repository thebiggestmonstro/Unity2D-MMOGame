using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Job;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class GameRoom : JobSerializer
	{
		public int RoomId { get; set; }

		Dictionary<int, Player> _players = new Dictionary<int, Player>();
		Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
		Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();

		public Map Map { get; private set; } = new Map();

		// 임시적으로 Init 함수를 static 함수로 변경
		// 인스턴스의 프로퍼티를 사용하도록 내부 로직을 임시적으로 수정
		//public static void Init(GameRoom room, int mapId)
		//{
		//	room.Map.LoadMap(mapId);
		//
		//	// TEMP
		//	Monster monster = ObjectManager.Instance.Add<Monster>();
		//	monster.CellPos = new Vector2Int(5, 5);
		//	room.EnterGame(monster);
		//}

		// 또는 static 함수로 수정하지 않고 다음의 방법을 사용
		public void Init(int mapId)
		{ 
			Map.LoadMap(mapId);

			Monster monster = ObjectManager.Instance.Add<Monster>();
			monster.CellPos = new Vector2Int(5, 5);
			EnterGame(monster);
		}

		public void Update()
		{
			foreach (Monster monster in _monsters.Values)
			{
				monster.Update();
			}

			foreach (Projectile projectile in _projectiles.Values)
			{
				projectile.Update();
			}
		}

        public void EnterGame(GameObject gameObject)
		{
			if (gameObject == null)
				return;

			GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            // 플레이어의 GameRoom 입장 및 스폰 처리
            if (type == GameObjectType.Player)
			{
				Player player = gameObject as Player;
				_players.Add(gameObject.Id, player);
				player.Room = this;

				Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y));

				{
					S_EnterGame enterPacket = new S_EnterGame();
					enterPacket.Player = player.Info;
					player.Session.Send(enterPacket);

                    // 입장한 플레이어의 시점에서 다른 플레이어들 스폰
                    S_Spawn spawnPacket = new S_Spawn();
					foreach (Player p in _players.Values)
					{
						if (player != p)
							spawnPacket.Objects.Add(p.Info);
					}

					// 입장한 플레이어의 시점에서 몬스터들 스폰
					foreach (Monster m in _monsters.Values)
					{
						spawnPacket.Objects.Add(m.Info);
					}

                    // 입장한 플레이어의 시점에서 투사체들 스폰
                    foreach (Projectile p in _projectiles.Values)
					{
						spawnPacket.Objects.Add(p.Info);
					}

					player.Session.Send(spawnPacket);
				}
			}
            // 몬스터의 GameRoom 입장 및 스폰 처리
            else if (type == GameObjectType.Monster)
			{
				Monster monster = gameObject as Monster;
				_monsters.Add(gameObject.Id, monster);
				monster.Room = this;

				Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));
			}
            // 투사체의 GameRoom 입장 및 스폰 처리
            else if (type == GameObjectType.Projectile)
			{
				Projectile projectile = gameObject as Projectile;
				_projectiles.Add(gameObject.Id, projectile);
				projectile.Room = this;
			}
            // 입장한 플레이어를 포함한 오브젝트들의 Spawn을 다른 플레이어들에게 전송
            {
                S_Spawn spawnPacket = new S_Spawn();
				spawnPacket.Objects.Add(gameObject.Info);
				foreach (Player p in _players.Values)
				{
					if (p.Id != gameObject.Id)
						p.Session.Send(spawnPacket);
				}
			}
		}

        public void LeaveGame(int objectId)
		{
			GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

            // 플레이어의 GameRoom 퇴장 및 스폰 해제 처리
            if (type == GameObjectType.Player)
			{
				Player player = null;
				if (_players.Remove(objectId, out player) == false)
					return;

                Map.ApplyLeave(player);
                player.Room = null;

                // 퇴장한 플레이어의 클라이언트에 퇴장 정보 전송
                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
					player.Session.Send(leavePacket);
				}
			}
            // 몬스터의 GameRoom 퇴장 및 스폰 해제 처리
            else if (type == GameObjectType.Monster)
			{
				Monster monster = null;
				if (_monsters.Remove(objectId, out monster) == false)
					return;

				monster.Room = null;
				Map.ApplyLeave(monster);
			}
            // 투사체의 GameRoom 퇴장 및 스폰 해제 처리
            else if (type == GameObjectType.Projectile)
			{
				Projectile projectile = null;
				if (_projectiles.Remove(objectId, out projectile) == false)
					return;

				projectile.Room = null;
			}

            // 퇴장한 플레이어를 포함한 오브젝트들의 Despawn을 다른 플레이어들에게 전송
            {
                S_Despawn despawnPacket = new S_Despawn();
				despawnPacket.ObjectIds.Add(objectId);
				foreach (Player p in _players.Values)
				{
					if (p.Id != objectId)
						p.Session.Send(despawnPacket);
				}
			}
		}

		public void HandleMove(Player player, C_Move movePacket)
		{
			if (player == null)
				return;

			PositionInfo movePosInfo = movePacket.PosInfo;
			ObjectInfo info = player.Info;

			if (movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
			{
				if (Map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
					return;
			}

			info.PosInfo.State = movePosInfo.State;
			info.PosInfo.MoveDir = movePosInfo.MoveDir;
			Map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));

			S_Move resMovePacket = new S_Move();
			resMovePacket.ObjectId = player.Info.ObjectId;
			resMovePacket.PosInfo = movePacket.PosInfo;

			Broadcast(resMovePacket);
		}

		public void HandleSkill(Player player, C_Skill skillPacket)
		{
			if (player == null)
				return;

			ObjectInfo info = player.Info;
			if (info.PosInfo.State != CreatureState.Idle)
				return;

			info.PosInfo.State = CreatureState.Skill;
			S_Skill skill = new S_Skill() { Info = new SkillInfo() };
			skill.ObjectId = info.ObjectId;
			skill.Info.SkillId = skillPacket.Info.SkillId;
			Broadcast(skill);

			Data.Skill skillData = null;
			if (DataManager.SkillDict.TryGetValue(skillPacket.Info.SkillId, out skillData) == false)
				return;

			switch (skillData.skillType)
			{
				case SkillType.SkillAuto:
					{
						Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
						GameObject target = Map.Find(skillPos);
						if (target != null)
						{
							Console.WriteLine("Hit GameObject !");
						}
					}
					break;
				case SkillType.SkillProjectile:
					{
						Arrow arrow = ObjectManager.Instance.Add<Arrow>();
						if (arrow == null)
							return;

						arrow.Owner = player;
						arrow.Data = skillData;
						arrow.PosInfo.State = CreatureState.Moving;
						arrow.PosInfo.MoveDir = player.PosInfo.MoveDir;
						arrow.PosInfo.PosX = player.PosInfo.PosX;
						arrow.PosInfo.PosY = player.PosInfo.PosY;
						arrow.Speed = skillData.projectile.speed;
						Push(EnterGame, arrow);
					}
					break;
			}
		}

		// 아래의 2개의 함수는 GameRoom 인스턴스에서 호출하면 문제없으므로 따로 수정하지 않음
		public Player FindPlayer(Func<GameObject, bool> condition)
		{
			foreach (Player player in _players.Values)
			{
				if (condition.Invoke(player))
					return player;
			}

			return null;
		}

		public void Broadcast(IMessage packet)
		{
			foreach (Player p in _players.Values)
			{
				p.Session.Send(packet);
			}
		}
	}
}
