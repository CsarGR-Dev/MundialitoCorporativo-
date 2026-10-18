using System.Data.SqlTypes;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.UnitTests.Domain.Common;

public class SequentialGuidTests
{
    [Fact]
    public void NewGuid_ShouldBeUnique()
    {
        var guids = Enumerable.Range(0, 1_000).Select(_ => SequentialGuid.NewGuid()).ToList();

        Assert.Equal(guids.Count, guids.Distinct().Count());
    }

    [Fact]
    public void NewGuid_ShouldBeAscendingUsingSqlServerOrdering()
    {
        var guids = Enumerable.Range(0, 100).Select(_ => new SqlGuid(SequentialGuid.NewGuid())).ToList();

        for (var i = 1; i < guids.Count; i++)
        {
            Assert.True((guids[i] > guids[i - 1]).IsTrue);
        }
    }
}
