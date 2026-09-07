using System;
using System.Collections.Generic;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    internal sealed class InMemoryModMessageBus : IModMessageBus
    {
        private readonly Dictionary<long, List<Action<object>>> _handlers = new();
        
        public int RegistrationCount { get; private set; }

        public int UnregistrationCount { get; private set; }

        public int SendCount { get; private set; }

        public List<object> SentPayloads { get; } = [];

        public void RegisterHandler(long channelId, Action<object> handler)
        {
            if (!_handlers.TryGetValue(channelId, out var handlers))
            {
                handlers = [];
                _handlers.Add(channelId, handlers);
            }

            handlers.Add(handler);
            RegistrationCount++;
        }

        public void UnregisterHandler(long channelId, Action<object> handler)
        {
            if (_handlers.TryGetValue(channelId, out var handlers) && handlers.Remove(handler))
                UnregistrationCount++;
        }

        public void Send(long channelId, object payload)
        {
            SendCount++;
            SentPayloads.Add(payload);

            if (!_handlers.TryGetValue(channelId, out var handlers))
                return;

            var snapshot = handlers.ToArray();

            foreach (var actions in snapshot)
                actions(payload);
        }
    }
}