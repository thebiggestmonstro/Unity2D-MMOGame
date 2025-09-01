using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.DB;
using Server.Game;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class PacketHandler
{
	public static void C_MoveHandler(PacketSession session, IMessage packet)
	{
		C_Move movePacket = packet as C_Move;
		ClientSession clientSession = session as ClientSession;

		//Console.WriteLine($"C_Move ({movePacket.PosInfo.PosX}, {movePacket.PosInfo.PosY})");

		Player player = clientSession.MyPlayer;
		if (player == null)
			return;

		GameRoom room = player.Room;
		if (room == null)
			return;

		room.Push(room.HandleMove, player, movePacket);
	}

	public static void C_SkillHandler(PacketSession session, IMessage packet)
	{
		C_Skill skillPacket = packet as C_Skill;
		ClientSession clientSession = session as ClientSession;

		Player player = clientSession.MyPlayer;
		if (player == null)
			return;

		GameRoom room = player.Room;
		if (room == null)
			return;

		room.Push(room.HandleSkill, player, skillPacket);
	}

	public static void C_LoginHandler(PacketSession session, IMessage packet)
	{
        C_Login loginPacket = packet as C_Login;
        ClientSession clientSession = session as ClientSession;

		Console.WriteLine($"로그인한 플레이어의 고유 ID : {loginPacket.UnigueId}");

		// TODO : 보안 체크

		// 유효한 ID인지 체크
		using (AppDbContext db = new AppDbContext())
		{
			AccountDb findAccount = db.Accounts
				.Where(a => a.AccountName == loginPacket.UnigueId).FirstOrDefault();

			// 유효하다면, 그대로 클라이언트에 S_Login 패킷 전송
			if (findAccount != null)
			{
				S_Login loginOk = new S_Login() { LoginOk = 1 };
				clientSession.Send(loginOk);
			}
			// 유효하지 않다면, 새롭게 Account 테이블의 데이터를 생성하고 클라이언트에 S_Login 패킷 전송
			else 
			{
				AccountDb newAccount = new AccountDb() { AccountName = loginPacket.UnigueId };
				db.Accounts.Add(newAccount);
				db.SaveChanges();

				S_Login loginOk = new S_Login() { LoginOk = 1 };
				clientSession.Send(loginOk);
            }
		}
    }
}
