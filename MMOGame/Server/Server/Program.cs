using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Utils;
using ServerCore;
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
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

			_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
			Console.WriteLine("Listening...");

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
