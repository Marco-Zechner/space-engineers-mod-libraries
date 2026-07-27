using System;
using System.Linq;
using Xunit;

namespace SpaceEngineersModLibraries.ReleaseTests;

public static class ReleaseTestData
{
    public static TheoryData<string> LibraryIds
    {
        get
        {
            var data =
                new TheoryData<string>();

            foreach (
                string packageId in
                ReleaseRepository
                    .Load()
                    .Libraries
                    .Keys
                    .OrderBy(
                        value => value,
                        StringComparer.Ordinal
                    )
            )
            {
                data.Add(packageId);
            }

            return data;
        }
    }
}
