﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Couchbase.IO.Strategies.Awaitable
{
    internal static class AwaitableSocketFactory
    {
        public static Func<IConnectionPool, BufferAllocator, SocketAwaitable> GetSocketAwaitable()
        {
            Func<IConnectionPool, BufferAllocator, SocketAwaitable> factory = (p, b) =>
            {
                var connection = p.Acquire();
                var eventArgs = new SocketAsyncEventArgs
                {
                    AcceptSocket = connection.Socket,
                    UserToken = new OperationAsyncState
                    {
                        Connection = connection
                    }
                };
                b.SetBuffer(eventArgs);
                return new SocketAwaitable(eventArgs);
            };
            return factory;
        }
    }
}
