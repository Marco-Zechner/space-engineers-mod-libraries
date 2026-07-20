using Xunit;

namespace Mz.Networking.Tests
{
    public sealed class NetworkMessageProcessorTests
    {
        [Fact]
        public void Process_ServerReceive_CorrectsSpoofedSenderBeforeHandler()
        {
            var envelope = new NetworkEnvelope(
                "Command.Execute",
                111UL,
                false,
                new byte[] { 1, 2, 3 }
            );

            NetworkReceiveContext? observedContext = null;

            NetworkReceiveContext result =
                NetworkMessageProcessor.Process(
                    envelope,
                    222UL,
                    true,
                    delegate(NetworkReceiveContext context)
                    {
                        observedContext = context;

                        Assert.Equal(
                            222UL,
                            context.Envelope.OriginalSenderId
                        );
                    }
                );

            Assert.Same(result, observedContext);

            Assert.Equal(
                222UL,
                result.Envelope.OriginalSenderId
            );

            Assert.True(result.OriginalSenderWasCorrected);
            Assert.True(result.RequiresSerialization);
        }

        [Fact]
        public void Process_ClientReceive_PreservesServerProvidedOriginalSender()
        {
            var envelope = new NetworkEnvelope(
                "Command.Result",
                333UL,
                true,
                new byte[] { 4, 5, 6 }
            );

            NetworkReceiveContext result =
                NetworkMessageProcessor.Process(
                    envelope,
                    999UL,
                    false,
                    delegate
                    {
                    }
                );

            Assert.Equal(
                333UL,
                result.Envelope.OriginalSenderId
            );

            Assert.False(result.OriginalSenderWasCorrected);
            Assert.False(result.RequiresSerialization);
        }

        [Fact]
        public void Process_HandlerCanRequestRelayAndReserialization()
        {
            var envelope = new NetworkEnvelope(
                "Command.Execute",
                444UL,
                false,
                new byte[] { 7, 8, 9 }
            );

            NetworkReceiveContext result =
                NetworkMessageProcessor.Process(
                    envelope,
                    444UL,
                    true,
                    delegate(NetworkReceiveContext context)
                    {
                        context.RelayMode =
                            NetworkRelayMode.ReturnToSender;

                        context.RequiresSerialization = true;
                    }
                );

            Assert.Equal(
                NetworkRelayMode.ReturnToSender,
                result.RelayMode
            );

            Assert.True(result.RequiresSerialization);
        }
    }
}