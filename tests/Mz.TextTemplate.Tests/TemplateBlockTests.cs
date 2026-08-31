using Xunit;

namespace Mz.TextTemplate.Tests
{
    public sealed class TemplateBlockTests
    {
        [Fact]
        public void Parse_SimpleBlock_ProducesNestedNode()
        {
            const string source =
                "{{#first}}hello{{/first}}";

            var result =
                TemplateParser.Parse(source);

            Assert.False(result.HasErrors);

            var block =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal("first", block.Name);
            Assert.True(block.HasClosingTag);
            Assert.Equal(
                new SourceSpan(0, source.Length),
                block.Span
            );
            Assert.Equal(
                new SourceSpan(0, 10),
                block.OpenTagSpan
            );
            Assert.Equal(
                new SourceSpan(3, 5),
                block.OpenNameSpan
            );
            Assert.Equal(
                new SourceSpan(15, 10),
                block.CloseTagSpan
            );
            Assert.Equal(
                new SourceSpan(18, 5),
                block.CloseNameSpan
            );

            var child =
                Assert.IsType<TemplateTextNode>(
                    Assert.Single(block.Children)
                );

            Assert.Equal("hello", child.Text);
            Assert.Equal(
                new SourceSpan(10, 5),
                child.Span
            );
        }

        [Fact]
        public void Parse_NestedBlocks_PreserveHierarchy()
        {
            var result =
                TemplateParser.Parse(
                    "{{#outer}}A{{#inner}}B{{/inner}}C{{/outer}}"
                );

            Assert.False(result.HasErrors);

            var outer =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal("outer", outer.Name);

            var outerChildren =
                outer.Children;

            Assert.Equal(3, outerChildren.Length);
            Assert.Equal(
                "A",
                Assert.IsType<TemplateTextNode>(
                    outerChildren[0]
                ).Text
            );

            var inner =
                Assert.IsType<TemplateBlockNode>(
                    outerChildren[1]
                );

            Assert.Equal("inner", inner.Name);
            Assert.Equal(
                "B",
                Assert.IsType<TemplateTextNode>(
                    Assert.Single(inner.Children)
                ).Text
            );

            Assert.Equal(
                "C",
                Assert.IsType<TemplateTextNode>(
                    outerChildren[2]
                ).Text
            );
        }

        [Fact]
        public void Parse_BlockOpeningTag_ReusesArgumentGrammar()
        {
            var result =
                TemplateParser.Parse(
                    "{{#group limit=2 enabled=true}}x{{/group}}"
                );

            Assert.False(result.HasErrors);

            var block =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var arguments =
                block.Arguments;

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

            var enabled =
                Assert.IsType<TemplateNamedArgument>(
                    arguments[1]
                );

            Assert.Equal("enabled", enabled.Name);
            Assert.Equal(
                TemplateArgumentValueKind.Boolean,
                enabled.Value.Kind
            );
        }

        [Fact]
        public void Parse_UnexpectedClosingBlock_ReportsRecoverableError()
        {
            var result =
                TemplateParser.Parse(
                    "a{{/first}}b"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.UnexpectedClosingBlockDiagnosticCode,
                diagnostic.Code
            );

            var nodes =
                result.Document.Nodes;

            Assert.Equal(2, nodes.Length);
            Assert.Equal(
                "a",
                Assert.IsType<TemplateTextNode>(
                    nodes[0]
                ).Text
            );
            Assert.Equal(
                "b",
                Assert.IsType<TemplateTextNode>(
                    nodes[1]
                ).Text
            );
        }

        [Fact]
        public void Parse_MismatchedClosingBlock_LeavesOpenBlockRecoverable()
        {
            var result =
                TemplateParser.Parse(
                    "{{#first}}x{{/last}}y{{/first}}"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.MismatchedClosingBlockDiagnosticCode,
                diagnostic.Code
            );
            Assert.Equal(
                "last",
                result.Source.Substring(
                    diagnostic.Span.Start,
                    diagnostic.Span.Length
                )
            );

            var block =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.True(block.HasClosingTag);
            Assert.Equal("first", block.Name);

            var children =
                block.Children;

            Assert.Equal(2, children.Length);
            Assert.Equal(
                "x",
                Assert.IsType<TemplateTextNode>(
                    children[0]
                ).Text
            );
            Assert.Equal(
                "y",
                Assert.IsType<TemplateTextNode>(
                    children[1]
                ).Text
            );
        }

        [Fact]
        public void Parse_UnclosedBlock_ProducesRecoveredBlockNode()
        {
            const string source =
                "{{#first}}hello";

            var result =
                TemplateParser.Parse(source);

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.UnclosedBlockDiagnosticCode,
                diagnostic.Code
            );

            var block =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.False(block.HasClosingTag);
            Assert.Equal(
                new SourceSpan(0, source.Length),
                block.Span
            );
            Assert.Equal(
                new SourceSpan(source.Length, 0),
                block.CloseTagSpan
            );
            Assert.Equal(
                new SourceSpan(source.Length, 0),
                block.CloseNameSpan
            );
        }

        [Fact]
        public void Parse_NestedUnclosedBlocks_DiagnosticsRemainInSourceOrder()
        {
            var result =
                TemplateParser.Parse(
                    "{{#outer}}{{#inner}}x"
                );

            Assert.True(result.HasErrors);

            var diagnostics =
                result.Diagnostics;

            Assert.Equal(2, diagnostics.Length);
            Assert.Equal(
                TemplateParser.UnclosedBlockDiagnosticCode,
                diagnostics[0].Code
            );
            Assert.Equal(
                TemplateParser.UnclosedBlockDiagnosticCode,
                diagnostics[1].Code
            );
            Assert.True(
                diagnostics[0].Span.Start
                < diagnostics[1].Span.Start
            );

            var outer =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var inner =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(outer.Children)
                );

