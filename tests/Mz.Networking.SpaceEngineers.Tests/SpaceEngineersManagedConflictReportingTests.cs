using System;
using System.Collections.Generic;
using Mz.ApiProtocol;
using Mz.ApiProtocol.SpaceEngineers;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.Networking.SpaceEngineers.Tests
{
    public sealed class SpaceEngineersManagedConflictReportingTests
    {
        private const ushort PreferredChannel =
            45120;

        private const ushort AssignedChannel =
            45122;

        private const ushort NewerAssignedChannel =
            45123;

        [Fact]
        public void Version11Provider_PrefersConflictEndpointAndReportsOncePerAssignment()
        {
            var gateway =
                new RecordingGateway();

            var bus =
                new InMemoryModMessageBus();

            var registration =
                new RegistrationCapture
                {
                    AssignDuringRegistration =
                        true,

                    RegistrationChannel =
                        AssignedChannel,

                    RegistrationGeneration =
                        4UL
                };

            using (
                var provider =
                    CreateVersion11Provider(
                        bus,
                        registration
                    )
            )
            {
                provider.Start();

                using (
                    var session =
                        new SpaceEngineersNetworkSession(
                            gateway,
                            bus,
                            CreateConfiguration()
                        )
                )
                {
                    Assert.True(
                        session.IsNetworkManagerConnected
                    );

                    Assert.Null(
                        session.NetworkManagerError
                    );

                    Assert.Equal(
                        0,
                        registration.LegacyRegistrationCount
                    );

                    Assert.Equal(
                        1,
                        registration.ConflictRegistrationCount
                    );

                    Assert.Equal(
                        AssignedChannel,
                        session.ChannelId
                    );

                    Assert.Equal(
                        4UL,
                        session.AssignmentGeneration
                    );

                    gateway.DeliverRaw(
                        AssignedChannel,
                        new byte[]
                        {
                            1,
                            2,
                            3,
                            4
                        },
                        200UL,
                        false
                    );

                    ConflictReport firstReport =
                        Assert.Single(
                            registration.ConflictReports
                        );

                    Assert.Equal(
                        AssignedChannel,
                        firstReport.ChannelId
                    );

                    Assert.Equal(
                        4UL,
                        firstReport.Generation
                    );

                    gateway.DeliverRaw(
                        AssignedChannel,
                        new byte[]
                        {
                            5,
                            6,
                            7
                        },
                        201UL,
                        false
                    );

                    Assert.Single(
                        registration.ConflictReports
                    );

                    session.Transport.SendToServer(
                        new NetworkEnvelope(
                            "Managed.Test",
                            100UL,
                            false,
                            new byte[]
                            {
                                8
                            }
                        )
                    );

                    Assert.NotNull(
                        gateway.LastServerPayload
                    );

                    gateway.DeliverRaw(
                        AssignedChannel,
                        gateway.LastServerPayload,
                        202UL,
                        false
                    );

                    Assert.Single(
                        registration.ConflictReports
                    );

                    registration.Assign(
                        NewerAssignedChannel,
                        5UL
                    );

                    Assert.Equal(
                        NewerAssignedChannel,
                        session.ChannelId
                    );

                    Assert.Equal(
                        5UL,
                        session.AssignmentGeneration
                    );

                    gateway.DeliverRaw(
                        NewerAssignedChannel,
                        new byte[]
                        {
                            9,
                            10,
                            11
                        },
                        203UL,
                        false
                    );

                    Assert.Equal(
                        2,
                        registration.ConflictReports.Count
                    );

                    ConflictReport secondReport =
                        registration.ConflictReports[1];

                    Assert.Equal(
                        NewerAssignedChannel,
                        secondReport.ChannelId
                    );

                    Assert.Equal(
                        5UL,
                        secondReport.Generation
                    );
                }
            }
        }

        [Fact]
        public void Version10Provider_UsesLegacyEndpointWithoutConflictReporting()
        {
            var gateway =
                new RecordingGateway();

            var bus =
                new InMemoryModMessageBus();

            var registration =
                new RegistrationCapture
                {
                    AssignDuringRegistration =
                        true,

                    RegistrationChannel =
                        AssignedChannel,

                    RegistrationGeneration =
                        3UL
                };

            using (
                var provider =
                    CreateVersion10Provider(
                        bus,
                        registration
                    )
            )
            {
                provider.Start();

                using (
                    var session =
                        new SpaceEngineersNetworkSession(
                            gateway,
                            bus,
                            CreateConfiguration()
                        )
                )
                {
                    Assert.True(
                        session.IsNetworkManagerConnected
                    );

                    Assert.Null(
                        session.NetworkManagerError
                    );

                    Assert.Equal(
                        1,
                        registration.LegacyRegistrationCount
                    );

                    Assert.Equal(
                        0,
                        registration.ConflictRegistrationCount
                    );

                    Assert.Equal(
                        AssignedChannel,
                        session.ChannelId
                    );

                    Assert.Equal(
                        3UL,
                        session.AssignmentGeneration
                    );

                    gateway.DeliverRaw(
                        AssignedChannel,
                        new byte[]
                        {
                            1,
                            2,
                            3
                        },
                        200UL,
                        false
                    );

                    Assert.Empty(
                        registration.ConflictReports
                    );
                }
            }
        }

        private static SpaceEngineersManagedNetworkConfiguration
            CreateConfiguration()
        {
            return
                new SpaceEngineersManagedNetworkConfiguration(
                    "Mz.ExampleMod",
                    "Example Mod",
                    new SemanticVersion(
                        1,
                        2,
                        3
                    ),
                    "Mz.Example.Network",
                    "Example Network",
                    PreferredChannel,
                    null
                );
        }

        private static ApiDiscoveryProvider
            CreateVersion10Provider(
                IModMessageBus bus,
                RegistrationCapture registration
            )
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
            > registerNetwork =
                registration.RegisterLegacy;

            return
                new ApiDiscoveryProvider(
                    bus,
                    new ApiModIdentity(
                        "Mz.NetworkManagerMod",
                        "Network Manager",
                        new SemanticVersion(
                            1,
                            0,
                            0
                        )
                    ),
                    new ApiDescriptor(
                        "Mz.NetworkManager",
                        new SemanticVersion(
                            1,
                            0,
                            0
                        )
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

        private static ApiDiscoveryProvider
            CreateVersion11Provider(
                IModMessageBus bus,
                RegistrationCapture registration
            )
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
            > registerNetwork =
                registration.RegisterLegacy;

            Func<
                string,
                string,
                Version,
                string,
                string,
                ushort,
                Action<
                    ushort,
                    ulong,
                    Action<ushort, ulong>
                >,
                Action
            > registerNetworkWithConflictReporting =
                registration.RegisterWithConflictReporting;

            return
                new ApiDiscoveryProvider(
                    bus,
                    new ApiModIdentity(
                        "Mz.NetworkManagerMod",
                        "Network Manager",
                        new SemanticVersion(
                            1,
                            0,
                            0
                        )
                    ),
                    new ApiDescriptor(
                        "Mz.NetworkManager",
                        new SemanticVersion(
                            1,
                            1,
                            0
                        )
                    ),
                    new Dictionary<string, Delegate>
                    {
                        {
                            "RegisterNetwork",
                            registerNetwork
                        },
                        {
                            "RegisterNetworkWithConflictReporting",
                            registerNetworkWithConflictReporting
                        }
                    }
                );
        }

        private sealed class RegistrationCapture
        {
            private Action<ushort, ulong>?
                _legacyAssignmentCallback;

            private Action<
                ushort,
                ulong,
                Action<ushort, ulong>
            >? _conflictAssignmentCallback;

            public bool AssignDuringRegistration
            {
                get;
                set;
            }

            public ushort RegistrationChannel
            {
                get;
                set;
            }

            public ulong RegistrationGeneration
            {
                get;
                set;
            }

            public int LegacyRegistrationCount
            {
                get;
                private set;
            }

            public int ConflictRegistrationCount
            {
                get;
                private set;
            }

            public List<ConflictReport> ConflictReports
            {
                get;
            } =
                new List<ConflictReport>();

            public Action RegisterLegacy(
                string modId,
                string modDisplayName,
                Version modVersion,
                string networkId,
                string networkName,
                ushort preferredChannel,
                Action<ushort, ulong> assignmentCallback
            )
            {
                LegacyRegistrationCount++;

                _legacyAssignmentCallback =
                    assignmentCallback;

                if (AssignDuringRegistration)
                {
                    assignmentCallback(
                        RegistrationChannel,
                        RegistrationGeneration
                    );
                }

                return delegate
                {
                };
            }

            public Action RegisterWithConflictReporting(
                string modId,
                string modDisplayName,
                Version modVersion,
                string networkId,
                string networkName,
                ushort preferredChannel,
                Action<
                    ushort,
                    ulong,
                    Action<ushort, ulong>
                > assignmentCallback
            )
            {
                ConflictRegistrationCount++;

                _conflictAssignmentCallback =
                    assignmentCallback;

                if (AssignDuringRegistration)
                {
                    assignmentCallback(
                        RegistrationChannel,
                        RegistrationGeneration,
                        ReportConflict
                    );
                }

                return delegate
                {
                };
            }

            public void Assign(
                ushort channelId,
                ulong generation
            )
            {
                if (_conflictAssignmentCallback != null)
                {
                    _conflictAssignmentCallback(
                        channelId,
                        generation,
                        ReportConflict
                    );

                    return;
                }

                Assert.NotNull(
                    _legacyAssignmentCallback
                );

                _legacyAssignmentCallback(
                    channelId,
                    generation
                );
            }

            private void ReportConflict(
                ushort channelId,
                ulong generation
            )
            {
                ConflictReports.Add(
                    new ConflictReport(
                        channelId,
                        generation
                    )
                );
            }
        }

        private sealed class ConflictReport
        {
            public ConflictReport(
                ushort channelId,
                ulong generation
            )
            {
                ChannelId =
                    channelId;

                Generation =
                    generation;
            }

            public ushort ChannelId
            {
                get;
            }

            public ulong Generation
            {
                get;
            }
        }

        private sealed class InMemoryModMessageBus :
            IModMessageBus
        {
            private readonly Dictionary<
                long,
                List<Action<object>>
            > _handlers =
                new Dictionary<
                    long,
                    List<Action<object>>
                >();

            public void RegisterHandler(
                long channelId,
                Action<object> handler
            )
            {
                List<Action<object>> handlers;

                if (
                    !_handlers.TryGetValue(
                        channelId,
                        out handlers
                    )
                )
                {
                    handlers =
                        new List<Action<object>>();

                    _handlers.Add(
                        channelId,
                        handlers
                    );
                }

                handlers.Add(
                    handler
                );
            }

            public void UnregisterHandler(
                long channelId,
                Action<object> handler
            )
            {
                List<Action<object>> handlers;

                if (
                    _handlers.TryGetValue(
                        channelId,
                        out handlers
                    )
                )
                {
                    handlers.Remove(
                        handler
                    );
                }
            }

            public void Send(
                long channelId,
                object payload
            )
            {
                List<Action<object>> handlers;

                if (
                    !_handlers.TryGetValue(
                        channelId,
                        out handlers
                    )
                )
                {
                    return;
                }

                Action<object>[] snapshot =
                    handlers.ToArray();

                for (
                    int index = 0;
                    index < snapshot.Length;
                    index++
                )
                {
                    snapshot[index](
                        payload
                    );
                }
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

            public bool IsServer =>
                true;

            public ulong LocalPeerId =>
                100UL;

            public byte[]? LastServerPayload
            {
                get;
                private set;
            }

            public void RegisterSecureMessageHandler(
                ushort channelId,
                Action<ushort, byte[], ulong, bool> handler
            )
            {
                _handlers[channelId] =
                    handler;
            }

            public void UnregisterSecureMessageHandler(
                ushort channelId,
                Action<ushort, byte[], ulong, bool> handler
            )
            {
                Action<ushort, byte[], ulong, bool>
                    registered;

                if (
                    _handlers.TryGetValue(
                        channelId,
                        out registered
                    )
                    && ReferenceEquals(
                        registered,
                        handler
                    )
                )
                {
                    _handlers.Remove(
                        channelId
                    );
                }
            }

            public byte[] Serialize(
                NetworkEnvelope envelope
            )
            {
                return
                    new byte[]
                    {
                        42
                    };
            }

            public NetworkEnvelope Deserialize(
                byte[] serialized
            )
            {
                throw
                    new InvalidOperationException(
                        "Configured malformed-own-packet failure."
                    );
            }

            public bool SendToServer(
                ushort channelId,
                byte[] serialized
            )
            {
                return SendToServer(
                    channelId,
                    serialized,
                    true
                );
            }

            public bool SendToServer(
                ushort channelId,
                byte[] serialized,
                bool reliable
            )
            {
                LastServerPayload =
                    Copy(
                        serialized
                    );

                return true;
            }

            public bool SendToPeer(
                ushort channelId,
                byte[] serialized,
                ulong peerId
            )
            {
                return true;
            }

            public bool SendToPeer(
                ushort channelId,
                byte[] serialized,
                ulong peerId,
                bool reliable
            )
            {
                return true;
            }

            public void GetPlayerIds(
                List<ulong> playerIds
            )
            {
            }

            public void DeliverRaw(
                ushort channelId,
                byte[] serialized,
                ulong senderPeerId,
                bool senderIsServer
            )
            {
                Action<ushort, byte[], ulong, bool>
                    handler;

                if (
                    !_handlers.TryGetValue(
                        channelId,
                        out handler
                    )
                )
                {
                    throw
                        new InvalidOperationException(
                            "No handler is registered for channel "
                            + channelId
                            + "."
                        );
                }

                handler(
                    channelId,
                    Copy(
                        serialized
                    ),
                    senderPeerId,
                    senderIsServer
                );
            }

            private static byte[] Copy(
                byte[] source
            )
            {
                var copy =
                    new byte[source.Length];

                Array.Copy(
                    source,
                    copy,
                    source.Length
                );

                return copy;
            }
        }
    }
}