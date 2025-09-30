using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Job;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public partial class GameRoom : JobSerializer
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
			monster.Init(1);
			monster.CellPos = new Vector2Int(5, 5);
			EnterGame(monster);
		}

		public void Update()
		{
			// 저장된 패킷을 처리
			Flush();
		}

        public void EnterGame(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            if (type == GameObjectType.Player)
            {
                Player player = gameObject as Player;
                _players.Add(gameObject.Id, player);
                player.Room = this;

                player.RefreshAdditionalStat();

                Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y));

                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = player.Info;
                    player.Session.Send(enterPacket);

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in _players.Values)
                    {
                        if (player != p)
                            spawnPacket.Objects.Add(p.Info);
                    }

                    foreach (Monster m in _monsters.Values)
                        spawnPacket.Objects.Add(m.Info);

                    foreach (Projectile p in _projectiles.Values)
                        spawnPacket.Objects.Add(p.Info);

                    player.Session.Send(spawnPacket);
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = gameObject as Monster;
                _monsters.Add(gameObject.Id, monster);
                monster.Room = this;

                Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));

                monster.Update();
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = gameObject as Projectile;
                _projectiles.Add(gameObject.Id, projectile);
                projectile.Room = this;

                projectile.Update();
            }

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

				player.OnLeaveGame();
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

				Map.ApplyLeave(monster);
                monster.Room = null;
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
