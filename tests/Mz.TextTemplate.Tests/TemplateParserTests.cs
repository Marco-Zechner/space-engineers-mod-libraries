using System;
using Xunit;

namespace Mz.TextTemplate.Tests
{
    public sealed class TemplateParserTests
    {
        [Fact]
        public void Parse_LiteralText_PreservesExactSource()
        {
            var result = TemplateParser.Parse("hello world");

            Assert.False(result.HasErrors);
            Assert.Equal("hello world", result.Source);

            var nodes = result.Document.Nodes;
            Assert.Single(nodes);

            var text = Assert.IsType<TemplateTextNode>(nodes[0]);
            Assert.Equal("hello world", text.Text);
            Assert.Equal(new SourceSpan(0, 11), text.Span);

            var syntax = result.SyntaxSpans;
            Assert.Single(syntax);
            Assert.Equal(
                TemplateSyntaxKind.LiteralText,
                syntax[0].Kind
            );
            Assert.Equal(
                new SourceSpan(0, 11),
                syntax[0].Span
            );
        }

        [Fact]
        public void Parse_Tag_ProducesExactNodesAndSyntaxSpans()
        {
            const string source = "Hello {{ rank.server }}!";

            var result = TemplateParser.Parse(source);

            Assert.False(result.HasErrors);

            var nodes = result.Document.Nodes;
            Assert.Equal(3, nodes.Length);

            var prefix =
                Assert.IsType<TemplateTextNode>(nodes[0]);
            var tag =
                Assert.IsType<TemplateTagNode>(nodes[1]);
            var suffix =
                Assert.IsType<TemplateTextNode>(nodes[2]);

            Assert.Equal("Hello ", prefix.Text);
            Assert.Equal("rank.server", tag.Name);
            Assert.Equal(
                new SourceSpan(6, 17),
                tag.Span
            );
            Assert.Equal(
                new SourceSpan(9, 11),
                tag.NameSpan
            );
            Assert.Equal("!", suffix.Text);

            var syntax = result.SyntaxSpans;
            Assert.Equal(5, syntax.Length);

            Assert.Equal(
                TemplateSyntaxKind.LiteralText,
                syntax[0].Kind
            );
            Assert.Equal(
                new SourceSpan(0, 6),
                syntax[0].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.Delimiter,
                syntax[1].Kind
            );
            Assert.Equal(
                new SourceSpan(6, 2),
                syntax[1].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.TagName,
                syntax[2].Kind
            );
            Assert.Equal(
                new SourceSpan(9, 11),
                syntax[2].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.Delimiter,
                syntax[3].Kind
            );
            Assert.Equal(
                new SourceSpan(21, 2),
                syntax[3].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.LiteralText,
                syntax[4].Kind
            );
            Assert.Equal(
                new SourceSpan(23, 1),
                syntax[4].Span
            );
        }

        [Theory]
        [InlineData("name")]
        [InlineData("_name")]
        [InlineData("rank.server")]
        [InlineData("player-name")]
        [InlineData("source.workspace-2")]
        public void Parse_ValidTagName_HasNoDiagnostics(
            string name
        )
        {
            var result =
                TemplateParser.Parse(
                    "{{" + name + "}}"
                );

            Assert.False(result.HasErrors);
            Assert.Empty(result.Diagnostics);

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal(name, tag.Name);
        }

        [Fact]
        public void Parse_WhitespaceAroundName_IsIgnoredByTag()
        {
            var result =
                TemplateParser.Parse(
                    "{{  name  }}"
                );

            Assert.False(result.HasErrors);

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal("name", tag.Name);
            Assert.Equal(
                new SourceSpan(4, 4),
                tag.NameSpan
            );
        }

        [Fact]
        public void Parse_SyntaxSpans_AreReturnedInSourceOrder()
        {
            var result =
                TemplateParser.Parse(
                    "A {{name}} B {{rank.server}} C"
                );

            Assert.False(result.HasErrors);

            var syntax = result.SyntaxSpans;

            for (
                int index = 1;
                index < syntax.Length;
                index++
            )
            {
                Assert.True(
                    syntax[index - 1].Span.Start
                    <= syntax[index].Span.Start
                );
            }
        }

        [Fact]
        public void Parse_MultipleTags_PreservesStableOrder()
        {
            var result =
                TemplateParser.Parse(
                    "[{{time}}] {{name}}: {{message}}"
                );

            Assert.False(result.HasErrors);

            var nodes = result.Document.Nodes;
            Assert.Equal(6, nodes.Length);

            Assert.Equal(
                "time",
                Assert.IsType<TemplateTagNode>(
                    nodes[1]
                ).Name
            );
            Assert.Equal(
                "name",
                Assert.IsType<TemplateTagNode>(
                    nodes[3]
                ).Name
            );
            Assert.Equal(
                "message",
                Assert.IsType<TemplateTagNode>(
                    nodes[5]
                ).Name
            );
        }

