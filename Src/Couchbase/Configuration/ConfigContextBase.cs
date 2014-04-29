using Common.Logging;
using Couchbase.Authentication.SASL;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Server.Serialization;
using Couchbase.Core;
using Couchbase.Core.Buckets;
using Couchbase.IO;
using Couchbase.IO.Strategies.Async;
using Couchbase.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Couchbase.Configuration
{
    internal abstract class ConfigContextBase : IConfigInfo
    {
        private readonly ILog _log = LogManager.GetCurrentClassLogger();
        protected IBucketConfig _bucketConfig;
        protected IKeyMapper _keyMapper;
        private readonly DateTime _creationTime;
        protected ClientConfiguration _clientConfig;
        protected readonly List<IServer> _servers = new List<IServer>();
        protected Func<IConnectionPool, IOStrategy> _ioStrategyFactory;
        protected Func<PoolConfiguration, IPEndPoint, IConnectionPool> _connectionPoolFactory;
        private bool _disposed;

        protected ConfigContextBase(IBucketConfig bucketConfig, ClientConfiguration clientConfig)
            : this(bucketConfig, clientConfig, pool => new SocketAsyncStrategy(pool, new PlainTextMechanism(bucketConfig.Name, string.Empty)),
                (config, endpoint) => new DefaultConnectionPool(config, endpoint))
        {
        }

        protected ConfigContextBase(IBucketConfig bucketConfig, ClientConfiguration clientConfig,
            Func<IConnectionPool, IOStrategy> ioStrategyFactory,
            Func<PoolConfiguration, IPEndPoint, IConnectionPool> connectionPoolFactory)
        {
            _clientConfig = clientConfig;
            _ioStrategyFactory = ioStrategyFactory;
            _connectionPoolFactory = connectionPoolFactory;
            _creationTime = DateTime.Now;
            LoadConfig(bucketConfig);///TODO fix resharper warning
        }

        public DateTime CreationTime
        {
            get { return _creationTime; }
        }

        public IBucketConfig BucketConfig
        {
            get { return _bucketConfig; }
        }

        public string BucketName
        {
            get { return _bucketConfig.Name; }
        }

        public ClientConfiguration ClientConfig
        {
            get { return _clientConfig; }
        }

        public BucketTypeEnum BucketType
        {
            get
            {
                BucketTypeEnum bucketType;
                if (!Enum.TryParse(_bucketConfig.BucketType, true, out bucketType))
                {
                    throw new NullConfigException("BucketType is not defined");
                }
                return bucketType;
            }
        }

        public NodeLocatorEnum NodeLocator
        {
            get
            {
                NodeLocatorEnum nodeLocator;
                if (!Enum.TryParse(_bucketConfig.NodeLocator, true, out nodeLocator))
                {
                    throw new NullConfigException("NodeLocator is not defined");
                }
                return nodeLocator;
            }
        }

        protected virtual IPEndPoint GetEndPoint(string hostName, IBucketConfig bucketConfig)
        {
            const string blah = "$HOST";
            var address = hostName.Replace(blah, bucketConfig.SurrogateHost);
            return Core.Server.GetEndPoint(address);
        }

        public abstract void LoadConfig(IBucketConfig bucketConfig);

        public IKeyMapper GetKeyMapper(string bucketName)
        {
            return _keyMapper;
        }

        public IServer GetServer()
        {
            return _servers.Shuffle().First();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed) return;
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
            _servers.ForEach(x => x.Dispose());
            _disposed = false;
        }

        ~ConfigContextBase()
        {
            Dispose(false);
        }
    }
}