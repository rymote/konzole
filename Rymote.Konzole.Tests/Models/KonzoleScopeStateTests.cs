using Rymote.Konzole.Models;
using Xunit;

namespace Rymote.Konzole.Tests.Models;

public class KonzoleScopeStateTests
{
    [Fact]
    public void Push_SetsCurrent_DisposeRestoresPrevious()
    {
        Assert.Null(KonzoleScopeState.Current);

        KonzoleScopeState outerState = new() { Tag = KonzoleTag.Start };
        using (KonzoleScopeState.Push(outerState))
        {
            Assert.Same(outerState, KonzoleScopeState.Current);

            KonzoleScopeState innerState = new() { Tag = KonzoleTag.Success };
            using (KonzoleScopeState.Push(innerState))
            {
                Assert.Same(innerState, KonzoleScopeState.Current);
            }

            Assert.Same(outerState, KonzoleScopeState.Current);
        }

        Assert.Null(KonzoleScopeState.Current);
    }

    [Fact]
    public async Task Current_SurvivesAwait()
    {
        KonzoleScopeState pushedState = new() { Tag = KonzoleTag.Watch };
        using (KonzoleScopeState.Push(pushedState))
        {
            await Task.Yield();
            Assert.Same(pushedState, KonzoleScopeState.Current);
        }
    }
}
