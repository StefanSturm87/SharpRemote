﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Test.CodeGeneration.Serialization;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using log4net.Core;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class OutOfProcessSiloTest
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			TestLogger.EnableConsoleLogging(Level.Error);
			TestLogger.SetLevel<OutOfProcessSilo>(Level.Info);
			TestLogger.SetLevel<SocketRemotingEndPoint>(Level.Info);
			TestLogger.SetLevel<AbstractSocketRemotingEndPoint>(Level.Info);
		}

		[Test]
		[NUnit.Framework.Description("Verifies that starting the default host process succeeds")]
		public void TestStart1()
		{
			using (var silo = new OutOfProcessSilo())
			{
				silo.IsProcessRunning.Should().BeFalse();
				silo.Start();

				silo.IsProcessRunning.Should().BeTrue();
				silo.HasProcessFailed.Should().BeFalse();
			}
		}

		[Test]
		[NUnit.Framework.Description("Verifies that starting a non-existant executable throws")]
		public void TestStart2()
		{
			using (var silo = new OutOfProcessSilo("Doesntexist.exe"))
			{
				silo.IsProcessRunning.Should().BeFalse();

				new Action(silo.Start)
					.ShouldThrow<FileNotFoundException>()
					.WithMessage("The system cannot find the file specified");

				silo.IsProcessRunning.Should().BeFalse();
				silo.HasProcessFailed.Should().BeFalse();

				new Action(() => silo.CreateGrain<IVoidMethodInt32Parameter>())
					.ShouldThrow<NotConnectedException>();
			}
		}

		[Test]
		[NUnit.Framework.Description("Verifies that starting a non executable throws")]
		public void TestStart3()
		{
			using (var silo = new OutOfProcessSilo("SharpRemote.dll"))
			{
				silo.IsProcessRunning.Should().BeFalse();

				new Action(silo.Start)
					.ShouldThrow<Win32Exception>()
					.WithMessage("The specified executable is not a valid application for this OS platform.");

				silo.IsProcessRunning.Should().BeFalse();
				silo.HasProcessFailed.Should().BeFalse();

				new Action(() => silo.CreateGrain<IVoidMethodInt32Parameter>())
					.ShouldThrow<NotConnectedException>();
			}
		}

		[Test]
		public void TestCreateGrain1()
		{
			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				var proxy = silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
				proxy.Value.Should().Be("Foobar");
			}
		}

		[Test]
		public void TestCtor1()
		{
			using (var silo = new OutOfProcessSilo())
			{
				silo.IsDisposed.Should().BeFalse();
				silo.HasProcessFailed.Should().BeFalse();
				silo.IsProcessRunning.Should().BeFalse();
			}
		}

		[Test]
		[NUnit.Framework.Description("Verifies that specifying a null executable name is not allowed")]
		public void TestCtor2()
		{
			new Action(() => new OutOfProcessSilo(null))
				.ShouldThrow<ArgumentNullException>();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that specifying an empty executable name is not allowed")]
		public void TestCtor3()
		{
			new Action(() => new OutOfProcessSilo(""))
				.ShouldThrow<ArgumentException>();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that specifying a whitespace executable name is not allowed")]
		public void TestCtor4()
		{
			new Action(() => new OutOfProcessSilo("	"))
				.ShouldThrow<ArgumentException>();
		}

		[Test]
		public void TestDispose()
		{
			OutOfProcessSilo silo;
			using (silo = new OutOfProcessSilo())
			{
				silo.Start();
				silo.IsProcessRunning.Should().BeTrue();
			}

			silo.IsDisposed.Should().BeTrue();
			silo.IsProcessRunning.Should().BeFalse();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that a crash of the host process is detected when it happens while a method call")]
		public void TestFailureDetection1()
		{
			using (var silo = new OutOfProcessSilo())
			{
				OutOfProcessSiloFaultReason? reason = null;
				silo.OnFaultDetected += x => reason = x;
				silo.Start();

				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof (KillsProcess));
				new Action(proxy.Do)
					.ShouldThrow<ConnectionLostException>("Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");

				silo.HasProcessFailed.Should().BeTrue("Because an aborted thread that is currently invoking a remote method call should cause SharpRemote to kill the host process and report failure");
				silo.IsProcessRunning.Should().BeFalse();
				reason.Should().Be(OutOfProcessSiloFaultReason.ConnectionFailure);
			}
		}

		[Test]
		[NUnit.Framework.Description("Verifies that an abortion of the executing thread of a remote method invocation is detected and that it causes a connection loss")]
		public void TestFailureDetection2()
		{
			using (var silo = new OutOfProcessSilo())
			{
				OutOfProcessSiloFaultReason? reason = null;
				silo.OnFaultDetected += x => reason = x;
				silo.Start();

				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof(KillsProcess));
				new Action(proxy.Do)
					.ShouldThrow<ConnectionLostException>("Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");

				silo.HasProcessFailed.Should().BeTrue("Because an unexpected exit of the host process counts as a failure");
				silo.IsProcessRunning.Should().BeFalse();
				reason.Should().Be(OutOfProcessSiloFaultReason.ConnectionFailure);
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[NUnit.Framework.Description("Verifies that a complete deadlock of the important remoting threads is detected")]
		public void TestFailureDetection3()
		{
			var settings = new HeartbeatSettings
				{
					ReportSkippedHeartbeatsAsFailureWithDebuggerAttached = true,
					Interval = TimeSpan.FromMilliseconds(100),
					SkippedHeartbeatThreshold = 4
				};
			using (var silo = new OutOfProcessSilo(heartbeatSettings: settings))
			{
				OutOfProcessSiloFaultReason? reason = null;
				silo.OnFaultDetected += x => reason = x;
				silo.Start();

				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof(DeadlocksProcess));
				new Action(() =>
					{
						Task.Factory.StartNew(proxy.Do, TaskCreationOptions.LongRunning)
						    .Wait(TimeSpan.FromSeconds(10))
						    .Should().BeTrue("Because the silo should've detected the deadlock in time");
					})
					.ShouldThrow<ConnectionLostException>("Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");

				silo.HasProcessFailed.Should().BeTrue("Because the heartbeat mechanism should have detected that the endpoint doesn't respond anymore");
				silo.IsProcessRunning.Should().BeFalse();
				reason.Should().Be(OutOfProcessSiloFaultReason.HeartbeatFailure);
			}
		}

		[Test]
		[NUnit.Framework.Description("Verifies that the create method uses the custom type resolver, if specified, to resolve types")]
		public void TestCreate()
		{
			var customTypeResolver = new CustomTypeResolver1();
			using (var silo = new OutOfProcessSilo(customTypeResolver: customTypeResolver))
			{
				silo.Start();

				customTypeResolver.GetTypeCalled.Should().Be(0);
				var grain = silo.CreateGrain<IReturnsType>(typeof(ReturnsTypeofString));
				customTypeResolver.GetTypeCalled.Should().Be(0, "because the custom type resolver in this process didn't need to resolve anything yet");

				grain.Do().Should().Be<string>();
				customTypeResolver.GetTypeCalled.Should().Be(1, "Because the custom type resolver in this process should've been used to resolve typeof(string)");
			}
		}

		[Test]
		[LocalTest("")]
		public void TestPerformanceSynchronous()
		{
			var time = TimeSpan.FromSeconds(5);
			var watch = new Stopwatch();
			int num = 0;

			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				var grain = silo.CreateGrain<IGetInt64Property, ReturnsNearlyInt64Max>();

				long sum = 0;

				// Optimization phase
				for (int i = 0; i < 100; ++i)
				{
					unchecked
					{
						sum += grain.Value;
					}
				}

				// Measurement phase
				watch.Start();
				while (watch.Elapsed < time)
				{
					for (int i = 0; i < 100; ++i)
					{
						unchecked
						{
							sum += grain.Value;
						}
					}
					num += 100;
				}
				watch.Stop();

				var numSeconds = watch.Elapsed.TotalSeconds;
				var ops = 1.0 * num / numSeconds;
				Console.WriteLine("Total calls: {0} (sum: {1})", num, sum);
				Console.WriteLine("OP/s: {0:F2}k/s", ops/1000);
				Console.WriteLine("Sent: {0}, {1}/s", FormatSize(silo.NumBytesSent), FormatSize((long) (silo.NumBytesSent/numSeconds)));
				Console.WriteLine("Received: {0}, {1}/s", FormatSize(silo.NumBytesReceived), FormatSize((long)(silo.NumBytesReceived/numSeconds)));
			}
		}

		[Test]
		[LocalTest("There is no point in running these on the CI server")]
		public void TestPerformanceAsynchronous()
		{
			var time = TimeSpan.FromSeconds(5);
			var watch = new Stopwatch();
			int num = 0;

			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				var grain = silo.CreateGrain<IReturnsIntTask, ReturnsIntMaxTask>();

				// Optimization phase
				const int numOptPasses = 100;
				var opts = new Task[numOptPasses];
				for (int i = 0; i < numOptPasses; ++i)
				{
					opts[i] = grain.DoStuff();
				}

				Task.WaitAll(opts);

				// Measurement phase

				const int batchSize = 64;
				var tasks = new Task[batchSize];

				watch.Start();
				while (watch.Elapsed < time)
				{
					for (int i = 0; i < batchSize; ++i)
					{
						tasks[i] = grain.DoStuff();
					}
					Task.WaitAll(tasks);

					num += batchSize;
				}
				watch.Stop();

				var numSeconds = watch.Elapsed.TotalSeconds;
				var ops = 1.0 * num / numSeconds;
				Console.WriteLine("Total calls: {0}", num);
				Console.WriteLine("OP/s: {0:F2}k/s", ops / 1000);
				Console.WriteLine("Sent: {0}, {1}/s", FormatSize(silo.NumBytesSent), FormatSize((long)(silo.NumBytesSent / numSeconds)));
				Console.WriteLine("Received: {0}, {1}/s", FormatSize(silo.NumBytesReceived), FormatSize((long)(silo.NumBytesReceived / numSeconds)));
			}
		}

		private string FormatSize(long numBytesSent)
		{
			const long oneKilobyte = 1024;
			const long oneMegabyte = 1024*oneKilobyte;
			const long oneGigabyte = 1024*oneMegabyte;

			if (numBytesSent > oneGigabyte)
				return string.Format("{0:F2} Gb", 1.0*numBytesSent/oneGigabyte);

			if (numBytesSent > oneMegabyte)
				return string.Format("{0:F2} Mb", 1.0 * numBytesSent / oneMegabyte);

			if (numBytesSent > oneKilobyte)
				return string.Format("{0:F2} Kb", 1.0 * numBytesSent / oneKilobyte);

			return string.Format("{0} bytes", numBytesSent);
		}
	}
}