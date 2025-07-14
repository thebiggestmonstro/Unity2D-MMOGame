using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game.Object;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Room
{
    public class GameRoom
    {
        Map _map = new Map();

        object _lock = new object();
        public int RoomId { get; set; }

        Dictionary<int, Player> _players = new Dictionary<int, Player>();

        public void Init(int mapId)
        {
            _map.LoadMap(mapId);
        }

        public void EnterGame(Player newPlayer)
        {
            if (newPlayer == null)
                return;

            lock (_lock)
            {
                _players.Add(newPlayer.Info.ObjectId, newPlayer);
                newPlayer.Room = this;

                // 플레이어 입장 처리 + 입장한 플레이어에게 다른 플레이어들 Spawn
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = newPlayer.Info;
                    newPlayer.Session.Send(enterPacket);

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in _players.Values)
                    {
                        if (newPlayer != p)
                            spawnPacket.Objects.Add(p.Info);
                    }
                    newPlayer.Session.Send(spawnPacket);
                }

                // 다른 플레이어들에게 입장한 플레이어를 Spawn
                {
                    S_Spawn spawnPacket = new S_Spawn();
                    spawnPacket.Objects.Add(newPlayer.Info);
                    foreach (Player p in _players.Values)
                    {
                        if (newPlayer != p)
                            p.Session.Send(spawnPacket);
                    }
                }
            }
        }

        public void LeaveGame(int playerId)
        {
            lock (_lock)
            {
                Player player = null;
                if (_players.Remove(playerId, out player) == false)
                    return;

                player.Room = null;

                // 플레이어 퇴장
                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    player.Session.Send(leavePacket);
                }

                // 다른 플레이어들로부터 퇴장한 플레이어의 Spawn의 해제를 처리
                {
                    S_Despawn despawnPacket = new S_Despawn();
                    despawnPacket.PlayerIds.Add(player.Info.ObjectId);
                    foreach (Player p in _players.Values)
                    {
                        if (player != p)
                            p.Session.Send(despawnPacket);
                    }
                }
            }
        }

        public void HandleMove(Player player, C_Move movePacket)
        {
            lock (_lock)
            {
                PositionInfo movePosInfo = movePacket.PosInfo;
                ObjectInfo info = player.Info;

                if (movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
                {
                    if (_map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
                        return;
                }

                info.PosInfo.State = movePosInfo.State;
                info.PosInfo.MoveDir = movePosInfo.MoveDir;
                _map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));

                S_Move resMovePacket = new S_Move();
                resMovePacket.PlayerId = player.Info.ObjectId;
                resMovePacket.PosInfo = movePacket.PosInfo;

                Broadcast(resMovePacket);
            }
        }

        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            if (player == null)
                return;

            lock (_lock)
            {
                ObjectInfo info = player.Info;
                if (info.PosInfo.State != CreatureState.Idle)
                    return;

                info.PosInfo.State = CreatureState.Skill;

                S_Skill skill = new S_Skill() { Info = new SkillInfo() };
                skill.PlayerId = info.ObjectId;
                skill.Info.SkillId = skillPacket.Info.SkillId;
                Broadcast(skill);

                // 일반 공격
                if (skillPacket.Info.SkillId == 1)
                {
                    // 피격 판정
                    Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
                    Player target = _map.Find(skillPos);
                    if (target != null)
                    {
                        Console.WriteLine("Hit Player !");
                    }
                }
                // 원거리 공격
                else if (skillPacket.Info.SkillId == 2)
                {

                }
            }
        }

        public void Broadcast(IMessage packet)
        {
            lock (_lock)
            {
                foreach (Player p in _players.Values)
                {
                    p.Session.Send(packet);
                }
            }
        }
    }
}