        [Fact]
        public void Parse_EmptyTag_ReportsRecoverableError()
        {
            var result =
                TemplateParser.Parse(
                    "a {{   }} b"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.EmptyTagDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                TemplateDiagnosticSeverity.Error,
                diagnostic.Severity
            );
            Assert.Equal(
                new SourceSpan(2, 7),
                diagnostic.Span
            );

            var tag =
                Assert.IsType<TemplateTagNode>(
                    result.Document.Nodes[1]
                );

            Assert.Equal(string.Empty, tag.Name);
        }

        [Fact]
        public void Parse_WhitespaceAfterTagName_StartsPositionalArgument()
        {
            var result =
                TemplateParser.Parse(
                    "{{rank server}}"
                );

            Assert.False(result.HasErrors);

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal("rank", tag.Name);

            var argument =
                Assert.IsType<TemplatePositionalArgument>(
                    Assert.Single(tag.Arguments)
                );

            Assert.Equal(
                TemplateArgumentValueKind.Bare,
                argument.Value.Kind
            );
            Assert.Equal(
                "server",
                argument.Value.Text
            );
            Assert.Equal(
                new SourceSpan(7, 6),
                argument.Value.Span
            );
        }
        [Theory]
        [InlineData("1name", 2)]
        [InlineData("rank..server", 7)]
        [InlineData("rank.", 6)]

        [InlineData("rank/server", 6)]
        public void Parse_InvalidTagName_ReportsOffendingCharacter(
            string name,
            int expectedSourceOffset
        )
        {
            var result =
                TemplateParser.Parse(
                    "{{" + name + "}}"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.InvalidTagNameDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                new SourceSpan(
                    expectedSourceOffset,
                    1
                ),
                diagnostic.Span
            );
        }

        [Fact]
        public void Parse_UnterminatedTag_ReportsErrorAndPreservesTail()
        {
            var result =
                TemplateParser.Parse(
                    "before {{name"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.UnterminatedTagDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                new SourceSpan(7, 6),
                diagnostic.Span
            );

            var nodes = result.Document.Nodes;
            Assert.Equal(2, nodes.Length);

            Assert.Equal(
                "before ",
                Assert.IsType<TemplateTextNode>(
                    nodes[0]
                ).Text
            );
            Assert.Equal(
                "{{name",
                Assert.IsType<TemplateTextNode>(
                    nodes[1]
                ).Text
            );
        }

        [Fact]
        public void Parse_UnexpectedClosingDelimiter_ReportsErrorButKeepsLiteral()
        {
            var result =
                TemplateParser.Parse(
                    "before }} after"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.UnexpectedClosingDelimiterDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                new SourceSpan(7, 2),
                diagnostic.Span
            );

            var text =
                Assert.IsType<TemplateTextNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal(
                "before }} after",
                text.Text
            );
        }

        [Fact]
        public void Parse_MultilineLiteral_PreservesAllNewlinesExactly()
        {
            const string source =
                "\nalpha\n\nomega\n";

            var result =
                TemplateParser.Parse(source);

            Assert.False(result.HasErrors);
            Assert.Equal(source, result.Source);
            Assert.Empty(result.Diagnostics);

            var textNode =
                Assert.IsType<TemplateTextNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal(source, textNode.Text);
            Assert.Equal(
                new SourceSpan(0, source.Length),
                textNode.Span
            );

            var syntax =
                Assert.Single(result.SyntaxSpans);

            Assert.Equal(
                TemplateSyntaxKind.LiteralText,
                syntax.Kind
            );
            Assert.Equal(
                new SourceSpan(0, source.Length),
                syntax.Span
            );
        }
        [Fact]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            var exception =
                Assert.Throws<ArgumentNullException>(
                    () => TemplateParser.Parse(null!)
                );

            Assert.Equal(
                "source",
                exception.ParamName
            );
        }

        [Fact]
        public void ParserOwnedModel_ConstructorsAreNotPublic()
        {
            Assert.Empty(typeof(TemplateTextNode).GetConstructors());
            Assert.Empty(typeof(TemplateTagNode).GetConstructors());
            Assert.Empty(typeof(TemplateBlockNode).GetConstructors());
            Assert.Empty(typeof(TemplateArgumentValue).GetConstructors());
            Assert.Empty(typeof(TemplatePositionalArgument).GetConstructors());
            Assert.Empty(typeof(TemplateNamedArgument).GetConstructors());
        }

        [Fact]
        public void Result_CollectionsAreDefensiveCopies()
        {
            var result =
                TemplateParser.Parse(
                    "{{name}}"
                );

            var nodes = result.Document.Nodes;
            nodes[0] = TemplateParser.Parse("mutated").Document.Nodes[0];

            var syntax = result.SyntaxSpans;
            syntax[0] = TemplateParser.Parse("mutated").SyntaxSpans[0];

            Assert.IsType<TemplateTagNode>(
                Assert.Single(
                    result.Document.Nodes
                )
            );

            Assert.Equal(
                TemplateSyntaxKind.Delimiter,
                result.SyntaxSpans[0].Kind
            );
        }
    }
}