﻿using System;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TTransport"></typeparam>
	internal abstract class AbstractNamedPipeEndPoint<TTransport>
		: AbstractBinaryStreamEndPoint<TTransport>
		where TTransport : PipeStream
	{
		protected const string Localhost = ".";

		private NamedPipeEndPoint _localEndPoint;
		private NamedPipeEndPoint _remoteEndPoint;

		internal AbstractNamedPipeEndPoint(string name,
			EndPointType type,
		                                   IAuthenticator clientAuthenticator,
			IAuthenticator serverAuthenticator,
		                                   ITypeResolver customTypeResolver,
			Serializer serializer,
		                                   HeartbeatSettings heartbeatSettings,
			LatencySettings latencySettings,
		                                   EndPointSettings endPointSettings)
			: base(
				new GrainIdGenerator(type), name, type, clientAuthenticator, serverAuthenticator, customTypeResolver, serializer, heartbeatSettings,
				latencySettings, endPointSettings)
		{
		}

		protected override EndPoint GetRemoteEndPointOf(TTransport socket)
		{
			return _remoteEndPoint;
		}

		/// <summary>
		/// 
		/// </summary>
		public new NamedPipeEndPoint LocalEndPoint
		{
			get { return _localEndPoint; }
			protected set { _localEndPoint = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public new NamedPipeEndPoint RemoteEndPoint
		{
			get { return _remoteEndPoint; }
		}

		protected override EndPoint InternalLocalEndPoint
		{
			get { return _localEndPoint; }
		}

		protected override EndPoint InternalRemoteEndPoint
		{
			get { return _remoteEndPoint; }
			set { _remoteEndPoint = (NamedPipeEndPoint)value; }
		}

		protected override void Send(TTransport socket, byte[] data, int offset, int size)
		{
			socket.Write(data, offset, size);
		}

		protected override bool SynchronizedRead(TTransport socket, byte[] buffer, out SocketError err)
		{
			return SynchronizedRead(socket, buffer, TimeSpan.MaxValue, out err);
		}

		protected override bool SynchronizedRead(TTransport socket, byte[] buffer, TimeSpan timeout, out SocketError err)
		{
			int read = socket.Read(buffer, 0, buffer.Length);
			if (read != buffer.Length)
			{
				err = SocketError.ConnectionAborted;
				return false;
			}

			err = SocketError.Success;
			return true;
		}

		protected override bool SynchronizedWrite(TTransport socket, byte[] data, int length, out SocketError err)
		{
			try
			{
				socket.Write(data, 0, length);
				err = SocketError.Success;
				return true;
			}
			catch (Exception)
			{
				err = SocketError.Fault;
				return false;
			}
		}

		protected override bool SendGoodbye(TTransport socket, long waitTime, TimeSpan timeSpan)
		{
			return false;
		}
	}
}