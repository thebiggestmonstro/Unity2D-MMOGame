using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Game
{
    public class VisionCube
    {
        public Player Owner { get; private set; }
        public HashSet<GameObject> PreviousObjects { get; private set; } = new HashSet<GameObject>();

        public VisionCube(Player owner)
        {
            Owner = owner;
        }

        // VisionCube에서 인식할 수 있는 오브젝트들을 가져오는 함수
        public HashSet<GameObject> GatherObjects()
        {
            if (Owner == null || Owner.Room == null)
                return null;

            HashSet<GameObject> objects = new HashSet<GameObject>();

            // Owner의 위치를 기준으로 VisionCube에서 인식할 수 있는 오브젝트를 탐색
            Vector2Int cellPos = Owner.CellPos;
            List<Zone> zones = Owner.Room.GetAdjacentZones(cellPos);

            foreach (Zone zone in zones) 
            {
                foreach (Player player in zone.Players)
                { 
                    int dx = player.CellPos.x - cellPos.x;
                    int dy = player.CellPos.y - cellPos.y;

                    // VisionCube로 판단할 수 있는 시야각에 플레이어가 존재하는지 판단
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if(Math.Abs(dy) > GameRoom.VisionCells)
                        continue;

                    objects.Add(player);
                }

                foreach (Monster monster in zone.Monsters)
                {
                    int dx = monster.CellPos.x - cellPos.x;
                    int dy = monster.CellPos.y - cellPos.y;

                    // VisionCube로 판단할 수 있는 시야각에 몬스터가 존재하는지 판단
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;

                    objects.Add(monster);
                }

                foreach (Projectile projectile in zone.Projectiles)
                {
                    int dx = projectile.CellPos.x - cellPos.x;
                    int dy = projectile.CellPos.y - cellPos.y;

                    // VisionCube로 판단할 수 있는 시야각에 투사체가 존재하는지 판단
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;

                    objects.Add(projectile);
                }
            }

            return objects;
        }

        // 설정한 프레임마다 VisionCube를 확인하는 함수
        public void Update()
        {
            if (Owner == null || Owner.Room == null)
                return;

            HashSet<GameObject> currentObjects = GatherObjects();

            // 기존에 보여지지 않았지만 새롭게 VisionCube에서 보여지는 오브젝트는 Spawn
            List<GameObject> added = currentObjects.Except(PreviousObjects).ToList();
            if (added.Count > 0)
            {
                S_Spawn spawnPacket = new S_Spawn();

                foreach (GameObject gameObject in added)
                {
                    ObjectInfo info = new ObjectInfo();
                    info.MergeFrom(gameObject.Info);
                    spawnPacket.Objects.Add(info);
                }

                Owner.Session.Send(spawnPacket);
            }

            // 기존에 보여졌지만 VisionCube에서 보이지 않는 오브젝트는 Despawn
            List<GameObject> removed = PreviousObjects.Except(currentObjects).ToList();
            if (removed.Count > 0)
            { 
                S_Despawn despawnPacket = new S_Despawn();

                foreach (GameObject gameObject in removed)
                { 
                    despawnPacket.ObjectIds.Add(gameObject.Id);
                }

                Owner.Session.Send(despawnPacket);
            }

            PreviousObjects = currentObjects;
            Owner.Room.PushAfter(100, Update);
        }
    }
}
