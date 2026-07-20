using System;
using Xunit;

namespace Mz.Networking.Tests
{
    public sealed class NetworkMessageDispatcherTests
    {
        [Fact]
        public void TryDispatch_RegisteredMessage_InvokesHandler()
        {
            var dispatcher = new NetworkMessageDispatcher();
            NetworkReceiveContext? observed = null;

            using (
                dispatcher.RegisterHandler(
                    "Command.Execute",
                    delegate(NetworkReceiveContext context)
                    {
                        observed = context;
                        context.RelayMode =
                            NetworkRelayMode.ReturnToSender;
                    }
                )
            )
            {
                bool dispatched =
                    dispatcher.TryDispatch(
                        new NetworkEnvelope(
                            "Command.Execute",
                            101UL,
                            false,
                            new byte[] { 1 }
                        ),
                        202UL,
                        true,
                        false,
                        out NetworkReceiveContext context
                    );

                Assert.True(dispatched);
                Assert.Same(context, observed);

                Assert.Equal(
                    202UL,
                    context.Envelope.OriginalSenderId
                );

                Assert.True(
                    context.OriginalSenderWasCorrected
                );

                Assert.Equal(
                    NetworkRelayMode.ReturnToSender,
                    context.RelayMode
                );
            }
        }

        [Fact]
        public void TryDispatch_UnknownMessage_ReturnsFalse()
        {
            var dispatcher = new NetworkMessageDispatcher();

            bool dispatched =
                dispatcher.TryDispatch(
                    new NetworkEnvelope(
                        "Unknown.Message",
                        101UL,
                        false,
                        new byte[0]
                    ),
                    101UL,
                    true,
                    false,
                    out NetworkReceiveContext context
                );

            Assert.False(dispatched);
            Assert.Null(context);
        }

        [Fact]
        public void RegisterHandler_DuplicateMessageType_Throws()
        {
            var dispatcher = new NetworkMessageDispatcher();

            using (
                dispatcher.RegisterHandler(
                    "Command.Execute",
                    delegate
                    {
                    }
                )
            )
            {
                Assert.Throws<InvalidOperationException>(
                    delegate
                    {
                        dispatcher.RegisterHandler(
                            "Command.Execute",
                            delegate
                            {
                            }
                        );
                    }
                );
            }
        }

        [Fact]
        public void Subscription_Dispose_RemovesExactHandlerOnce()
        {
            var dispatcher = new NetworkMessageDispatcher();

            NetworkMessageSubscription subscription =
                dispatcher.RegisterHandler(
                    "Command.Execute",
                    delegate
                    {
                    }
                );

            subscription.Dispose();
            subscription.Dispose();

            bool dispatched =
                dispatcher.TryDispatch(
                    new NetworkEnvelope(
                        "Command.Execute",
                        101UL,
                        false,
                        new byte[0]
                    ),
                    101UL,
                    true,
                    false,
                    out NetworkReceiveContext context
                );

            Assert.False(dispatched);
            Assert.Null(context);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RegisterHandler_InvalidMessageType_Throws(
            string? messageType
        )
        {
            var dispatcher = new NetworkMessageDispatcher();

            Assert.ThrowsAny<ArgumentException>(
                delegate
                {
                    dispatcher.RegisterHandler(
                        messageType!,
                        delegate
                        {
                        }
                    );
                }
            );
        }

        [Fact]
        public void RegisterHandler_NullHandler_Throws()
        {
            var dispatcher = new NetworkMessageDispatcher();

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    dispatcher.RegisterHandler(
                        "Command.Execute",
                        null!
                    );
                }
            );
        }
    }
}
