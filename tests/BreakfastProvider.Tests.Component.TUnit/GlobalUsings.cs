global using TUnit.Assertions.Should;
global using TUnit.Assertions.Should.Extensions;
global using TestTrackingDiagrams.Tracking;
global using TUnit.Core;
global using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
global using BreakfastProvider.Tests.Component.Shared.Infrastructure.Configuration;
global using Settings = BreakfastProvider.Tests.Component.Shared.Infrastructure.Configuration.ComponentTestSettings;
global using BreakfastProvider.Tests.Component.Shared.Infrastructure.DependencyInjection;
global using BreakfastProvider.Tests.Component.Shared.Infrastructure.Hosting;
global using static BreakfastProvider.Tests.Component.Shared.Constants.IgnoreReasons;

[assembly: ParallelLimiter<BreakfastProvider.Tests.Component.TUnit.Infrastructure.SingleThreadParallelLimit>]
