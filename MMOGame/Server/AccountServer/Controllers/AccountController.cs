using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountServer.DB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedDB;

namespace AccountServer.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		AppDbContext _context;
        SharedDbContext _shared;

        public AccountController(AppDbContext context, SharedDbContext shared)
        {
            _context = context;
            _shared = shared;
        }

        [HttpPost]
		[Route("create")]
		public CreateAccountPacketRes CreateAccount([FromBody] CreateAccountPacketReq req)
		{
			CreateAccountPacketRes res = new CreateAccountPacketRes();

			AccountDb account = _context.Accounts
									.AsNoTracking()
									.Where(a => a.AccountName == req.AccountName)
									.FirstOrDefault();

			if (account == null)
			{
				_context.Accounts.Add(new AccountDb()
				{
					AccountName = req.AccountName,
					Password = req.Password
				});

				bool success = _context.SaveChangesEx();
				res.CreateOk = success;
			}
			else
			{
				res.CreateOk = false;
			}

			return res;
		}

		[HttpPost]
		[Route("login")]
		public LoginAccountPacketRes LoginAccount([FromBody] LoginAccountPacketReq req)
		{
			LoginAccountPacketRes res = new LoginAccountPacketRes();

			AccountDb account = _context.Accounts
				.AsNoTracking()
				.Where(a => a.AccountName == req.AccountName && a.Password == req.Password)
				.FirstOrDefault();

			if (account == null)
			{
				res.LoginOk = false;
			}
			else
			{
				res.LoginOk = true;

				// 토큰 발급
				DateTime expired = DateTime.UtcNow; // 절대시간으로 토큰 만료시간 설정
				expired.AddSeconds(600);			// 600초 후에 만료

                TokenDb tokenDb = _shared.Tokens.Where(t => t.AccountDbId == account.AccountDbId).FirstOrDefault();

                // 기존의 토큰이 존재하는 경우
                if (tokenDb != null)
                {
                    tokenDb.Token = new Random().Next(Int32.MinValue, Int32.MaxValue);
                    tokenDb.Expired = expired;
                    _shared.SaveChangesEx();
                }
                // 토큰이 존재하지 않는 경우
                else
                {
                    tokenDb = new TokenDb()
                    {
                        AccountDbId = account.AccountDbId,
                        Token = new Random().Next(Int32.MinValue, Int32.MaxValue),
                        Expired = expired
                    };
                    _shared.Add(tokenDb);
                    _shared.SaveChangesEx();
                }

                res.AccountId = account.AccountDbId;
                res.Token = tokenDb.Token;
                res.ServerList = new List<ServerInfo>();

                foreach (ServerDb serverDb in _shared.Servers)
                {
                    res.ServerList.Add(new ServerInfo()
                    {
                        Name = serverDb.Name,
                        IpAddress = serverDb.IpAddress,
                        Port = serverDb.Port,
                        BusyScore = serverDb.BusyScore
                    });
                }
            }

			return res;
		}
	}
}
