using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiConnectionTests
    {
        [Fact]
        public void Constructor_CopiesEndpoints()
        {
            Action endpoint = delegate { };

            var endpoints = new Dictionary<string, Delegate>
            {
                { "Ping", endpoint }
            };

            var announcement = new ApiAnnouncement(
                CreateProviderIdentity(),
                CreateDescriptor(),
                Guid.NewGuid(),
                Guid.Empty,
                endpoints
            );
            
            var connection = new ApiConnection(announcement);

            endpoints.Clear();

            Assert.Same(endpoint, connection.Endpoints["Ping"]);
        }

        [Fact]
        public void TryGetEndpoint_MatchingDelegate_ReturnsTrue()
        {
            Action<string> endpoint = delegate { };

            ApiConnection connection = CreateConnection("Echo", endpoint);

            bool found = connection.TryGetEndpoint("Echo", out Action<string> result);

            Assert.True(found);
            Assert.Same(endpoint, result);
        }

        [Fact]
        public void TryGetEndpoint_MissingEndpoint_ReturnsFalse()
        {
            ApiConnection connection = CreateConnection("Ping", (Action)delegate { });

            bool found = connection.TryGetEndpoint("Missing", out Action result);

            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void TryGetEndpoint_WrongDelegateType_ReturnsFalse()
        {
            ApiConnection connection = CreateConnection("Ping", (Action)delegate { });

            bool found = connection.TryGetEndpoint("Ping", out Action<string> result);

            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void TryGetEndpoint_NonDelegateType_ReturnsFalse()
        {
            ApiConnection connection = CreateConnection("Ping", (Action)delegate { });
            bool found = connection.TryGetEndpoint("Ping", out string result);
            Assert.False(found);
            Assert.Null(result);
        }
        private static ApiConnection CreateConnection(string endpointName, Delegate endpoint)
        {
            var announcement = new ApiAnnouncement(
                CreateProviderIdentity(),
                CreateDescriptor(),
                Guid.NewGuid(),
                Guid.Empty,
                new Dictionary<string, Delegate>
                {
                    { endpointName, endpoint }
                }
            );
            
            return new(announcement);
        }

        private static ApiDescriptor CreateDescriptor() 
            => new("Mz.CommandAPI", new(1, 0, 0));

        private static ApiModIdentity CreateProviderIdentity() 
            => new("Mz.CommandApiMod", "Command API", new(1, 4, 0));
    }
}