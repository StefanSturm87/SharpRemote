using System;
using System.IO;
using System.Text;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryMethodResultWriter
		: IMethodResultWriter
	{
		private readonly BinarySerializer2 _serializer;
		private readonly IRemotingEndPoint _endPoint;
		private readonly BinaryWriter _writer;

		public BinaryMethodResultWriter(BinarySerializer2 serializer,
		                                Stream stream,
		                                ulong rpcId,
		                                IRemotingEndPoint endPoint = null)
		{
			_serializer = serializer;
			_endPoint = endPoint;
			_writer = new BinaryWriter(stream, Encoding.UTF8, true);
			_writer.Write((byte)MessageType2.Result);
			_writer.Write(rpcId);
		}

		public void Dispose()
		{
			_writer.Dispose();
		}

		public void WriteFinished()
		{
			throw new NotImplementedException();
		}

		public void WriteResult(object value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(sbyte value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(byte value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(ushort value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(short value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(uint value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(int value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(ulong value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(long value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(float value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(double value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(string value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteResult(byte[] value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteException(Exception e)
		{
			_serializer.WriteObject(_writer, e, _endPoint);
		}
	}
}