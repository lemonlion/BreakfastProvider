using System.Data.Common;

namespace BreakfastProvider.Api.Data.ClickHouse;

public static class ClickHouseCommandExtensions
{
    /// <summary>
    /// Adds a named parameter matching a <c>{name:Type}</c> placeholder in the command text.
    /// Uses only <see cref="DbCommand.CreateParameter"/> so the command may be a tracking decorator.
    /// </summary>
    public static DbCommand AddParameter(this DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
        return command;
    }
}
