using Xunit;

namespace Mz.TextTemplate.Tests
{
    public sealed class TemplateArgumentTests
    {
        [Fact]
        public void Parse_TabTag_ProducesPositionalAndNamedArguments()
        {
            const string source =
                "{{tab 4 wrap=1}}";

            var result =
                TemplateParser.Parse(source);

            Assert.False(result.HasErrors);

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal("tab", tag.Name);

            var arguments = tag.Arguments;
            Assert.Equal(2, arguments.Length);

            var positional =
                Assert.IsType<TemplatePositionalArgument>(
                    arguments[0]
                );

            Assert.Equal(
                TemplateArgumentValueKind.Number,
                positional.Value.Kind
            );
            Assert.Equal("4", positional.Value.RawText);
            Assert.Equal("4", positional.Value.Text);
            Assert.Equal(
                new SourceSpan(6, 1),
                positional.Value.Span
            );

            var named =
                Assert.IsType<TemplateNamedArgument>(
                    arguments[1]
                );

            Assert.Equal("wrap", named.Name);
            Assert.Equal(
                new SourceSpan(8, 4),
                named.NameSpan
            );
            Assert.Equal(
                new SourceSpan(12, 1),
                named.EqualsSpan
            );
            Assert.Equal(
                TemplateArgumentValueKind.Number,
                named.Value.Kind
            );
            Assert.Equal("1", named.Value.Text);
            Assert.Equal(
                new SourceSpan(8, 6),
                named.Span
            );
        }

        [Fact]
        public void Parse_NamedArguments_ClassifyNumberAndBoolean()
        {
            var result =
                TemplateParser.Parse(
                    "{{name limit=12 ellipsis=true}}"
                );

            Assert.False(result.HasErrors);

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var arguments = tag.Arguments;
            Assert.Equal(2, arguments.Length);

            var limit =
                Assert.IsType<TemplateNamedArgument>(
                    arguments[0]
                );

            Assert.Equal("limit", limit.Name);
            Assert.Equal(
                TemplateArgumentValueKind.Number,
                limit.Value.Kind
            );
            Assert.Equal("12", limit.Value.Text);

            var ellipsis =
                Assert.IsType<TemplateNamedArgument>(
                    arguments[1]
                );

            Assert.Equal("ellipsis", ellipsis.Name);
            Assert.Equal(
                TemplateArgumentValueKind.Boolean,
                ellipsis.Value.Kind
            );
            Assert.Equal("true", ellipsis.Value.Text);
        }

        [Fact]
        public void Parse_QuotedNamedValue_PreservesRawAndContentSpans()
        {
            const string source =
                "{{time format=\"HH:mm:ss\"}}";

            var result =
                TemplateParser.Parse(source);

            Assert.False(result.HasErrors);

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var argument =
                Assert.IsType<TemplateNamedArgument>(
                    Assert.Single(tag.Arguments)
                );

            Assert.Equal("format", argument.Name);
            Assert.Equal(
                TemplateArgumentValueKind.String,
                argument.Value.Kind
            );
            Assert.Equal(
                "\"HH:mm:ss\"",
                argument.Value.RawText
            );
            Assert.Equal(
                "HH:mm:ss",
                argument.Value.Text
            );
            Assert.Equal(
                new SourceSpan(14, 10),
                argument.Value.Span
            );
            Assert.Equal(
                new SourceSpan(15, 8),
                argument.Value.ContentSpan
            );
        }

        [Fact]
        public void Parse_QuotedPositionalValue_CanContainWhitespace()
        {
            var result =
                TemplateParser.Parse(
                    "{{tag \"hello world\"}}"
                );

            Assert.False(result.HasErrors);

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var argument =
                Assert.IsType<TemplatePositionalArgument>(
                    Assert.Single(tag.Arguments)
                );

            Assert.Equal(
                TemplateArgumentValueKind.String,
                argument.Value.Kind
            );
            Assert.Equal(
                "hello world",
                argument.Value.Text
            );
        }

        [Fact]
        public void Parse_QuotedValue_CanContainClosingDelimiterText()
        {
            var result =
                TemplateParser.Parse(
                    "{{tag value=\"a}}b\"}}"
                );

            Assert.False(result.HasErrors);

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var argument =
                Assert.IsType<TemplateNamedArgument>(
                    Assert.Single(tag.Arguments)
                );

            Assert.Equal(
                "a}}b",
                argument.Value.Text
            );
        }

        [Fact]
        public void Parse_NamedArgument_AllowsWhitespaceAroundEquals()
        {
            var result =
                TemplateParser.Parse(
                    "{{tag limit = 12}}"
                );

            Assert.False(result.HasErrors);

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var argument =
                Assert.IsType<TemplateNamedArgument>(
                    Assert.Single(tag.Arguments)
                );

            Assert.Equal("limit", argument.Name);
            Assert.Equal("12", argument.Value.Text);
        }

        [Fact]
        public void Parse_MissingNamedValue_ReportsRecoverableDiagnostic()
        {
            var result =
                TemplateParser.Parse(
                    "{{tag limit=}}"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.MissingArgumentValueDiagnosticCode,
                diagnostic.Code
            );

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var argument =
                Assert.IsType<TemplateNamedArgument>(
                    Assert.Single(tag.Arguments)
                );

            Assert.Equal(
                TemplateArgumentValueKind.Missing,
                argument.Value.Kind
            );
            Assert.Equal(string.Empty, argument.Value.Text);
        }

