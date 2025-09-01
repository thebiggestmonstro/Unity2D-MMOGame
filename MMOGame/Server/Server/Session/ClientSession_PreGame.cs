using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.DB;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server
{
    public partial class ClientSession : PacketSession
    {
        public void HandleLogin(C_Login loginPacket)
        {
            // 클라이언트의 상태 체크
            if (ServerState != PlayerServerState.ServerStateLogin)
                return;

            using (AppDbContext db = new AppDbContext())
            {
                AccountDb findAccount = db.Accounts
                    .Include(a => a.Players)
                    .Where(a => a.AccountName == loginPacket.UnigueId).FirstOrDefault();

                if (findAccount != null)
                {
                    S_Login loginOk = new S_Login() { LoginOk = 1 };
                    Send(loginOk);
                }
                else
                {
                    AccountDb newAccount = new AccountDb() { AccountName = loginPacket.UnigueId };
                    db.Accounts.Add(newAccount);
                    db.SaveChanges();

                    S_Login loginOk = new S_Login() { LoginOk = 1 };
                    Send(loginOk);
                }
            }
        }
    }
}
