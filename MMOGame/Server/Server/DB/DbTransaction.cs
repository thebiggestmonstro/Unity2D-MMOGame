using Microsoft.EntityFrameworkCore;
using Server.Game;
using Server.Game.Job;
using Server.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.DB
{
    public class DbTransaction : JobSerializer
    {
        public static DbTransaction Instance { get; } = new DbTransaction();

        // 절차는 다음과 같음
        // GameRoom에서 Db로 Job을 전달 -> Db에서 Job을 처리 -> 처리한 결과에 따라 GameRoom에서 작업

        // (1) 한번에 모든 절차를 처리하는 함수
        public static void SavePlayerStatus_AllInOne(Player player, GameRoom room)
        {
            if (player == null || room == null)
                return;

            // GameRoom에서 Job 설정
            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;

            // Db로 Job 전달
            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(playerDb).State = EntityState.Unchanged;
                    db.Entry(playerDb).Property(nameof(PlayerDb.Hp)).IsModified = true;
                    bool success = db.SaveChangesEx();
                    if (success)
                    {
                        // Job 처리 결과에 따라 GameRoom에서 작업
                        room.Push(() => Console.WriteLine($"Saved Hp : {playerDb.Hp}"));
                    }
                }
            });
        }

        // (2) 단계별로 나눠서 처리하는 함수
        // (2 - 1) GameRoom에서 Job 설정
        public static void SavePlayerStatus_Step1(Player player, GameRoom room)
        {
            if (player == null || room == null)
                return;

            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;
            Instance.Push<PlayerDb, GameRoom>(SavePlayerStatus_Step2, playerDb, room);
        }

        // (2 - 2) Db로 Job 전달하여 처리, 처리한 결과에 따라 GameRoom에서 작업 
        public static void SavePlayerStatus_Step2(PlayerDb playerDb, GameRoom room)
        {
            using (AppDbContext db = new AppDbContext())
            {
                db.Entry(playerDb).State = EntityState.Unchanged;
                db.Entry(playerDb).Property(nameof(PlayerDb.Hp)).IsModified = true;
                bool success = db.SaveChangesEx();
                if (success)
                {
                    room.Push(SavePlayerStatus_Step3, playerDb.Hp);
                }
            }
        }

        // (2 - 3) GameRoom에서 Db의 처리 결과에 따라 수행할 함수
        public static void SavePlayerStatus_Step3(int hp)
        {
            Console.WriteLine($"Saved Hp : {hp}");
        }
    }
}
