using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiAnnouncementTests
    {
        [Fact]
        public void Constructor_CopiesAndNormalizesEndpoints()
        {
            Action endpoint = delegate { };

            var source = new Dictionary<string, Delegate>
            {
                { "  RegisterCommand  ", endpoint }
            };
            
            var announcement = new ApiAnnouncement(
                CreateProviderIdentity(),
                CreateDescriptor(),
                Guid.NewGuid(),
                Guid.Empty,
                source
            );

            source.Clear();

            Delegate stored = Assert.Single(announcement.Endpoints).Value;

            Assert.Same(endpoint, stored);
            Assert.True(announcement.Endpoints.ContainsKey("RegisterCommand"));
        }

        [Fact]
        public void Constructor_UsesCaseSensitiveEndpointNames()
        {
            var endpoints = new Dictionary<string, Delegate>
            {
                { "Register", (Action)Upper },
                { "register", (Action)Lower }
            };

            var announcement = new ApiAnnouncement(
                CreateProviderIdentity(),
                CreateDescriptor(),
                Guid.NewGuid(),
                Guid.Empty,
                endpoints
            );

            Assert.Equal(2, announcement.Endpoints.Count);
            return;

            void Lower() { }
            void Upper() { }
        }

        [Fact]
        public void Constructor_NullDescriptor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                {
                    new ApiAnnouncement(
                        CreateProviderIdentity(),
                        null!,
                        Guid.NewGuid(),
                        Guid.Empty,
                        new Dictionary<string, Delegate>()
                    );
                }
            );
        }

        [Fact]
        public void Constructor_NullEndpoints_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                {
                    new ApiAnnouncement(
                        CreateProviderIdentity(),
                        CreateDescriptor(),
                        Guid.NewGuid(),
                        Guid.Empty,
                        null!
                    );
                }
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidEndpointName_ThrowsArgumentException(string endpointName)
        {
            var endpoints = new Dictionary<string, Delegate>
            {
                { endpointName, (Action)delegate { } }
            };

            Assert.Throws<ArgumentException>(() =>
                {
                    new ApiAnnouncement(
                        CreateProviderIdentity(),
                        CreateDescriptor(),
                        Guid.NewGuid(),
                        Guid.Empty,
                        endpoints
                    );
                }
            );
        }

        [Fact]
        public void Constructor_NullEndpointDelegate_ThrowsArgumentException()
        {
            var endpoints = new Dictionary<string, Delegate>
            {
                { "RegisterCommand", null! }
            };

            Assert.Throws<ArgumentException>(() =>
                {
                    new ApiAnnouncement(
                        CreateProviderIdentity(),
                        CreateDescriptor(),
                        Guid.NewGuid(),
                        Guid.Empty,
                        endpoints
                    );
                }
            );
        }

        private static ApiDescriptor CreateDescriptor() 
            => new("Mz.CommandAPI", new(1, 0, 0));

        private static ApiModIdentity CreateProviderIdentity() 
            => new("Mz.CommandApiMod", "Command API", new(1, 4, 0));
    }
}