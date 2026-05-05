using TUnit.Core;
using TUnit.Core.Interfaces;

namespace BreakfastProvider.Tests.Component.TUnit.Infrastructure;

public class SingleThreadParallelLimit : IParallelLimit
{
    public int Limit => 1;
}
