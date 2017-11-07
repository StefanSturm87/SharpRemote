﻿using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public abstract class AbstractSerializerAcceptanceTest
	{
		protected abstract ISerializer2 Create();

		[Test]
		public void TestEmptyMethodCall([ValueSource("GrainIds")] ulong grainId,
		                                [ValueSource("RpcIds")] ulong rpcId)
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (serializer.CreateMethodInvocationWriter(stream, rpcId, grainId, "Foo"))
				{}

				stream.Position.Should()
				      .BeGreaterThan(expected: 0, because: "because some data must've been written to the stream");
				stream.Position = 0;

				WriteMessage(stream);

				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.RpcId.Should().Be(rpcId);
					reader.GrainId.Should().Be(grainId);
					reader.MethodName.Should().Be("Foo");
				}
			}
		}

		protected abstract string Format(MemoryStream stream);

		private void WriteMessage(MemoryStream stream)
		{
			var formatted = Format(stream);
			TestContext.Out.Write(formatted);
			stream.Position = 0;
		}
	}
}