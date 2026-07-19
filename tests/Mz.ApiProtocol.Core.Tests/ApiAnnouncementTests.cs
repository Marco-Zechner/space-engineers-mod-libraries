using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiAnnouncementTests
    {
        [Fact]
        public void Constructor_CopiesAndNormalizesEndpoints()
        {
            Action endpoint =
                delegate
                {
                };

            var source = new Dictionary<string, Delegate>
            {
                {
                    "  RegisterCommand  ",
                    endpoint
                }
            };

            var announcement = new ApiAnnouncement(
                CreateDescriptor(),
                Guid.Empty,
                source
            );

            source.Clear();

            var stored = Assert.Single(
                announcement.Endpoints
            ).Value;

            Assert.Same(endpoint, stored);

            Assert.True(
                announcement.Endpoints.ContainsKey(
                    "RegisterCommand"
                )
            );
        }

        [Fact]
        public void Constructor_UsesCaseSensitiveEndpointNames()
        {
            Action upper =
                delegate
                {
                };

            Action lower =
                delegate
                {
                };

            var endpoints = new Dictionary<string, Delegate>
            {
                { "Register", upper },
                { "register", lower }
            };

            var announcement = new ApiAnnouncement(
                CreateDescriptor(),
                Guid.Empty,
                endpoints
            );

            Assert.Equal(2, announcement.Endpoints.Count);
        }

        [Fact]
        public void Constructor_NullDescriptor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new ApiAnnouncement(
                        null!,
                        Guid.Empty,
                        new Dictionary<string, Delegate>()
                    );
                }
            );
        }

        [Fact]
        public void Constructor_NullEndpoints_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new ApiAnnouncement(
                        CreateDescriptor(),
                        Guid.Empty,
                        null!
                    );
                }
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidEndpointName_ThrowsArgumentException(
            string endpointName
        )
        {
            var endpoints = new Dictionary<string, Delegate>
            {
                {
                    endpointName,
                    (Action)delegate
                    {
                    }
                }
            };

            Assert.Throws<ArgumentException>(
                delegate
                {
                    new ApiAnnouncement(
                        CreateDescriptor(),
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

            Assert.Throws<ArgumentException>(
                delegate
                {
                    new ApiAnnouncement(
                        CreateDescriptor(),
                        Guid.Empty,
                        endpoints
                    );
                }
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