using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;
using Google.Protobuf.Protocol;
using Google.Protobuf;
using Server.Game;
using Server.Data;

namespace Server
{
	public partial class ClientSession : PacketSession
	{
		public Player MyPlayer { get; set; }
		public int SessionId { get; set; }

		public PlayerServerState ServerState { get; private set; } = PlayerServerState.ServerStateLogin;

		object _lock = new object();
		List<ArraySegment<byte>> _reserveQueue = new List<ArraySegment<byte>>();

		// 패킷을 모아서 처리하기 위한 변수
		int _reservedSendBytes = 0;
		long _lastSendTick = 0;

        #region Network
		// 패킷을 단순 예약만 하는 함수
        public void Send(IMessage packet)
		{
			string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
			MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
			ushort size = (ushort)packet.CalculateSize();
			byte[] sendBuffer = new byte[size + 4];
			Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
			Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
			Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);

			lock (_lock)
			{
				_reserveQueue.Add(sendBuffer);
				_reservedSendBytes += sendBuffer.Length;
			}
		}

		// 예약된 패킷을 실제로 Send하는 함수
		public void FlushSend()
		{
			List<ArraySegment<byte>> sendList = null;
			lock (_lock)
			{
				// 마지막 Send 시점으로부터 0.1초가 지나기 이전이고 패킷이 충분히 모이지 않았다면
				long delta = (System.Environment.TickCount64 - _lastSendTick);
				if (delta < 100 && _reservedSendBytes < 10000)
					return;

				// 패킷 모아 보내기
				_reservedSendBytes = 0;
				_lastSendTick = System.Environment.TickCount64;

				sendList = _reserveQueue;
				_reserveQueue = new List<ArraySegment<byte>>();
			}

			Send(sendList);
		}

		public override void OnConnected(EndPoint endPoint)
		{
			{ 
				S_Connected connectedPacket = new S_Connected();
				Send(connectedPacket);
			}

			// 클라이언트가 서버와 연결된 시점부터 5초 간격으로 Ping 전송
			GameLogic.Instance.PushAfter(5000, Ping);
		}

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
            GameLogic.Instance.Push(() =>
            {
				if (MyPlayer == null)
					return;

                GameRoom room = GameLogic.Instance.Find(1);
                room.Push(room.LeaveGame, MyPlayer.Info.ObjectId);
            });

			SessionManager.Instance.Remove(this);
		}

		public override void OnSend(int numOfBytes)
		{
			//Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}

		long _pingpongTick = 0;
		public void Ping()
		{
			// 서버 - 클라간의 Ping 교환이 발생
			if (_pingpongTick > 0)
			{
				long delta = (System.Environment.TickCount64 - _pingpongTick);
				
				// Ping 교환이 30초 이상인 경우 강제 연결 중지
				if (delta > 30 * 1000)
				{
					Console.WriteLine($"Disconnected by PingCheck");
					Disconnect();
					return;
				}
			}

			S_Ping pingPacket = new S_Ping();
			Send(pingPacket);

			// 클라이언트에 5초 간격으로 Ping을 전송
			GameLogic.Instance.PushAfter(5000, Ping);
		}

		public void HandlePong()
		{
			_pingpongTick = System.Environment.TickCount64;
        }

        #endregion
    }
}
