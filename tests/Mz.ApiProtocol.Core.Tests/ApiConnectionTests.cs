using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiConnectionTests
    {
        [Fact]
        public void Constructor_CopiesEndpoints()
        {
            Action endpoint =
                delegate
                {
                };

            var endpoints = new Dictionary<string, Delegate>
            {
                { "Ping", endpoint }
            };

            var announcement = new ApiAnnouncement(
                CreateDescriptor(),
                Guid.NewGuid(),
                Guid.Empty,
                endpoints
            );
            
            var connection = new ApiConnection(
                announcement
            );

            endpoints.Clear();

            Assert.Same(
                endpoint,
                connection.Endpoints["Ping"]
            );
        }

        [Fact]
        public void TryGetEndpoint_MatchingDelegate_ReturnsTrue()
        {
            Action<string> endpoint =
                delegate
                {
                };

            var connection = CreateConnection(
                "Echo",
                endpoint
            );

            var found = connection.TryGetEndpoint(
                "Echo",
                out Action<string> result
            );

            Assert.True(found);
            Assert.Same(endpoint, result);
        }

        [Fact]
        public void TryGetEndpoint_MissingEndpoint_ReturnsFalse()
        {
            var connection = CreateConnection(
                "Ping",
                (Action)delegate
                {
                }
            );

            var found = connection.TryGetEndpoint(
                "Missing",
                out Action result
            );

            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void TryGetEndpoint_WrongDelegateType_ReturnsFalse()
        {
            var connection = CreateConnection(
                "Ping",
                (Action)delegate
                {
                }
            );

            var found = connection.TryGetEndpoint(
                "Ping",
                out Action<string> result
            );

            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void TryGetEndpoint_NonDelegateType_ThrowsArgumentException()
        {
            var connection = CreateConnection(
                "Ping",
                (Action)delegate
                {
                }
            );

            Assert.Throws<ArgumentException>(
                delegate
                {
                    connection.TryGetEndpoint(
                        "Ping",
                        out string result
                    );
                }
            );
        }

        private static ApiConnection CreateConnection(
            string endpointName,
            Delegate endpoint
        )
        {
            var announcement = new ApiAnnouncement(
                CreateDescriptor(),
                Guid.NewGuid(),
                Guid.Empty,
                new Dictionary<string, Delegate>
                {
                    { endpointName, endpoint }
                }
            );
            
            return new ApiConnection(
                announcement
            );
        }

        private static ApiDescriptor CreateDescriptor()
        {
            return new ApiDescriptor(
                "Mz.CommandAPI",
                new SemanticVersion(1, 0, 0)
            );
        }
    }
}