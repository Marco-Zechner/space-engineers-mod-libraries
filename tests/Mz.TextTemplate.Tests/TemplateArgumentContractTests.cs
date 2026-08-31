using System;
using Xunit;

namespace Mz.TextTemplate.Tests
{
    public sealed class TemplateArgumentContractTests
    {
        [Fact]
        public void Analyze_DefaultTagDefinition_RejectsArguments()
        {
            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "tab",
                            TemplateTagRole.Command
                        )
                    },
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{tab 4 anything=true}}"
                    ),
                    language
                );

            var diagnostics =
                result.Diagnostics;

            Assert.Equal(2, diagnostics.Length);
            Assert.Equal(
                TemplateLanguageAnalyzer.UnexpectedPositionalArgumentDiagnosticCode,
                diagnostics[0].Code
            );
            Assert.Equal(
                TemplateLanguageAnalyzer.UnknownNamedArgumentDiagnosticCode,
                diagnostics[1].Code
            );
        }

        [Fact]
        public void Analyze_ExplicitEmptyContract_RejectsAllArguments()
        {
            var contract =
                new TemplateArgumentContract(
                    new TemplatePositionalArgumentDefinition[0],
                    new TemplateNamedArgumentDefinition[0]
                );

            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "tag",
                            TemplateTagRole.Command,
                            contract
                        )
                    },
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{tag 1 extra=true}}"
                    ),
                    language
                );

            var diagnostics =
                result.Diagnostics;

            Assert.Equal(2, diagnostics.Length);
            Assert.Equal(
                TemplateLanguageAnalyzer.UnexpectedPositionalArgumentDiagnosticCode,
                diagnostics[0].Code
            );
            Assert.Equal(
                TemplateLanguageAnalyzer.UnknownNamedArgumentDiagnosticCode,
                diagnostics[1].Code
            );
        }

        [Fact]
        public void Analyze_RequiredPositionalArgument_MustBePresent()
        {
            var contract =
                new TemplateArgumentContract(
                    new[]
                    {
                        new TemplatePositionalArgumentDefinition(
                            true,
                            new[]
                            {
                                TemplateArgumentValueKind.Number
                            }
                        )
                    },
                    new TemplateNamedArgumentDefinition[0]
                );

            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "tab",
                            TemplateTagRole.Command,
                            contract
                        )
                    },
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{tab}}"
                    ),
                    language
                );

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateLanguageAnalyzer.MissingRequiredPositionalArgumentDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                new SourceSpan(2, 3),
                diagnostic.Span
            );
        }

        [Fact]
        public void Analyze_OptionalPositionalArgument_MayBeOmitted()
        {
            var contract =
                new TemplateArgumentContract(
                    new[]
                    {
                        new TemplatePositionalArgumentDefinition(
                            false,
                            new[]
                            {
                                TemplateArgumentValueKind.Number
                            }
                        )
                    },
                    new TemplateNamedArgumentDefinition[0]
                );

            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "tab",
                            TemplateTagRole.Command,
                            contract
                        )
                    },
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{tab}}"
                    ),
                    language
                );

            Assert.False(result.HasErrors);
        }

        [Fact]
        public void Analyze_RequiredNamedArgument_MustBePresent()
        {
            var contract =
                new TemplateArgumentContract(
                    new TemplatePositionalArgumentDefinition[0],
                    new[]
                    {
                        new TemplateNamedArgumentDefinition(
                            "format",
                            true,
                            new[]
                            {
                                TemplateArgumentValueKind.String
                            }
                        )
                    }
                );

            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "time",
                            TemplateTagRole.Value,
                            contract
                        )
                    },
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{time}}"
                    ),
                    language
                );

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateLanguageAnalyzer.MissingRequiredNamedArgumentDiagnosticCode,
                diagnostic.Code
            );
        }

        [Fact]
        public void Analyze_UnknownNamedArgument_ReportsNameSpan()
        {
            var contract =
                new TemplateArgumentContract(
                    new TemplatePositionalArgumentDefinition[0],
                    new TemplateNamedArgumentDefinition[0]
                );

            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "name",
                            TemplateTagRole.Value,
                            contract
                        )
                    },
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{name typo=1}}"
                    ),
                    language
                );

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateLanguageAnalyzer.UnknownNamedArgumentDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                new SourceSpan(7, 4),
                diagnostic.Span
            );
        }

        [Fact]
        public void Analyze_DuplicateNamedArgument_ReportsSecondName()
        {
            var contract =
                new TemplateArgumentContract(
                    new TemplatePositionalArgumentDefinition[0],
                    new[]
                    {
                        new TemplateNamedArgumentDefinition(
                            "wrap",
                            false,
                            new[]
                            {
                                TemplateArgumentValueKind.Number
                            }
                        )
                    }
                );

            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "tab",
                            TemplateTagRole.Command,
                            contract
                        )
                    },
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{tab wrap=1 wrap=2}}"
                    ),
                    language
                );

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateLanguageAnalyzer.DuplicateNamedArgumentDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                new SourceSpan(13, 4),
                diagnostic.Span
            );
        }

        [Fact]
        public void Analyze_PositionalValueKind_IsValidated()
        {
            var contract =
                new TemplateArgumentContract(
                    new[]
                    {
                        new TemplatePositionalArgumentDefinition(
                            true,
                            new[]
                            {
                                TemplateArgumentValueKind.Number
                            }
                        )
                    },
                    new TemplateNamedArgumentDefinition[0]
                );

            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "tab",
                            TemplateTagRole.Command,
                            contract
                        )
                    },
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{tab \"four\"}}"
                    ),
                    language
                );

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateLanguageAnalyzer.InvalidArgumentValueKindDiagnosticCode,
                diagnostic.Code
            );
        }

        [Fact]
        public void Analyze_NamedValueKind_IsValidated()
        {
            var contract =
                new TemplateArgumentContract(
                    new TemplatePositionalArgumentDefinition[0],
                    new[]
                    {
                        new TemplateNamedArgumentDefinition(
                            "ellipsis",
                            false,
                            new[]
                            {
                                TemplateArgumentValueKind.Boolean
                            }
                        )
                    }
                );

            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "name",
                            TemplateTagRole.Value,
                            contract
                        )
                    },
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{name ellipsis=1}}"
                    ),
                    language
                );

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateLanguageAnalyzer.InvalidArgumentValueKindDiagnosticCode,
                diagnostic.Code
            );
        }

        [Fact]
        public void Analyze_BlockArgumentContract_IsApplied()
        {
            var contract =
                new TemplateArgumentContract(
                    new TemplatePositionalArgumentDefinition[0],
                    new[]
                    {
                        new TemplateNamedArgumentDefinition(
                            "enabled",
                            true,
                            new[]
                            {
                                TemplateArgumentValueKind.Boolean
                            }
                        )
                    }
                );

            var language =
                new TemplateLanguageDefinition(
                    new TemplateTagDefinition[0],
                    new[]
                    {
                        new TemplateBlockDefinition(
                            "group",
                            contract
                        )
                    }
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{#group enabled=1}}x{{/group}}"
                    ),
                    language
                );

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateLanguageAnalyzer.InvalidArgumentValueKindDiagnosticCode,
                diagnostic.Code
            );
        }

        [Fact]
        public void Analyze_DefaultBlockDefinition_RejectsArguments()
        {
            var language =
                new TemplateLanguageDefinition(
                    new TemplateTagDefinition[0],
                    new[]
                    {
                        new TemplateBlockDefinition(
                            "group"
                        )
                    }
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{#group enabled=true}}x{{/group}}"
                    ),
                    language
                );

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateLanguageAnalyzer.UnknownNamedArgumentDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                new SourceSpan(9, 7),
                diagnostic.Span
            );
        }

        [Theory]
        [InlineData("1limit")]
        [InlineData("bad/name")]
        [InlineData("bad.name")]
        [InlineData("bad name")]
        public void NamedArgumentDefinition_InvalidName_IsRejected(string name)
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new TemplateNamedArgumentDefinition(
                    name,
                    false,
                    new[]
                    {
                        TemplateArgumentValueKind.Number
                    }
                )
            );

            Assert.Equal("name", exception.ParamName);
        }

        [Fact]
        public void TagDefinition_NullArgumentContract_IsRejected()
        {
            var exception =
                Assert.Throws<ArgumentNullException>(
                    () =>
                        new TemplateTagDefinition(
                            "tag",
                            TemplateTagRole.Command,
                            null!
                        )
                );

            Assert.Equal(
                "argumentContract",
                exception.ParamName
            );
        }

        [Fact]
        public void BlockDefinition_NullArgumentContract_IsRejected()
        {
            var exception =
                Assert.Throws<ArgumentNullException>(
                    () =>
                        new TemplateBlockDefinition(
                            "block",
                            null!
                        )
                );

            Assert.Equal(
                "argumentContract",
                exception.ParamName
            );
        }
        [Fact]
        public void ArgumentContract_RequiredPositionalsCannotFollowOptionalOnes()
        {
            var exception =
                Assert.Throws<ArgumentException>(
                    () =>
                        new TemplateArgumentContract(
                            new[]
                            {
                                new TemplatePositionalArgumentDefinition(
                                    false,
                                    new[]
                                    {
                                        TemplateArgumentValueKind.Number
                                    }
                                ),
                                new TemplatePositionalArgumentDefinition(
                                    true,
                                    new[]
                                    {
                                        TemplateArgumentValueKind.Number
                                    }
                                )
                            },
                            new TemplateNamedArgumentDefinition[0]
                        )
                );

            Assert.Equal(
                "positionalArguments",
                exception.ParamName
            );
        }

        [Fact]
        public void ArgumentContract_DefinitionsAndKindsAreDefensiveAndValidated()
        {
            var kinds =
                new[]
                {
                    TemplateArgumentValueKind.Number
                };

            var positional =
                new TemplatePositionalArgumentDefinition(
                    true,
                    kinds
                );

            kinds[0] =
                TemplateArgumentValueKind.String;

            Assert.Equal(
                TemplateArgumentValueKind.Number,
                positional.AllowedValueKinds[0]
            );

            var namedDefinition =
                new TemplateNamedArgumentDefinition(
                    "wrap",
                    false,
                    new[]
                    {
                        TemplateArgumentValueKind.Number
                    }
                );

            var contract =
                new TemplateArgumentContract(
                    new[]
                    {
                        positional
                    },
                    new[]
                    {
                        namedDefinition
                    }
                );

            var positionalDefinitions =
                contract.PositionalArguments;

            positionalDefinitions[0] =
                new TemplatePositionalArgumentDefinition(
                    false,
                    new[]
                    {
                        TemplateArgumentValueKind.String
                    }
                );

            Assert.True(
                contract.PositionalArguments[0].Required
            );

            Assert.Throws<ArgumentException>(
                () =>
                    new TemplateNamedArgumentDefinition(
                        "bad",
                        false,
                        new[]
                        {
                            TemplateArgumentValueKind.Missing
                        }
                    )
            );
        }
    }
}