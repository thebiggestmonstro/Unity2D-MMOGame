using Google.Protobuf;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class GameRoom
    {
        object _lock = new object();
        public int RoomId { get; set; }

        List<Player> _players = new List<Player>();

        public void EnterGame(Player newPlayer)
        {
            if (newPlayer == null)
                return;

            lock (_lock) 
            {
                _players.Add(newPlayer);
                newPlayer.Room = this;

                // 플레이어 입장 처리 + 입장한 플레이어에게 다른 플레이어들 Spawn
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = newPlayer.Info;
                    newPlayer.Session.Send(enterPacket);

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in _players)
                    {
                        if (newPlayer != p)
                            spawnPacket.Players.Add(p.Info);
                    }
                    newPlayer.Session.Send(spawnPacket);
                }

                // 다른 플레이어들에게 입장한 플레이어를 Spawn
                {
                    S_Spawn spawnPacket = new S_Spawn();
                    spawnPacket.Players.Add(newPlayer.Info);
                    foreach (Player p in _players)
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
                Player player = _players.Find(p => p.Info.PlayerId == playerId);
                if (player == null)
                    return;

                _players.Remove(player);
                player.Room = null;

                // 플레이어 퇴장
                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    player.Session.Send(leavePacket);
                }

                // 다른 플레이어들로부터 퇴장한 플레이어의 Spawn의 해제를 처리
                {
                    S_Despawn despawnPacket = new S_Despawn();
                    despawnPacket.PlayerIds.Add(player.Info.PlayerId);
                    foreach (Player p in _players)
                    { 
                        if(player != p)
                            p.Session.Send(despawnPacket);
                    }
                }
            }
        }

        public void HandleMove(Player player, C_Move movePacket)
        {
            // 클라이언트의 이동 결과를 판단하는 로직은 아직 X

            lock (_lock)
            {
                PlayerInfo info = player.Info;
                info.PosInfo = movePacket.PosInfo;

                S_Move resMovePacket = new S_Move();
                resMovePacket.PlayerId = player.Info.PlayerId;
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
                PlayerInfo info = player.Info;
                if (info.PosInfo.State != CreatureState.Idle)
                    return;

                // 스킬 사용가능 여부를 판단하는 로직은 아직 X

                info.PosInfo.State = CreatureState.Skill;

                S_Skill skill = new S_Skill() { Info = new SkillInfo() };
                skill.PlayerId = info.PlayerId;
                skill.Info.SkillId = 1;
                Broadcast(skill);
            }
        }

        public void Broadcast(IMessage packet)
        {
            lock (_lock) 
            {
                foreach (Player p in _players)
                {
                    p.Session.Send(packet);
                }
            }
        }
    }
}
