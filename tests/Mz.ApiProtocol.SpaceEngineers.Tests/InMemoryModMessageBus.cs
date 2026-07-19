using System;
using System.Collections.Generic;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    internal sealed class InMemoryModMessageBus : IModMessageBus
    {
        private readonly Dictionary<long, List<Action<object>>>
            _handlers =
                new Dictionary<long, List<Action<object>>>();

        public int RegistrationCount { get; private set; }

        public int UnregistrationCount { get; private set; }

        public int SendCount { get; private set; }

        public List<object> SentPayloads { get; } =
            new List<object>();

        public void RegisterHandler(
            long channelId,
            Action<object> handler
        )
        {
            List<Action<object>> handlers;

            if (!_handlers.TryGetValue(
                channelId,
                out handlers
            ))
            {
                handlers = new List<Action<object>>();
                _handlers.Add(channelId, handlers);
            }

            handlers.Add(handler);
            RegistrationCount++;
        }

        public void UnregisterHandler(
            long channelId,
            Action<object> handler
        )
        {
            List<Action<object>> handlers;

            if (_handlers.TryGetValue(
                channelId,
                out handlers
            )
                && handlers.Remove(handler))
            {
                UnregistrationCount++;
            }
        }

        public void Send(
            long channelId,
            object payload
        )
        {
            SendCount++;
            SentPayloads.Add(payload);

            List<Action<object>> handlers;

            if (!_handlers.TryGetValue(
                channelId,
                out handlers
            ))
            {
                return;
            }

            Action<object>[] snapshot = handlers.ToArray();

            for (int index = 0; index < snapshot.Length; index++)
                snapshot[index](payload);
        }
    }
}