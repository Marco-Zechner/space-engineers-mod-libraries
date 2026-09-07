using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiEndpointContractTests
    {
        [Fact]
        public void Requirement_NormalizesAndStoresValues()
        {
            var requirement = new ApiEndpointRequirement("  RegisterCommand  ", typeof(Action<string>));

            Assert.Equal("RegisterCommand", requirement.Name);
            Assert.Equal(typeof(Action<string>), requirement.DelegateType);
        }

        [Fact]
        public void Requirement_StoresExpectedTypeWithoutReflection()
        {
            var requirement = new ApiEndpointRequirement("RegisterCommand", typeof(string));
            Assert.Equal(typeof(string), requirement.DelegateType);
        }
        [Fact]
        public void Constructor_DuplicateNames_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                {
                    new ApiEndpointContract(
                        new(" Ping ", typeof(Action)), 
                        new("Ping", typeof(Action))
                    );
                }
            );
        }

        [Fact]
        public void Validate_CompatibleEndpoints_ReturnsCompatible()
        {
            ApiEndpointContract contract = CreateContract();

            ApiConnection connection = CreateConnection(
                new Dictionary<string, Delegate>
                {
                    { "RegisterCommand", (Action<string>)delegate { } },
                    { "TryExecute", (Func<string, bool>)delegate { return true; } }
                }
            );

            ApiEndpointContractValidation validation = contract.Validate(connection);

            Assert.True(validation.IsCompatible);
            Assert.Empty(validation.Issues);
        }

        [Fact]
        public void Validate_MissingEndpoint_ReturnsIssue()
        {
            ApiEndpointContract contract = CreateContract();

            ApiConnection connection = CreateConnection(
                new Dictionary<string, Delegate>
                {
                    { "RegisterCommand", (Action<string>)delegate { } }
                }
            );

            ApiEndpointContractValidation validation = contract.Validate(connection);

            Assert.False(validation.IsCompatible);
            Assert.Single(validation.Issues);

            ApiEndpointContractIssue issue = validation.Issues[0];

            Assert.Equal(ApiEndpointContractIssueKind.MissingEndpoint, issue.Kind);
            Assert.Equal("TryExecute", issue.Requirement.Name);
            Assert.Null(issue.ActualDelegateType);
        }

        [Fact]
        public void Validate_WrongDelegateType_ReturnsIssue()
        {
            ApiEndpointContract contract = CreateContract();

            ApiConnection connection = CreateConnection(
                new Dictionary<string, Delegate>
                {
                    { "RegisterCommand", (Action<int>)delegate { } },
                    { "TryExecute", (Func<string, bool>)delegate { return true; } }
                }
            );

            ApiEndpointContractValidation validation = contract.Validate(connection);

            Assert.False(validation.IsCompatible);
            Assert.Single(validation.Issues);

            ApiEndpointContractIssue issue = validation.Issues[0];

            Assert.Equal(ApiEndpointContractIssueKind.WrongDelegateType, issue.Kind);
            Assert.Equal(typeof(Action<string>), issue.Requirement.DelegateType);
            Assert.Equal(typeof(Action<int>), issue.ActualDelegateType);
        }

        [Fact]
        public void Validate_MultipleProblems_ReturnsEveryIssue()
        {
            ApiEndpointContract contract = CreateContract();

            ApiConnection connection = CreateConnection(
                new Dictionary<string, Delegate>
                {
                    { "RegisterCommand", (Action<int>)delegate { } }
                }
            );

            ApiEndpointContractValidation validation = contract.Validate(connection);

            Assert.False(validation.IsCompatible);
            Assert.Equal(2, validation.Issues.Count);
        }

        [Fact]
        public void EnsureCompatible_IncompatibleConnection_Throws()
        {
            ApiEndpointContract contract = CreateContract();

            ApiConnection connection = CreateConnection(new Dictionary<string, Delegate>());

            var exception = Assert.Throws<ApiEndpointContractException>(() => contract.EnsureCompatible(connection));

            Assert.False(exception.Validation.IsCompatible);
            Assert.Equal(2, exception.Validation.Issues.Count);
            Assert.Contains("RegisterCommand", exception.Message);
            Assert.Contains("TryExecute", exception.Message);
        }

        [Fact]
        public void EnsureCompatible_CompatibleConnection_DoesNotThrow()
        {
            ApiEndpointContract contract = CreateContract();

            ApiConnection connection = CreateConnection(
                new Dictionary<string, Delegate>
                {
                    { "RegisterCommand", (Action<string>)delegate { } },
                    { "TryExecute", (Func<string, bool>)delegate { return true; } }
                }
            );

            contract.EnsureCompatible(connection);
        }

        private static ApiEndpointContract CreateContract() => new(
            new("RegisterCommand", typeof(Action<string>)), 
            new("TryExecute", typeof(Func<string, bool>))
        );

        private static ApiConnection CreateConnection(IDictionary<string, Delegate> endpoints)
        {
            var announcement = new ApiAnnouncement(
                new("Mz.CommandApiMod", "Command API", new(1, 4, 0)),
                new("Mz.CommandAPI", new(1, 5, 0)),
                Guid.NewGuid(),
                Guid.Empty,
                endpoints
            );

            return new(announcement);
        }
    }
}