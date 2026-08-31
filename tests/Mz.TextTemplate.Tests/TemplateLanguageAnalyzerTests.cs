using System;
using Xunit;

namespace Mz.TextTemplate.Tests
{
    public sealed class TemplateLanguageAnalyzerTests
    {
        [Fact]
        public void Analyze_KnownValueAndCommandTags_ReclassifiesNames()
        {
            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "name",
                            TemplateTagRole.Value
                        ),
                        new TemplateTagDefinition(
                            "tab",
                            TemplateTagRole.Command
                        )
                    },
                    new TemplateBlockDefinition[0]
                );

            var parse =
                TemplateParser.Parse(
                    "{{name}} {{tab}}"
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    parse,
                    language
                );

            Assert.False(result.HasErrors);
            Assert.Empty(result.Diagnostics);

            var syntax =
                result.SyntaxSpans;

            Assert.Equal(
                TemplateSyntaxKind.ValueName,
                syntax[1].Kind
            );
            Assert.Equal(
                new SourceSpan(2, 4),
                syntax[1].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.CommandName,
                syntax[5].Kind
            );
            Assert.Equal(
                new SourceSpan(11, 3),
                syntax[5].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.TagName,
                parse.SyntaxSpans[1].Kind
            );
        }

        [Fact]
        public void Analyze_UnknownTag_ReportsExactNameAndKeepsGenericSyntax()
        {
            var language =
                new TemplateLanguageDefinition(
                    new TemplateTagDefinition[0],
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{missing}}"
                    ),
                    language
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateLanguageAnalyzer.UnknownTagDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                new SourceSpan(2, 7),
                diagnostic.Span
            );

            Assert.Equal(
                TemplateSyntaxKind.TagName,
                result.SyntaxSpans[1].Kind
            );
        }

        [Fact]
        public void Analyze_KnownBlock_AnalyzesNestedTagsRecursively()
        {
            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "name",
                            TemplateTagRole.Value
                        )
                    },
                    new[]
                    {
                        new TemplateBlockDefinition(
                            "first"
                        )
                    }
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{#first}}{{name}}{{/first}}"
                    ),
                    language
                );

            Assert.False(result.HasErrors);
            Assert.Empty(result.Diagnostics);

            var syntax =
                result.SyntaxSpans;

            bool valueFound = false;
            bool blockFound = false;

            for (
                int index = 0;
                index < syntax.Length;
                index++
            )
            {
                if (
                    syntax[index].Kind
                    == TemplateSyntaxKind.ValueName
                )
                {
                    valueFound = true;
                }

                if (
                    syntax[index].Kind
                    == TemplateSyntaxKind.BlockName
                )
                {
                    blockFound = true;
                }
            }

            Assert.True(valueFound);
            Assert.True(blockFound);
        }

        [Fact]
        public void Analyze_UnknownBlock_ReportsOpeningNameOnce()
        {
            var language =
                new TemplateLanguageDefinition(
                    new TemplateTagDefinition[0],
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{#missing}}x{{/missing}}"
                    ),
                    language
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateLanguageAnalyzer.UnknownBlockDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                new SourceSpan(3, 7),
                diagnostic.Span
            );
        }

        [Fact]
        public void Analyze_InvalidTagName_DoesNotCascadeUnknownTag()
        {
            var language =
                new TemplateLanguageDefinition(
                    new TemplateTagDefinition[0],
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{1bad}}"
                    ),
                    language
                );

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.InvalidTagNameDiagnosticCode,
                diagnostic.Code
            );
        }

        [Fact]
        public void Analyze_InvalidBlockName_DoesNotCascadeUnknownBlock()
        {
            var language =
                new TemplateLanguageDefinition(
                    new TemplateTagDefinition[0],
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{#1bad}}x{{/1bad}}"
                    ),
                    language
                );

            var diagnostics =
                result.Diagnostics;

            Assert.Equal(2, diagnostics.Length);

            Assert.Equal(
                TemplateParser.InvalidBlockNameDiagnosticCode,
                diagnostics[0].Code
            );
            Assert.Equal(
                TemplateParser.InvalidBlockNameDiagnosticCode,
                diagnostics[1].Code
            );
        }

        [Fact]
        public void Analyze_CombinedDiagnostics_AreReturnedInSourceOrder()
        {
            var language =
                new TemplateLanguageDefinition(
                    new TemplateTagDefinition[0],
                    new TemplateBlockDefinition[0]
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{missing}} {{1bad}} {{other}}"
                    ),
                    language
                );

            var diagnostics =
                result.Diagnostics;

            Assert.Equal(3, diagnostics.Length);

            for (
                int index = 1;
                index < diagnostics.Length;
                index++
            )
            {
                Assert.True(
                    diagnostics[index - 1].Span.Start
                    <= diagnostics[index].Span.Start
                );
            }

            Assert.Equal(
                TemplateLanguageAnalyzer.UnknownTagDiagnosticCode,
                diagnostics[0].Code
            );
            Assert.Equal(
                TemplateParser.InvalidTagNameDiagnosticCode,
                diagnostics[1].Code
            );
            Assert.Equal(
                TemplateLanguageAnalyzer.UnknownTagDiagnosticCode,
                diagnostics[2].Code
            );
        }

        [Theory]
        [InlineData("1name")]
        [InlineData("rank..server")]
        [InlineData("rank.")]
        [InlineData("rank/server")]
        [InlineData("bad name")]
        public void TagDefinition_InvalidName_IsRejected(string name)
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new TemplateTagDefinition(name, TemplateTagRole.Value)
            );

            Assert.Equal("name", exception.ParamName);
        }

        [Theory]
        [InlineData("1block")]
        [InlineData("group..nested")]
        [InlineData("group.")]
        [InlineData("group/nested")]
        [InlineData("bad name")]
        public void BlockDefinition_InvalidName_IsRejected(string name)
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new TemplateBlockDefinition(name)
            );

            Assert.Equal("name", exception.ParamName);
        }

        [Fact]
        public void TagDefinition_UnsupportedRole_IsRejected()
        {
            var exception =
                Assert.Throws<ArgumentException>(
                    () =>
                        new TemplateTagDefinition(
                            "name",
                            (TemplateTagRole)123
                        )
                );

            Assert.Equal(
                "role",
                exception.ParamName
            );
        }
        [Fact]
        public void LanguageDefinition_DuplicateTagNames_AreRejected()
        {
            var exception =
                Assert.Throws<ArgumentException>(
                    () =>
                        new TemplateLanguageDefinition(
                            new[]
                            {
                                new TemplateTagDefinition(
                                    "name",
                                    TemplateTagRole.Value
                                ),
                                new TemplateTagDefinition(
                                    "name",
                                    TemplateTagRole.Command
                                )
                            },
                            new TemplateBlockDefinition[0]
                        )
                );

            Assert.Equal(
                "tags",
                exception.ParamName
            );
        }

        [Fact]
        public void LanguageDefinition_DuplicateBlockNames_AreRejected()
        {
            var exception =
                Assert.Throws<ArgumentException>(
                    () =>
                        new TemplateLanguageDefinition(
                            new TemplateTagDefinition[0],
                            new[]
                            {
                                new TemplateBlockDefinition(
                                    "first"
                                ),
                                new TemplateBlockDefinition(
                                    "first"
                                )
                            }
                        )
                );

            Assert.Equal(
                "blocks",
                exception.ParamName
            );
        }

        [Fact]
        public void LanguageDefinition_TagAndBlockMayShareName()
        {
            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "thing",
                            TemplateTagRole.Value
                        )
                    },
                    new[]
                    {
                        new TemplateBlockDefinition(
                            "thing"
                        )
                    }
                );

            var result =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{thing}}{{#thing}}x{{/thing}}"
                    ),
                    language
                );

            Assert.False(result.HasErrors);
            Assert.Empty(result.Diagnostics);

            bool valueFound = false;

            var syntax =
                result.SyntaxSpans;

            for (
                int index = 0;
                index < syntax.Length;
                index++
            )
            {
                if (
                    syntax[index].Kind
                    == TemplateSyntaxKind.ValueName
                )
                {
                    valueFound = true;
                }
            }

            Assert.True(valueFound);
        }

        [Fact]
        public void LanguageAndAnalysisCollections_AreDefensiveCopies()
        {
            var language =
                new TemplateLanguageDefinition(
                    new[]
                    {
                        new TemplateTagDefinition(
                            "name",
                            TemplateTagRole.Value
                        )
                    },
                    new[]
                    {
                        new TemplateBlockDefinition(
                            "first"
                        )
                    }
                );

            var tags = language.Tags;
            tags[0] =
                new TemplateTagDefinition(
                    "mutated",
                    TemplateTagRole.Command
                );

            var blocks = language.Blocks;
            blocks[0] =
                new TemplateBlockDefinition(
                    "mutated"
                );

            Assert.Equal(
                "name",
                language.Tags[0].Name
            );
            Assert.Equal(
                "first",
                language.Blocks[0].Name
            );

            var analysis =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse(
                        "{{missing}}"
                    ),
                    language
                );

            var diagnostics = analysis.Diagnostics;

            var replacementAnalysis =
                TemplateLanguageAnalyzer.Analyze(
                    TemplateParser.Parse("{{other}}"),
                    language
                );

            diagnostics[0] = replacementAnalysis.Diagnostics[0];

            var syntax = analysis.SyntaxSpans;
            syntax[0] = TemplateParser.Parse("literal").SyntaxSpans[0];

            Assert.Equal(
                TemplateLanguageAnalyzer.UnknownTagDiagnosticCode,
                analysis.Diagnostics[0].Code
            );
            Assert.Equal(
                TemplateSyntaxKind.Delimiter,
                analysis.SyntaxSpans[0].Kind
            );
        }
    }
}