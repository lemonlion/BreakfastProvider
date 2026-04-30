using System.Linq.Expressions;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;

namespace BreakfastProvider.Tests.Component.LightBDD.Util;

#pragma warning disable CS1998
public static class StartWith
{
    public static ICompositeStepBuilder<NoContext> SubSteps(params Expression<Func<NoContext, Task>>[] steps)
    {
        return CompositeStep.DefineNew().AddAsyncSteps(steps);
    }

    public static ICompositeStepBuilder<NoContext> SubSteps(params Func<Task>[] steps)
    {
        return CompositeStep.DefineNew().AddAsyncSteps(steps);
    }
}
