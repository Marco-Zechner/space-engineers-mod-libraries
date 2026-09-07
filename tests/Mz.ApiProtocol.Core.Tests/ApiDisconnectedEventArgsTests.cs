using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiDisconnectedEventArgsTests
    {
        [Fact]
        public void Constructor_StoresConnectionAndReason()
        {
            ApiConnection connection = CreateConnection();

            var eventArgs = new ApiDisconnectedEventArgs(connection, ApiDisconnectReason.RediscoveryRequested);

            Assert.Same(connection, eventArgs.PreviousConnection);
            Assert.Equal(ApiDisconnectReason.RediscoveryRequested, eventArgs.Reason);
        }

        [Fact]
        public void Constructor_NullConnection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                {
                    new ApiDisconnectedEventArgs(null!, ApiDisconnectReason.ConsumerRequested);
                }
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        [InlineData(100)]
        public void Constructor_InvalidReason_ThrowsArgumentException(int numericReason)
        {
            Assert.Throws<ArgumentException>(() =>
                {
                    new ApiDisconnectedEventArgs(CreateConnection(), (ApiDisconnectReason)numericReason);
                }
            );
        }

        private static ApiConnection CreateConnection()
        {
            var announcement = new ApiAnnouncement(
                CreateProviderIdentity(),
                new("Mz.CommandAPI", new(1, 0, 0)),
                Guid.NewGuid(),
                Guid.Empty,
                new Dictionary<string, Delegate>()
            );
            
            return new(announcement);
        }
        
                        
        private static ApiModIdentity CreateProviderIdentity()
            => new("Mz.CommandApiMod", "Command API", new(1, 4, 0));
    }
}