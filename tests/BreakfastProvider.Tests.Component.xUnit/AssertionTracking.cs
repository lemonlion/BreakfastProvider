using TestTrackingDiagrams.Tracking;

[assembly: TrackAssertions]

namespace TestTrackingDiagrams.Tracking;

[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class TrackAssertionsAttribute : Attribute;
