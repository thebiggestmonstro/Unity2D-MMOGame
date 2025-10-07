using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Game
{
	public partial class GameRoom : JobSerializer
	{
		public const int VisionCells = 5;
		public int RoomId { get; set; }

		Dictionary<int, Player> _players = new Dictionary<int, Player>();
		Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
		Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();

		public Zone[,] Zones { get; private set; }
		public int ZoneCells { get; private set; }

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

		// 해당 좌표가 위치해 있는 Zone을 가져오는 함수
		public Zone GetZone(Vector2Int cellPos)
		{
			int x = (cellPos.x - Map.MinX) / ZoneCells;
			int y = (Map.MaxY - cellPos.y) / ZoneCells;

			if (x < 0 || x >= Zones.GetLength(1))
				return null;

			if (y < 0 || y >= Zones.GetLength(0))
				return null;

			return Zones[y, x];
		}

		// 또는 static 함수로 수정하지 않고 다음의 방법을 사용
		public void Init(int mapId, int zoneCells)
		{ 
			Map.LoadMap(mapId);

			// Zone
			ZoneCells = zoneCells;
			int countY = (Map.SizeY + zoneCells - 1) / zoneCells;
			int countX = (Map.SizeX + zoneCells - 1) / zoneCells;
			Zones = new Zone[countY, countX];

			for (int y = 0; y < countY; y++)
			{
				for (int x = 0; x < countX; x++)
				{
					Zones[y, x] = new Zone(y, x);
				}
			}

			Monster monster = ObjectManager.Instance.Add<Monster>();
			monster.Init(1);
			monster.CellPos = new Vector2Int(5, 5);
			EnterGame(monster);
		}

		public void Update()
		{
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
				GetZone(player.CellPos).Players.Add(player);

				// VisionCube를 도입하기 이전의 Spawn 패킷을 나에게 전송하는 로직을 제거
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = player.Info;
                    player.Session.Send(enterPacket);

					player.Vision.Update();
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = gameObject as Monster;
                _monsters.Add(gameObject.Id, monster);
                monster.Room = this;

                GetZone(monster.CellPos).Monsters.Add(monster);
                Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));

                monster.Update();
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = gameObject as Projectile;
                _projectiles.Add(gameObject.Id, projectile);
                projectile.Room = this;

                GetZone(projectile.CellPos).Projectiles.Add(projectile);
                projectile.Update();
            }
        }

        public void LeaveGame(int objectId)
		{
			GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

            if (type == GameObjectType.Player)
			{
				Player player = null;
				if (_players.Remove(objectId, out player) == false)
					return;

				GetZone(player.CellPos).Players.Remove(player);

				player.OnLeaveGame();
                Map.ApplyLeave(player);
                player.Room = null;

                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
					player.Session.Send(leavePacket);
				}
			}
            else if (type == GameObjectType.Monster)
			{
                Monster monster = null;
                if (_monsters.Remove(objectId, out monster) == false)
                    return;

                GetZone(monster.CellPos).Monsters.Remove(monster);
                Map.ApplyLeave(monster);
                monster.Room = null;
            }
            else if (type == GameObjectType.Projectile)
			{
				Projectile projectile = null;
				if (_projectiles.Remove(objectId, out projectile) == false)
					return;

                GetZone(projectile.CellPos).Projectiles.Remove(projectile);
                projectile.Room = null;
			}
		}

		public Player FindPlayer(Func<GameObject, bool> condition)
		{
			foreach (Player player in _players.Values)
			{
				if (condition.Invoke(player))
					return player;
			}

			return null;
		}

		public void Broadcast(Vector2Int pos, IMessage packet)
		{
			List<Zone> zones = GetAdjacentZones(pos);

			foreach (Player p in zones.SelectMany(z => z.Players))
			{
                int dx = p.CellPos.x - pos.x;
                int dy = p.CellPos.y - pos.y;

                // VisionCube로 판단할 수 있는 시야각에 플레이어가 존재하는지 판단
                if (Math.Abs(dx) > GameRoom.VisionCells)
                    continue;
                if (Math.Abs(dy) > GameRoom.VisionCells)
                    continue;

				// 시야각에 들어오는 다른 플레이어들에게 브로드캐스트
                p.Session.Send(packet);
			}
		}

		// 인접한 Zone을 찾는 함수
		public List<Zone> GetAdjacentZones(Vector2Int cellPos, int cells = VisionCells)
		{ 
			HashSet<Zone> zones = new HashSet<Zone>();

			int[] delta = new int[2] { -cells, +cells };
			foreach (int dy in delta)
			{
				foreach (int dx in delta)
				{
					int y = cellPos.y + dy;
					int x = cellPos.x + dx;
					Zone zone = GetZone(new Vector2Int(x, y));
					if (zone == null)
						continue;

					zones.Add(zone);
				}
			}

			return zones.ToList();
		}
	}
}
