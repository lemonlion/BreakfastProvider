namespace BreakfastProvider.Tests.Component.LightBDD.LightBddCustomisations;

public interface IIgnorable<out T> where T : class
{
    T IgnoreSettings { get; }
}