            Assert.Equal("outer", outer.Name);
            Assert.Equal("inner", inner.Name);
        }

        [Fact]
        public void Parse_BlockSyntaxSpans_AreExactAndSourceOrdered()
        {
            var result =
                TemplateParser.Parse(
                    "{{#first}}x{{/first}}"
                );

            Assert.False(result.HasErrors);

            var syntax =
                result.SyntaxSpans;

            Assert.Equal(9, syntax.Length);

            Assert.Equal(
                TemplateSyntaxKind.Delimiter,
                syntax[0].Kind
            );
            Assert.Equal(
                new SourceSpan(0, 2),
                syntax[0].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.BlockMarker,
                syntax[1].Kind
            );
            Assert.Equal(
                new SourceSpan(2, 1),
                syntax[1].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.BlockName,
                syntax[2].Kind
            );
            Assert.Equal(
                new SourceSpan(3, 5),
                syntax[2].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.Delimiter,
                syntax[3].Kind
            );
            Assert.Equal(
                new SourceSpan(8, 2),
                syntax[3].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.LiteralText,
                syntax[4].Kind
            );
            Assert.Equal(
                new SourceSpan(10, 1),
                syntax[4].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.Delimiter,
                syntax[5].Kind
            );
            Assert.Equal(
                new SourceSpan(11, 2),
                syntax[5].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.BlockMarker,
                syntax[6].Kind
            );
            Assert.Equal(
                new SourceSpan(13, 1),
                syntax[6].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.BlockName,
                syntax[7].Kind
            );
            Assert.Equal(
                new SourceSpan(14, 5),
                syntax[7].Span
            );

            Assert.Equal(
                TemplateSyntaxKind.Delimiter,
                syntax[8].Kind
            );
            Assert.Equal(
                new SourceSpan(19, 2),
                syntax[8].Span
            );
        }

        [Fact]
        public void Parse_ClosingAncestor_RecoversUnclosedInnerBlock()
        {
            var result =
                TemplateParser.Parse(
                    "{{#outer}}{{#inner}}x{{/outer}}"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.UnclosedBlockDiagnosticCode,
                diagnostic.Code
            );

            var outer =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal("outer", outer.Name);
            Assert.True(outer.HasClosingTag);

            var inner =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(outer.Children)
                );

            Assert.Equal("inner", inner.Name);
            Assert.False(inner.HasClosingTag);

            Assert.Equal(
                new SourceSpan(21, 0),
                inner.CloseTagSpan
            );

            Assert.Equal(
                "x",
                Assert.IsType<TemplateTextNode>(
                    Assert.Single(inner.Children)
                ).Text
            );
        }

        [Fact]
        public void Parse_InvalidBlockName_StillPairsStructurally()
        {
            var result =
                TemplateParser.Parse(
                    "{{#1bad}}x{{/1bad}}"
                );

            Assert.True(result.HasErrors);

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

            var block =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal("1bad", block.Name);
            Assert.True(block.HasClosingTag);

            Assert.Equal(
                "x",
                Assert.IsType<TemplateTextNode>(
                    Assert.Single(block.Children)
                ).Text
            );
        }

        [Fact]
        public void Parse_EmptyOpeningBlockName_DoesNotCreateBlockFrame()
        {
            var result =
                TemplateParser.Parse(
                    "{{#}}x"
                );

            Assert.True(result.HasErrors);

            var diagnostic =
                Assert.Single(result.Diagnostics);

            Assert.Equal(
                TemplateParser.EmptyBlockNameDiagnosticCode,
                diagnostic.Code
            );

            var text =
                Assert.IsType<TemplateTextNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal("x", text.Text);
            Assert.Equal(
                new SourceSpan(5, 1),
                text.Span
            );
        }
        [Fact]
        public void BlockCollections_AreDefensiveCopies()
        {
            var result =
                TemplateParser.Parse(
                    "{{#group limit=2}}x{{/group}}"
                );

            var block =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(result.Document.Nodes)
                );

            var children =
                block.Children;

            children[0] =
                new TemplateTextNode(
                    "mutated",
                    new SourceSpan(0, 0)
                );

            var arguments =
                block.Arguments;

            arguments[0] =
                new TemplatePositionalArgument(
                    new TemplateArgumentValue(
                        TemplateArgumentValueKind.Bare,
                        "x",
                        "x",
                        new SourceSpan(0, 1),
                        new SourceSpan(0, 1)
                    )
                );

            Assert.Equal(
                "x",
                Assert.IsType<TemplateTextNode>(
                    block.Children[0]
                ).Text
            );

            Assert.IsType<TemplateNamedArgument>(
                block.Arguments[0]
            );
        }

        [Fact]
        public void Parse_WhitespaceAroundClosingBlockName_IsAccepted()
        {
            var result =
                TemplateParser.Parse(
                    "{{#first}}x{{/ first }}"
                );

            Assert.False(result.HasErrors);

            var block =
                Assert.IsType<TemplateBlockNode>(
                    Assert.Single(result.Document.Nodes)
                );

            Assert.Equal("first", block.Name);
            Assert.True(block.HasClosingTag);
        }
    }
}