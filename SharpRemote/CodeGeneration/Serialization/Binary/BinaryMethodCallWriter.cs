using System;
using System.IO;
using System.Text;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryMethodCallWriter
		: IMethodCallWriter
	{
		private readonly BinaryWriter _writer;

		public BinaryMethodCallWriter(Stream stream, ulong grainId, string methodName, ulong rpcId)
		{
			_writer = new BinaryWriter(stream, Encoding.UTF8, true);
			_writer.Write((byte)MessageType2.Call);
			_writer.Write(grainId);
			_writer.Write(methodName);
			_writer.Write(rpcId);
		}

		public void Dispose()
		{
			_writer.Dispose();
		}

		public void WriteArgument(object value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(sbyte value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(byte value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(ushort value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(short value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(uint value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(int value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(ulong value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(long value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(float value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(double value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(decimal value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(DateTime value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(string value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}

		public void WriteArgument(byte[] value)
		{
			BinarySerializer2.WriteValue(_writer, value);
		}
	}
}