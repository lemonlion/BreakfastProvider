namespace BreakfastProvider.Tests.Component.Shared.Models.Muffins;

public class TestMuffinRequest
{
    public string? Milk { get; set; }
    public string? Flour { get; set; }
    public string? Eggs { get; set; }
    public string? Apples { get; set; }
    public string? Cinnamon { get; set; }
    public TestBakingProfile? Baking { get; set; }
    public List<TestMuffinTopping> Toppings { get; set; } = [];
}

public class TestBakingProfile
{
    public int Temperature { get; set; }
    public int DurationMinutes { get; set; }
    public string? PanType { get; set; }
}

public class TestMuffinTopping
{
    public string? Name { get; set; }
    public string? Amount { get; set; }
}
