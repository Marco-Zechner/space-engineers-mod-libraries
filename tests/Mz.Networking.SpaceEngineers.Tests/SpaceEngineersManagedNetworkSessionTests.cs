using System;
using System.Collections.Generic;
using Mz.ApiProtocol;
using Mz.ApiProtocol.SpaceEngineers;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.Networking.SpaceEngineers.Tests
{
    public sealed class SpaceEngineersManagedNetworkSessionTests
    {
        private const ushort PreferredChannel = 45120;
        private const ushort ForcedChannel = 45121;
        private const ushort AssignedChannel = 45122;
        private const ushort NewerAssignedChannel = 45123;

        [Fact]
        public void ManagedSession_StartsOnPreferredChannelBeforeDiscovery()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var configuration = CreateConfiguration(null);

            using (
                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    bus,
                    configuration
                )
            )
            {
                Assert.Equal(PreferredChannel, session.ChannelId);
                Assert.False(session.IsForcedChannel);
                Assert.Null(session.AssignmentGeneration);
                Assert.Equal(
                    new[] { PreferredChannel },
                    gateway.RegisteredChannels
                );
                Assert.Equal(1, bus.RegistrationCount);
                Assert.Equal(1, bus.SendCount);
            }
        }

        [Fact]
        public void ProviderFirst_DiscoveryRegistersAndAppliesSynchronousAssignment()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var registration = new RegistrationCapture
            {
                AssignDuringRegistration = true,
                RegistrationChannel = AssignedChannel,
                RegistrationGeneration = 4UL
            };

            using (var provider = CreateProvider(bus, registration))
            {
                provider.Start();

                using (
                    var session = new SpaceEngineersNetworkSession(
                        gateway,
                        bus,
                        CreateConfiguration(null)
                    )
                )
                {
                    Assert.True(session.IsNetworkManagerConnected);
                    Assert.Null(session.NetworkManagerError);
                    Assert.Equal(AssignedChannel, session.ChannelId);
                    Assert.Equal(4UL, session.AssignmentGeneration);
                    Assert.Equal(1, registration.RegistrationCount);
                    Assert.Equal("Mz.ExampleMod", registration.ModId);
                    Assert.Equal("Example Mod", registration.ModDisplayName);
                    Assert.Equal(new Version(1, 2, 3), registration.ModVersion);
                    Assert.Equal(
                        "Mz.Example.Network",
                        registration.NetworkId
                    );
                    Assert.Equal(
                        "Example Network",
                        registration.NetworkName
                    );
                    Assert.Equal(
                        PreferredChannel,
                        registration.PreferredChannel
                    );
                    Assert.Equal(
                        new[] { PreferredChannel, AssignedChannel },
                        gateway.RegisteredChannels
                    );
                    Assert.Equal(
                        new[] { PreferredChannel },
                        gateway.UnregisteredChannels
                    );

                    session.Transport.SendToServer(
                        new NetworkEnvelope(
                            "Managed.Test",
                            100UL,
                            false,
                            new byte[] { 1 }
                        )
                    );

                    Assert.Equal(
                        AssignedChannel,
                        gateway.LastServerSendChannel
                    );
                }
            }
        }

        [Fact]
        public void AssignmentEvent_ReportsAcceptedGenerationAndChannelChange()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var registration = new RegistrationCapture();

            using (var provider = CreateProvider(bus, registration))
            {
                provider.Start();

                using (
                    var session = new SpaceEngineersNetworkSession(
                        gateway,
                        bus,
                        CreateConfiguration(null)
                    )
                )
                {
                    SpaceEngineersNetworkChannelAssignmentEventArgs
                        observed = null!;

                    session.ChannelAssignmentApplied +=
                        delegate(
                            SpaceEngineersNetworkChannelAssignmentEventArgs
                                eventArgs
                        )
                        {
                            observed = eventArgs;
                        };

                    registration.Assign(AssignedChannel, 8UL);

                    Assert.NotNull(observed);
                    Assert.Equal(
                        PreferredChannel,
                        observed.PreviousChannel
                    );
                    Assert.Equal(AssignedChannel, observed.ChannelId);
                    Assert.Equal(8UL, observed.Generation);
                    Assert.True(observed.ChannelChanged);
                }
            }
        }

        [Fact]
        public void InvalidProviderContract_LeavesFallbackAndExposesError()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();

            using (var provider = CreateInvalidProvider(bus))
            {
                provider.Start();

                using (
                    var session = new SpaceEngineersNetworkSession(
                        gateway,
                        bus,
                        CreateConfiguration(null)
                    )
                )
                {
                    Assert.False(session.IsNetworkManagerConnected);
                    Assert.NotNull(session.NetworkManagerError);
                    Assert.Equal(PreferredChannel, session.ChannelId);
                    Assert.Null(session.AssignmentGeneration);
                }
            }
        }

        [Fact]
        public void InvalidProviderWithdrawal_ClearsErrorAndAllowsReplacement()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();

            using (
                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    bus,
                    CreateConfiguration(null)
                )
            )
            {
                using (var invalidProvider = CreateInvalidProvider(bus))
                {
                    invalidProvider.Start();

                    Assert.NotNull(session.NetworkManagerError);
                    Assert.False(session.IsNetworkManagerConnected);

                    invalidProvider.Stop();

                    Assert.Null(session.NetworkManagerError);
                    Assert.False(session.IsNetworkManagerConnected);
                    Assert.Equal(PreferredChannel, session.ChannelId);
                }

                var registration = new RegistrationCapture
                {
                    AssignDuringRegistration = true,
                    RegistrationChannel = AssignedChannel,
                    RegistrationGeneration = 1UL
                };

                using (
                    var validProvider = CreateProvider(
                        bus,
                        registration
                    )
                )
                {
                    validProvider.Start();

                    Assert.True(session.IsNetworkManagerConnected);
                    Assert.Null(session.NetworkManagerError);
                    Assert.Equal(AssignedChannel, session.ChannelId);
                    Assert.Equal(1UL, session.AssignmentGeneration);
                }
            }
        }

        [Fact]
        public void DiscoverySendFailure_KeepsFallbackAndRecoversOnAnnouncement()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus
            {
                SendException =
                    new InvalidOperationException(
                        "Discovery transport unavailable."
                    )
            };

            using (
                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    bus,
                    CreateConfiguration(null)
                )
            )
            {
                Assert.Equal(PreferredChannel, session.ChannelId);
                Assert.False(session.IsNetworkManagerConnected);
                Assert.NotNull(session.NetworkManagerError);
                Assert.True(gateway.IsRegistered(PreferredChannel));

                bus.SendException = null;

                var registration = new RegistrationCapture
                {
                    AssignDuringRegistration = true,
                    RegistrationChannel = AssignedChannel,
                    RegistrationGeneration = 2UL
                };

                using (
                    var provider = CreateProvider(
                        bus,
                        registration
                    )
                )
                {
                    provider.Start();

                    Assert.True(session.IsNetworkManagerConnected);
                    Assert.Null(session.NetworkManagerError);
                    Assert.Equal(AssignedChannel, session.ChannelId);
                    Assert.Equal(2UL, session.AssignmentGeneration);
                }
            }
        }

        [Fact]
        public void AssignmentEvent_SubscriberFailureIsIsolated()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var registration = new RegistrationCapture();
            var observedCount = 0;

            using (var provider = CreateProvider(bus, registration))
            {
                provider.Start();

                using (
                    var session = new SpaceEngineersNetworkSession(
                        gateway,
                        bus,
                        CreateConfiguration(null)
                    )
                )
                {
                    session.ChannelAssignmentApplied +=
                        delegate
                        {
                            throw new InvalidOperationException(
                                "Subscriber failure."
                            );
                        };

                    session.ChannelAssignmentApplied +=
                        delegate
                        {
                            observedCount++;
                        };

                    registration.Assign(AssignedChannel, 3UL);

                    Assert.Equal(1, observedCount);
                    Assert.Equal(AssignedChannel, session.ChannelId);
                    Assert.Equal(3UL, session.AssignmentGeneration);
                }
            }
        }

        [Fact]
        public void ConsumerFirst_LateProviderReassignsExistingSession()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var registration = new RegistrationCapture
            {
                AssignDuringRegistration = true,
                RegistrationChannel = AssignedChannel,
                RegistrationGeneration = 1UL
            };

            using (
                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    bus,
                    CreateConfiguration(null)
                )
            )
            using (var provider = CreateProvider(bus, registration))
            {
                Assert.Equal(PreferredChannel, session.ChannelId);

                provider.Start();

                Assert.Equal(AssignedChannel, session.ChannelId);
                Assert.Equal(1UL, session.AssignmentGeneration);
                Assert.Equal(1, registration.RegistrationCount);
            }
        }

        [Fact]
        public void Assignments_ApplyOnlyStrictlyNewerGeneration()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var registration = new RegistrationCapture();

            using (var provider = CreateProvider(bus, registration))
            {
                provider.Start();

                using (
                    var session = new SpaceEngineersNetworkSession(
                        gateway,
                        bus,
                        CreateConfiguration(null)
                    )
                )
                {
                    registration.Assign(AssignedChannel, 5UL);
                    registration.Assign(NewerAssignedChannel, 4UL);
                    registration.Assign(NewerAssignedChannel, 5UL);

                    Assert.Equal(AssignedChannel, session.ChannelId);
                    Assert.Equal(5UL, session.AssignmentGeneration);
                    Assert.Equal(
                        new[] { PreferredChannel, AssignedChannel },
                        gateway.RegisteredChannels
                    );

                    registration.Assign(NewerAssignedChannel, 6UL);

                    Assert.Equal(NewerAssignedChannel, session.ChannelId);
                    Assert.Equal(6UL, session.AssignmentGeneration);
                    Assert.Equal(
                        new[]
                        {
                            PreferredChannel,
                            AssignedChannel,
                            NewerAssignedChannel
                        },
                        gateway.RegisteredChannels
                    );
                }
            }
        }

        [Fact]
        public void NewerAssignmentOnCurrentChannel_UpdatesGenerationWithoutReregistering()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var registration = new RegistrationCapture();

            using (var provider = CreateProvider(bus, registration))
            {
                provider.Start();

                using (
                    var session = new SpaceEngineersNetworkSession(
                        gateway,
                        bus,
                        CreateConfiguration(null)
                    )
                )
                {
                    registration.Assign(PreferredChannel, 7UL);

                    Assert.Equal(PreferredChannel, session.ChannelId);
                    Assert.Equal(7UL, session.AssignmentGeneration);
                    Assert.Equal(
                        new[] { PreferredChannel },
                        gateway.RegisteredChannels
                    );
                    Assert.Empty(gateway.UnregisteredChannels);
                }
            }
        }

        [Fact]
        public void ProviderWithdrawal_RetainsChannelAndRejectsRetiredCallback()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var registration = new RegistrationCapture
            {
                AssignDuringRegistration = true,
                RegistrationChannel = AssignedChannel,
                RegistrationGeneration = 3UL
            };

            using (
                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    bus,
                    CreateConfiguration(null)
                )
            )
            using (var provider = CreateProvider(bus, registration))
            {
                provider.Start();

                Assert.Equal(AssignedChannel, session.ChannelId);
                Assert.Equal(3UL, session.AssignmentGeneration);

                provider.Stop();

                Assert.Equal(1, registration.UnregisterCount);
                Assert.False(session.IsNetworkManagerConnected);
                Assert.Equal(AssignedChannel, session.ChannelId);

                registration.Assign(NewerAssignedChannel, 4UL);

                Assert.Equal(AssignedChannel, session.ChannelId);
                Assert.Equal(3UL, session.AssignmentGeneration);
            }
        }

        [Fact]
        public void ProviderRestart_AcceptsResetGenerationAndRejectsOldCallback()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var firstRegistration = new RegistrationCapture
            {
                AssignDuringRegistration = true,
                RegistrationChannel = AssignedChannel,
                RegistrationGeneration = 10UL
            };

            using (
                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    bus,
                    CreateConfiguration(null)
                )
            )
            {
                using (
                    var firstProvider = CreateProvider(
                        bus,
                        firstRegistration
                    )
                )
                {
                    firstProvider.Start();

                    Assert.Equal(AssignedChannel, session.ChannelId);
                    Assert.Equal(10UL, session.AssignmentGeneration);

                    firstProvider.Stop();
                }

                var secondRegistration = new RegistrationCapture
                {
                    AssignDuringRegistration = true,
                    RegistrationChannel = NewerAssignedChannel,
                    RegistrationGeneration = 1UL
                };

                using (
                    var secondProvider = CreateProvider(
                        bus,
                        secondRegistration
                    )
                )
                {
                    secondProvider.Start();

                    Assert.Equal(
                        NewerAssignedChannel,
                        session.ChannelId
                    );
                    Assert.Equal(1UL, session.AssignmentGeneration);

                    firstRegistration.Assign(
                        PreferredChannel,
                        11UL
                    );

                    Assert.Equal(
                        NewerAssignedChannel,
                        session.ChannelId
                    );
                    Assert.Equal(1UL, session.AssignmentGeneration);
                }
            }
        }

        [Fact]
        public void FailedChannelSwitch_LeavesPreviousChannelAndGenerationActive()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var registration = new RegistrationCapture();

            using (var provider = CreateProvider(bus, registration))
            {
                provider.Start();

                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    bus,
                    CreateConfiguration(null)
                );

                try
                {
                    gateway.ThrowOnUnregisterChannel = PreferredChannel;

                    Assert.Throws<InvalidOperationException>(
                        delegate
                        {
                            registration.Assign(AssignedChannel, 1UL);
                        }
                    );

                    Assert.Equal(PreferredChannel, session.ChannelId);
                    Assert.Null(session.AssignmentGeneration);
                    Assert.True(
                        gateway.IsRegistered(PreferredChannel)
                    );
                    Assert.False(
                        gateway.IsRegistered(AssignedChannel)
                    );

                    session.Transport.SendToServer(
                        new NetworkEnvelope(
                            "Managed.Test",
                            100UL,
                            false,
                            new byte[] { 1 }
                        )
                    );

                    Assert.Equal(
                        PreferredChannel,
                        gateway.LastServerSendChannel
                    );
                }
                finally
                {
                    gateway.ThrowOnUnregisterChannel = null;
                    session.Dispose();
                }
            }
        }

        [Fact]
        public void ForcedChannel_GatewayConstructorHidesApiProtocolSetup()
        {
            var gateway = new RecordingGateway();

            using (
                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    CreateConfiguration(ForcedChannel)
                )
            )
            {
                Assert.Equal(ForcedChannel, session.ChannelId);
                Assert.True(session.IsForcedChannel);
                Assert.False(session.IsNetworkManagerConnected);
                Assert.Null(session.NetworkManagerError);
            }
        }

        [Fact]
        public void ForcedChannel_AllowsNoMessageBusAndSkipsDiscovery()
        {
            var gateway = new RecordingGateway();
            var configuration = CreateConfiguration(ForcedChannel);

            using (
                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    null,
                    configuration
                )
            )
            {
                Assert.Equal(ForcedChannel, session.ChannelId);
                Assert.True(session.IsForcedChannel);
                Assert.Null(session.AssignmentGeneration);
                Assert.Equal(
                    new[] { ForcedChannel },
                    gateway.RegisteredChannels
                );
            }
        }

        [Fact]
        public void Dispose_ReleasesManagerDiscoveryRegistrationAndCurrentChannel()
        {
            var gateway = new RecordingGateway();
            var bus = new InMemoryModMessageBus();
            var registration = new RegistrationCapture
            {
                AssignDuringRegistration = true,
                RegistrationChannel = AssignedChannel,
                RegistrationGeneration = 2UL
            };

            using (var provider = CreateProvider(bus, registration))
            {
                provider.Start();

                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    bus,
                    CreateConfiguration(null)
                );

                session.Dispose();
                session.Dispose();

                Assert.Equal(1, registration.UnregisterCount);
                Assert.Equal(1, bus.UnregistrationCount);
                Assert.False(gateway.IsRegistered(AssignedChannel));

                registration.Assign(NewerAssignedChannel, 3UL);

                Assert.Equal(AssignedChannel, session.ChannelId);
                Assert.Equal(2UL, session.AssignmentGeneration);
            }
        }

        private static SpaceEngineersManagedNetworkConfiguration
            CreateConfiguration(ushort? forcedChannel)
        {
            return new SpaceEngineersManagedNetworkConfiguration(
                "Mz.ExampleMod",
                "Example Mod",
                new SemanticVersion(1, 2, 3),
                "Mz.Example.Network",
                "Example Network",
                PreferredChannel,
                forcedChannel
            );
        }

        private static ApiDiscoveryProvider CreateInvalidProvider(
            IModMessageBus bus)
        {
            return new ApiDiscoveryProvider(
                bus,
                new ApiModIdentity(
                    "Mz.InvalidNetworkManagerMod",
                    "Invalid Network Manager",
                    new SemanticVersion(1, 0, 0)
                ),
                new ApiDescriptor(
                    "Mz.NetworkManager",
                    new SemanticVersion(1, 0, 0)
                ),
                new Dictionary<string, Delegate>
                {
                    {
                        "RegisterNetwork",
                        (Action)delegate
                        {
                        }
                    }
                }
            );
        }

        private static ApiDiscoveryProvider CreateProvider(
            IModMessageBus bus,
            RegistrationCapture registration)
        {
            Func<
                string,
                string,
                Version,
                string,
                string,
                ushort,
                Action<ushort, ulong>,
                Action
            > registerNetwork = registration.Register;

            return new ApiDiscoveryProvider(
                bus,
                new ApiModIdentity(
                    "Mz.NetworkManagerMod",
                    "Network Manager",
                    new SemanticVersion(1, 0, 0)
                ),
                new ApiDescriptor(
                    "Mz.NetworkManager",
                    new SemanticVersion(1, 0, 0)
                ),
                new Dictionary<string, Delegate>
                {
                    {
                        "RegisterNetwork",
                        registerNetwork
                    }
                }
            );
        }

        private sealed class RegistrationCapture
        {
            private Action<ushort, ulong> _assignmentCallback = null!;

            public bool AssignDuringRegistration { get; set; }

            public ushort RegistrationChannel { get; set; }

            public ulong RegistrationGeneration { get; set; }

            public int RegistrationCount { get; private set; }

            public int UnregisterCount { get; private set; }

            public string ModId { get; private set; } = null!;

            public string ModDisplayName { get; private set; } = null!;

            public Version ModVersion { get; private set; } = null!;

            public string NetworkId { get; private set; } = null!;

            public string NetworkName { get; private set; } = null!;

            public ushort PreferredChannel { get; private set; }

            public Action Register(
                string modId,
                string modDisplayName,
                Version modVersion,
                string networkId,
                string networkName,
                ushort preferredChannel,
                Action<ushort, ulong> assignmentCallback)
            {
                RegistrationCount++;
                ModId = modId;
                ModDisplayName = modDisplayName;
                ModVersion = modVersion;
                NetworkId = networkId;
                NetworkName = networkName;
                PreferredChannel = preferredChannel;
                _assignmentCallback = assignmentCallback;

                if (AssignDuringRegistration)
                {
                    assignmentCallback(
                        RegistrationChannel,
                        RegistrationGeneration
                    );
                }

                return delegate
                {
                    UnregisterCount++;
                };
            }

            public void Assign(ushort channelId, ulong generation)
            {
                Assert.NotNull(_assignmentCallback);
                _assignmentCallback(channelId, generation);
            }
        }

        private sealed class InMemoryModMessageBus : IModMessageBus
        {
            private readonly Dictionary<long, List<Action<object>>>
                _handlers =
                    new Dictionary<long, List<Action<object>>>();

            public int RegistrationCount { get; private set; }

            public int UnregistrationCount { get; private set; }

            public int SendCount { get; private set; }

            public Exception? SendException { get; set; }

            public void RegisterHandler(
                long channelId,
                Action<object> handler)
            {
                List<Action<object>> handlers;

                if (!_handlers.TryGetValue(channelId, out handlers))
                {
                    handlers = new List<Action<object>>();
                    _handlers.Add(channelId, handlers);
                }

                handlers.Add(handler);
                RegistrationCount++;
            }

            public void UnregisterHandler(
                long channelId,
                Action<object> handler)
            {
                List<Action<object>> handlers;

                if (
                    _handlers.TryGetValue(channelId, out handlers)
                    && handlers.Remove(handler)
                )
                {
                    UnregistrationCount++;
                }
            }

            public void Send(long channelId, object payload)
            {
                SendCount++;

                if (SendException != null)
                    throw SendException;

                List<Action<object>> handlers;

                if (!_handlers.TryGetValue(channelId, out handlers))
                    return;

                var snapshot = handlers.ToArray();

                for (var index = 0; index < snapshot.Length; index++)
                    snapshot[index](payload);
            }
        }

        private sealed class RecordingGateway :
            ISpaceEngineersNetworkDeliveryGateway
        {
            private readonly Dictionary<
                ushort,
                Action<ushort, byte[], ulong, bool>
            > _handlers =
                new Dictionary<
                    ushort,
                    Action<ushort, byte[], ulong, bool>
                >();

            public bool IsServer => true;

            public ulong LocalPeerId => 100UL;

            public List<ushort> RegisteredChannels { get; } =
                new List<ushort>();

            public List<ushort> UnregisteredChannels { get; } =
                new List<ushort>();

            public ushort? ThrowOnRegisterChannel { get; set; }

            public ushort? ThrowOnUnregisterChannel { get; set; }

            public ushort LastServerSendChannel { get; private set; }

            public void RegisterSecureMessageHandler(
                ushort channelId,
                Action<ushort, byte[], ulong, bool> handler)
            {
                if (ThrowOnRegisterChannel == channelId)
                {
                    throw new InvalidOperationException(
                        "Configured registration failure."
                    );
                }

                _handlers[channelId] = handler;
                RegisteredChannels.Add(channelId);
            }

            public void UnregisterSecureMessageHandler(
                ushort channelId,
                Action<ushort, byte[], ulong, bool> handler)
            {
                if (ThrowOnUnregisterChannel == channelId)
                {
                    throw new InvalidOperationException(
                        "Configured unregistration failure."
                    );
                }

                Action<ushort, byte[], ulong, bool> registered;

                if (
                    _handlers.TryGetValue(channelId, out registered)
                    && ReferenceEquals(registered, handler)
                )
                {
                    _handlers.Remove(channelId);
                }

                UnregisteredChannels.Add(channelId);
            }

            public bool IsRegistered(ushort channelId)
                => _handlers.ContainsKey(channelId);

            public byte[] Serialize(NetworkEnvelope envelope)
                => new byte[] { 1 };

            public NetworkEnvelope Deserialize(byte[] serialized)
                => throw new NotSupportedException();

            public bool SendToServer(
                ushort channelId,
                byte[] serialized)
                => SendToServer(channelId, serialized, true);

            public bool SendToServer(
                ushort channelId,
                byte[] serialized,
                bool reliable)
            {
                LastServerSendChannel = channelId;
                return true;
            }

            public bool SendToPeer(
                ushort channelId,
                byte[] serialized,
                ulong peerId)
                => true;

            public bool SendToPeer(
                ushort channelId,
                byte[] serialized,
                ulong peerId,
                bool reliable)
                => true;

            public void GetPlayerIds(List<ulong> playerIds)
            {
            }
        }
    }
}
