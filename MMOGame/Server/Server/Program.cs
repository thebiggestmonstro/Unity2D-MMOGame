using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Utils;
using ServerCore;
using SharedDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
	class Program
	{
		static Listener _listener = new Listener();

		// 게임 관련 로직을 수행하는 스레드
		static void GameLogicTask()
		{
			while (true)
			{
				GameLogic.Instance.Update();
				Thread.Sleep(0);    // Job을 전부 수행했다면 Sleep을 수행 -> 해당 작업을 통해 게임 로직 스레드에 대한 CPU의 부적절한 낭비를 방지
            }
		}

		// DB 관련 로직을 수행하는 메인 스레드
		static void DbTask()
		{
            while (true)
            {
                DbTransaction.Instance.Flush();
				Thread.Sleep(0);	// Job을 전부 수행했다면 Sleep을 수행 -> 해당 작업을 통해 메인 스레드에 대한 CPU의 부적절한 낭비를 방지
            }
        }

		// 네트워크 관련 로직을 수행하는 스레드
		static void NetworkTask()
		{
			while (true)
			{ 
				List<ClientSession> sessions = SessionManager.Instance.GetSessions();
				
				foreach (ClientSession session in sessions)
				{
					session.FlushSend();
				}

				Thread.Sleep(0);
			}
		}

        // 10초마다 GameServer를 통해 SharedDB를 갱신하는 함수
        static void StartServerInfoTask()
        {
            var t = new System.Timers.Timer();
            t.AutoReset = true;

            t.Elapsed += new System.Timers.ElapsedEventHandler((s, e) =>
            {
                using (SharedDbContext shared = new SharedDbContext())
                {
                    ServerDb serverDb = shared.Servers.Where(s => s.Name == Name).FirstOrDefault();
                    if (serverDb != null)
                    {
                        serverDb.IpAddress = IpAddress;
                        serverDb.Port = Port;
                        serverDb.BusyScore = SessionManager.Instance.GetBusyScore();
                        shared.SaveChangesEx();
                    }
                    else
                    {
                        serverDb = new ServerDb()
                        {
                            Name = Program.Name,
                            IpAddress = Program.IpAddress,
                            Port = Program.Port,
                            BusyScore = SessionManager.Instance.GetBusyScore()
                        };
                        shared.Servers.Add(serverDb);
                        shared.SaveChangesEx();
                    }
                }
            });

            t.Interval = 10 * 1000;
            t.Start();
        }

        public static string Name { get; } = "OutLand";
        public static int Port { get; } = 7777;
        public static string IpAddress { get; set; }

        // ServerDB를 추가 / 제거하는 함수를 넣어 리팩토링이 충분히 가능

        static void Main(string[] args)
		{
			ConfigManager.LoadConfig();
			DataManager.LoadData();

            GameLogic.Instance.Push(() =>
            {
                GameRoom room = GameLogic.Instance.Add(1);
            });

            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, Port);

            IpAddress = ipAddr.ToString();

            _listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
            Console.WriteLine("Listening...");

            StartServerInfoTask();

            // DbTask를 통한 DB 관련 로직을 수행하는 스레드 생성 및 실행
            {
                Thread t = new Thread(DbTask);
				t.Name = "DB";
				t.Start();
			}

			// NetworkTask를 통한 네트워크 관련 로직을 수행하는 스레드 생성 및 실행
            {
				Thread t = new Thread(NetworkTask);
				t.Name = "Network";
				t.Start();
            }

			//  GameLogicTask를 통한 GameRoom 관련 로직을 메인 스레드에서 수행
			Thread.CurrentThread.Name = "GameLogic";
			GameLogicTask();
        }
	}
}
