using System;
using System.Collections.Generic;
using System.Linq;
using Mz.Collections;
using Xunit;

namespace Mz.Collections.Tests;

public sealed class ReadOnlyListViewTests
{
    [Fact]
    public void ConstructorRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ReadOnlyListView<string>(null!));
    }

    [Fact]
    public void ViewForwardsCountIndexerAndEnumeration()
    {
        List<string> items = new() { "Alpha", "Beta" };
        ReadOnlyListView<string> view = new(items);

        Assert.Equal(2, view.Count);
        Assert.Equal("Beta", view[1]);
        Assert.Equal(new[] { "Alpha", "Beta" }, view.ToArray());
    }

    [Fact]
    public void ViewReflectsBackingListChanges()
    {
        List<string> items = new() { "Alpha" };
        ReadOnlyListView<string> view = new(items);

        items.Add("Beta");
        items[0] = "Updated";

        Assert.Equal(new[] { "Updated", "Beta" }, view.ToArray());
    }
}