        [Fact]
        public void Parse_MissingNamedArgumentName_ReportsDiagnostic()
        {
            var result =
                TemplateParser.Parse(
                    "{{tag =12}}"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.MissingArgumentNameDiagnosticCode,
                diagnostic.Code
            );

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var argument =
                Assert.IsType<TemplateNamedArgument>(
                    Assert.Single(tag.Arguments)
                );

            Assert.Equal(string.Empty, argument.Name);
            Assert.Equal("12", argument.Value.Text);
        }

        [Fact]
        public void Parse_UnterminatedQuotedValue_ReportsDiagnostic()
        {
            var result =
                TemplateParser.Parse(
                    "{{time format=\"HH:mm:ss}}"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.UnterminatedStringDiagnosticCode,
                diagnostic.Code
            );

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var argument =
                Assert.IsType<TemplateNamedArgument>(
                    Assert.Single(tag.Arguments)
                );

            Assert.Equal(
                TemplateArgumentValueKind.String,
                argument.Value.Kind
            );
            Assert.Equal(
                "HH:mm:ss",
                argument.Value.Text
            );
        }

        [Theory]
        [InlineData("1limit", 6)]
        [InlineData("bad/name", 9)]
        [InlineData("bad.name", 9)]
        public void Parse_InvalidNamedArgumentName_ReportsOffendingCharacter(
            string name,
            int expectedOffset
        )
        {
            var result =
                TemplateParser.Parse(
                    "{{tag " + name + "=1}}"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.InvalidArgumentNameDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                new SourceSpan(
                    expectedOffset,
                    1
                ),
                diagnostic.Span
            );
        }

        [Fact]
        public void Parse_TabTag_ProducesExactSyntaxSpans()
        {
            var result =
                TemplateParser.Parse(
                    "{{tab 4 wrap=1}}"
                );

            Assert.False(result.HasErrors);

            var syntax = result.SyntaxSpans;
            Assert.Equal(7, syntax.Length);

            Assert.Equal(
                TemplateSyntaxKind.Delimiter,
                syntax[0].Kind
            );
            Assert.Equal(
                new SourceSpan(0, 2),
                syntax[0].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.TagName,
                syntax[1].Kind
            );
            Assert.Equal(
                new SourceSpan(2, 3),
                syntax[1].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.NumberValue,
                syntax[2].Kind
            );
            Assert.Equal(
                new SourceSpan(6, 1),
                syntax[2].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.ArgumentName,
                syntax[3].Kind
            );
            Assert.Equal(
                new SourceSpan(8, 4),
                syntax[3].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.AssignmentOperator,
                syntax[4].Kind
            );
            Assert.Equal(
                new SourceSpan(12, 1),
                syntax[4].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.NumberValue,
                syntax[5].Kind
            );
            Assert.Equal(
                new SourceSpan(13, 1),
                syntax[5].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.Delimiter,
                syntax[6].Kind
            );
            Assert.Equal(
                new SourceSpan(14, 2),
                syntax[6].Span
            );
        }
        [Fact]
        public void TagArguments_AreDefensiveCopies()
        {
            var result =
                TemplateParser.Parse(
                    "{{tab 4 wrap=1}}"
                );

            var tag =
                Assert.IsType<TemplateTagNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var arguments = tag.Arguments;
            arguments[0] = arguments[1];

            Assert.IsType<TemplatePositionalArgument>(
                tag.Arguments[0]
            );
        }

        [Fact]
        public void Parse_ArgumentSyntaxSpans_AreInSourceOrder()
        {
            var result =
                TemplateParser.Parse(
                    "{{tab 4 wrap=1 label=\"x\" enabled=true}}"
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

            Assert.Contains(
                syntax,
                item =>
                    item.Kind
                    == TemplateSyntaxKind.NumberValue
            );
            Assert.Contains(
                syntax,
                item =>
                    item.Kind
                    == TemplateSyntaxKind.ArgumentName
            );
            Assert.Contains(
                syntax,
                item =>
                    item.Kind
                    == TemplateSyntaxKind.AssignmentOperator
            );
            Assert.Contains(
                syntax,
                item =>
                    item.Kind
                    == TemplateSyntaxKind.StringValue
            );
            Assert.Contains(
                syntax,
                item =>
                    item.Kind
                    == TemplateSyntaxKind.BooleanValue
            );
        }

        [Fact]
        public void Parse_MultilineLiteral_PreservesLeadingTrailingAndInteriorNewlines()
        {
            const string source =
                "\nfirst\n{{name}}\nlast\n";

            var result =
                TemplateParser.Parse(source);

            Assert.False(result.HasErrors);
            Assert.Equal(source, result.Source);

            var nodes = result.Document.Nodes;
            Assert.Equal(3, nodes.Length);

            Assert.Equal(
                "\nfirst\n",
                Assert.IsType<TemplateTextNode>(
                    nodes[0]
                ).Text
            );
            Assert.Equal(
                "\nlast\n",
                Assert.IsType<TemplateTextNode>(
                    nodes[2]
                ).Text
            );

            Assert.Equal(
                new SourceSpan(0, 7),
                nodes[0].Span
            );
            Assert.Equal(
                new SourceSpan(15, 6),
                nodes[2].Span
            );
        }
    }
}