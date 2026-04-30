using System.Linq.Expressions;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;

namespace BreakfastProvider.Tests.Component.LightBDD.Util;

#pragma warning disable CS1998
public static class Sub
{
    public static CompositeStep Steps(params Expression<Func<NoContext, Task>>[] steps)
    {
        return CompositeStep.DefineNew().AddAsyncSteps(steps).Build();
    }

    public static CompositeStep Steps(params Func<Task>[] steps)
    {
        return CompositeStep.DefineNew().AddAsyncSteps(steps).Build();
    }

    public static ICompositeStepBuilder<NoContext> Steps()
    {
        return CompositeStep.DefineNew();
    }
}
