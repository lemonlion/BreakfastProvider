namespace BreakfastProvider.Tests.Component.Shared.Constants;

public static class ToppingDefaults
{
    public const string Raspberries = "Raspberries";
    public const string Blueberries = "Blueberries";
    public const string MapleSyrup = "Maple Syrup";
    public const string WhippedCream = "Whipped Cream";
    public const string ChocolateChips = "Chocolate Chips";
    public const string Strawberries = "Strawberries";
    public const string FruitCategory = "Fruit";
    public const string ExtraTopping = "Extra_Topping";

    public static readonly Guid KnownRaspberryToppingId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid KnownBlueberryToppingId = Guid.Parse("11111111-0000-0000-0000-000000000002");

    public const int ExpectedToppingCount = 5;
}
