﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     This class is responsible for measuring the average latency of a <see cref="ILatency.Roundtrip()" />
	///     invocation. It can be used by installing a <see cref="ILatency" /> proxy on the side that wants to
	///     measure the latency and a <see cref="Latency" /> servant on the other side.
	/// </summary>
	internal sealed class LatencyMonitor
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly string _endPointName;

		private readonly TimeSpan _interval;
		private readonly ILatency _latencyGrain;
		private readonly RingBuffer<TimeSpan> _measurements;
		private readonly bool _performLatencyMeasurements;
		private readonly object _syncRoot;
		private volatile bool _isDisposed;
		private TimeSpan _roundTripTime;

		private Task _task;
		private EndPoint _localEndPoint;
		private EndPoint _remoteEndPoint;

		/// <summary>
		///     Initializes this latency monitor with the given interval and number of samples over which
		///     the average latency is determined.
		/// </summary>
		/// <param name="latencyGrain"></param>
		/// <param name="interval"></param>
		/// <param name="numSamples"></param>
		/// <param name="performLatencyMeasurements"></param>
		/// <param name="endPointName"></param>
		/// <param name="localEndPoint"></param>
		/// <param name="remoteEndPoint"></param>
		public LatencyMonitor(
			ILatency latencyGrain,
			TimeSpan interval,
			int numSamples,
			bool performLatencyMeasurements,
			string endPointName = null,
			EndPoint localEndPoint = null,
			EndPoint remoteEndPoint = null
		)
		{
			if (latencyGrain == null) throw new ArgumentNullException(nameof(latencyGrain));
			if (interval < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(interval), "A positive interval must be given");
			if (numSamples < 1) throw new ArgumentOutOfRangeException(nameof(numSamples), "1 or more samples must be specified");

			_syncRoot = new object();
			_interval = interval;
			_performLatencyMeasurements = performLatencyMeasurements;
			_latencyGrain = latencyGrain;
			_measurements = new RingBuffer<TimeSpan>(numSamples);
			_endPointName = endPointName;
			_localEndPoint = localEndPoint;
			_remoteEndPoint = remoteEndPoint;
		}

		/// <summary>
		///     Initializes this latency monitor with the given interval and number of samples over which
		///     the average latency is determined.
		/// </summary>
		/// <param name="latencyGrain"></param>
		/// <param name="settings"></param>
		/// <param name="endPointName"></param>
		/// <param name="localEndPoint"></param>
		/// <param name="remoteEndPoint"></param>
		public LatencyMonitor(ILatency latencyGrain,
		                      LatencySettings settings,
		                      string endPointName = null,
		                      EndPoint localEndPoint = null,
		                      EndPoint remoteEndPoint = null)
			: this(latencyGrain,
			       settings.Interval,
			       settings.NumSamples,
			       settings.PerformLatencyMeasurements,
			       endPointName, localEndPoint, remoteEndPoint)
		{
		}

		/// <summary>
		///     The average roundtrip time of a <see cref="ILatency.Roundtrip()" /> call.
		///     Can be used to determine the base overhead of the remoting system.
		/// </summary>
		public TimeSpan RoundtripTime
		{
			get
			{
				lock (_syncRoot)
				{
					return _roundTripTime;
				}
			}
		}

		/// <summary>
		///     Whether or not this latency monitor has been disposed of.
		/// </summary>
		public bool IsDisposed => _isDisposed;

		/// <summary>
		///     Whether or not <see cref="Start()" /> has been called (and <see cref="Stop()" /> has not since then).
		/// </summary>
		public bool IsStarted { get; private set; }

		public void Dispose()
		{
			Stop();
			_isDisposed = true;
		}

		/// <summary>
		///     Starts this latency monitor, e.g. begins measuring the latency.
		/// </summary>
		public void Start()
		{
			IsStarted = true;
			if (_performLatencyMeasurements)
			{
				_task = new Task(MeasureLatencyLoop, TaskCreationOptions.LongRunning);
				_task.Start();
			}
		}

		/// <summary>
		///     Stops the latency monitor from perform any further measurements.
		/// </summary>
		public void Stop()
		{
			IsStarted = false;
			_task = null;
		}

		private void MeasureLatencyLoop()
		{
			var sw = new Stopwatch();
			while (IsStarted)
			{
				TimeSpan toSleep;
				if (!MeasureLatency(sw, out toSleep))
					break;

				if (toSleep > TimeSpan.Zero) Thread.Sleep(toSleep);
			}
		}

		/// <summary>
		///     Measures and stores the current latencyGrain and returns the amount of time
		///     the calling thread should sleep in order to repeat measurements at
		///     <see cref="_interval" />.
		/// </summary>
		/// <param name="sw"></param>
		/// <param name="toSleep"></param>
		private bool MeasureLatency(Stopwatch sw, out TimeSpan toSleep)
		{
			try
			{
				sw.Restart();
				_latencyGrain.Roundtrip();
				sw.Stop();
				var rtt = sw.Elapsed;

				_measurements.Enqueue(rtt);
				var averageRtt = TimeSpan.FromTicks((long) ((double) _measurements.Sum(x => x.Ticks) / _measurements.Length));

				lock (_syncRoot)
				{
					_roundTripTime = averageRtt;
				}

				if (Log.IsDebugEnabled)
					Log.DebugFormat("{0}: {1} to {2}, current RTT: {3:F1}ms, avg. RTT: {4:F1}ms",
					                _endPointName,
					                _localEndPoint,
					                _remoteEndPoint,
					                rtt.TotalMilliseconds,
					                averageRtt.TotalMilliseconds
					               );

				toSleep = _interval - rtt;
				return true;
			}
			catch (NotConnectedException)
			{
				toSleep = TimeSpan.Zero;
				return false;
			}
			catch (ConnectionLostException)
			{
				toSleep = TimeSpan.Zero;
				return false;
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception while measureing latency: {0}", e);
				toSleep = _interval;
				return true;
			}
		}
	}
}