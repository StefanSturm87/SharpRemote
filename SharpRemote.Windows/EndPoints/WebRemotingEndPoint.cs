﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace SharpRemote.EndPoints
{
	internal sealed class WebRemotingEndPoint
		: AbstractEndPoint
		  , IRemotingEndPoint
		  , IEndPointChannel
	{
		public Task<MemoryStream> CallRemoteMethodAsync(ulong servantId, string interfaceType, string methodName, MemoryStream arguments)
		{
			throw new NotImplementedException();
		}

		public MemoryStream CallRemoteMethod(ulong servantId, string interfaceType, string methodName, MemoryStream arguments)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public string Name
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsConnected
		{
			get { throw new NotImplementedException(); }
		}

		public void Disconnect()
		{
			throw new NotImplementedException();
		}

		public T CreateProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		public T GetProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			throw new NotImplementedException();
		}

		public T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		public IServant GetExistingOrCreateNewServant<T>(T subject) where T : class
		{
			throw new NotImplementedException();
		}
	}
}