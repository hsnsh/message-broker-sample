using System.Net.Sockets;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace GeneralTestApi.Base;

public class RabbitMqPersistentConnection : IRabbitMqPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly int _retryCount;

    [CanBeNull]
    private IConnection _connection;

    private bool _disposed;

    private readonly object _syncRoot = new();

    public RabbitMqPersistentConnection(IOptions<RabbitMqConnectionSettings> conSettings)
    {
        _connectionFactory = new ConnectionFactory()
        {
            HostName = conSettings.Value.HostName,
            Port = conSettings.Value.Port,
            UserName = conSettings.Value.UserName,
            Password = conSettings.Value.Password,
            VirtualHost = conSettings.Value.VirtualHost,
            RequestedHeartbeat = TimeSpan.FromSeconds(60)
        };
        _retryCount = conSettings.Value.ConnectionRetryCount;
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return _connection?.CreateModel();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            if (!IsConnected) return;
            _connection!.ConnectionShutdown -= OnConnectionShutdown;
            _connection.CallbackException -= OnCallbackException;
            _connection.ConnectionBlocked -= OnConnectionBlocked;
            _connection.Dispose();
        }
        catch (IOException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public bool TryConnect()
    {
        Console.WriteLine("RabbitMQ Client is trying to connect");

        lock (_syncRoot)
        {
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        Console.WriteLine("RabbitMQ Client could not connect after {0}s ({1})", $"{time.TotalSeconds:n1}", ex.Message);
                    }
                );

            policy.Execute(() =>
            {
                _connection = _connectionFactory
                    .CreateConnection();
            });

            if (IsConnected)
            {
                _connection!.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;

                Console.WriteLine("RabbitMQ Client acquired a persistent connection to '{0}'", _connection.Endpoint.HostName);

                return true;
            }

            Console.WriteLine("FATAL ERROR: RabbitMQ connections could not be created and opened");

            return false;
        }
    }

    private void OnConnectionBlocked([CanBeNull] object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;

        Console.WriteLine("A RabbitMQ connection is shutdown. Trying to re-connect...");

        TryConnect();
    }

    void OnCallbackException([CanBeNull] object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;

        Console.WriteLine("A RabbitMQ connection throw exception. Trying to re-connect...");

        TryConnect();
    }

    void OnConnectionShutdown([CanBeNull] object sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;

        Console.WriteLine("A RabbitMQ connection is on shutdown. Trying to re-connect...");

        TryConnect();
    }
}