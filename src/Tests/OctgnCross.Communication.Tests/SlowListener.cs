﻿namespace Octgn.Communication.Tests
{
    public class SlowListener : IConnectionListener
    {
        public bool IsEnabled {
            get => _listener.IsEnabled;
            set => _listener.IsEnabled = value;
        }

        public event ConnectionCreated ConnectionCreated {
            add => _listener.ConnectionCreated += value;
            remove => _listener.ConnectionCreated -= value;
        }

        private readonly IConnectionListener _listener;

        public SlowListener(IConnectionListener listener)
        {
            _listener = listener;
        }

        public void Initialize(Server server) {
            _listener.Initialize(server);
        }
    }
}
