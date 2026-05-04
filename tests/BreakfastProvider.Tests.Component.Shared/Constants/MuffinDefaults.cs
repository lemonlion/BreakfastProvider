namespace BreakfastProvider.Tests.Component.Shared.Constants;

public static class MuffinDefaults
{
    public const string GrannySmithApples = "Granny_Smith_Apples";
    public const string CeylonCinnamon = "Ceylon_Cinnamon";
    public const string DefaultPanType = "Standard";
    public const int DefaultTemperature = 180;
    public const int DefaultDuration = 25;

    public static class ValidationMessages
    {
        public const string MilkRequired = "'Milk' is required.";
        public const string FlourRequired = "'Flour' is required.";
        public const string EggsRequired = "'Eggs' is required.";
        public const string ApplesRequired = "'Apples' is required.";
        public const string CinnamonRequired = "'Cinnamon' is required.";
        public const string BakingRequired = "'Baking' profile is required.";
        public const string TemperatureRange = "Baking temperature must be between 150 and 220 degrees.";
        public const string DurationRange = "Baking duration must be between 10 and 60 minutes.";
        public const string PanTypeRequired = "'Pan Type' is required.";
        public const string MaxToppingsExceeded = "Maximum toppings exceeded.";
    }
}
