using System;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiDiscoveryRequestTests
    {
        [Fact]
        public void Constructor_StoresNormalizedApiIdAndCorrelationId()
        {
            var correlationId = Guid.NewGuid();

            var request = new ApiDiscoveryRequest(
                "  Mz.CommandAPI  ",
                correlationId
            );

            Assert.Equal("Mz.CommandAPI", request.ApiId);
            Assert.Equal(correlationId, request.CorrelationId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidApiId_ThrowsArgumentException(
            string? apiId
        )
        {
            Assert.ThrowsAny<ArgumentException>(
                delegate
                {
                    new ApiDiscoveryRequest(
                        apiId!,
                        Guid.NewGuid()
                    );
                }
            );
        }

        [Fact]
        public void Constructor_EmptyCorrelationId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                delegate
                {
                    new ApiDiscoveryRequest(
                        "Mz.CommandAPI",
                        Guid.Empty
                    );
                }
            );
        }
    }
}