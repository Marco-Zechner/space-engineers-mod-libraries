using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiDisconnectedEventArgsTests
    {
        [Fact]
        public void Constructor_StoresConnectionAndReason()
        {
            ApiConnection connection = CreateConnection();

            var eventArgs = new ApiDisconnectedEventArgs(
                connection,
                ApiDisconnectReason.RediscoveryRequested
            );

            Assert.Same(
                connection,
                eventArgs.PreviousConnection
            );

            Assert.Equal(
                ApiDisconnectReason.RediscoveryRequested,
                eventArgs.Reason
            );
        }

        [Fact]
        public void Constructor_NullConnection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new ApiDisconnectedEventArgs(
                        null!,
                        ApiDisconnectReason.ConsumerRequested
                    );
                }
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(100)]
        public void Constructor_InvalidReason_ThrowsArgumentOutOfRangeException(
            int numericReason
        )
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                delegate
                {
                    new ApiDisconnectedEventArgs(
                        CreateConnection(),
                        (ApiDisconnectReason)numericReason
                    );
                }
            );
        }

        private static ApiConnection CreateConnection()
        {
            return new ApiConnection(
                new ApiDescriptor(
                    "Mz.CommandAPI",
                    new SemanticVersion(1, 0, 0)
                ),
                new Dictionary<string, Delegate>()
            );
        }
    }
}