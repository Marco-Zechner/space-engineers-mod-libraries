using System;
using System.Collections.Generic;
using System.Linq;
using Mz.Collections;
using Xunit;

namespace Mz.Collections.Tests;

public sealed class ReadOnlyDictionaryViewTests
{
    [Fact]
    public void ConstructorRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ReadOnlyDictionaryView<string, int>(null!));
    }

    [Fact]
    public void ViewForwardsDictionaryOperations()
    {
        Dictionary<string, int> items = new() {
            ["Alpha"] = 1,
            ["Beta"] = 2
        };
        ReadOnlyDictionaryView<string, int> view = new(items);

        Assert.Equal(2, view.Count);
        Assert.True(view.ContainsKey("Alpha"));
        Assert.Equal(2, view["Beta"]);
        Assert.Equal(new[] { "Alpha", "Beta" }, view.Keys.OrderBy(value => value).ToArray());
        Assert.Equal(new[] { 1, 2 }, view.Values.OrderBy(value => value).ToArray());

        Assert.True(view.TryGetValue("Alpha", out int value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void ViewReflectsBackingDictionaryChanges()
    {
        Dictionary<string, int> items = new() { ["Alpha"] = 1 };
        ReadOnlyDictionaryView<string, int> view = new(items);

        items["Alpha"] = 4;
        items["Beta"] = 2;

        Assert.Equal(2, view.Count);
        Assert.Equal(4, view["Alpha"]);
        Assert.Equal(2, view["Beta"]);
    }
}