using System.Diagnostics;

namespace SoapAndSoul.Infrastructure;

/// <summary>
/// It is recommended to use a custom type to hold references for ActivitySource.
/// This avoids possible type collisions with other components in the DI container.
/// </summary>
public class InstrumentationOpenTelemetry : IDisposable
{
    internal const string ActivitySourceName = nameof(SoapAndSoul);
    internal const string ActivitySourceVersion = "1.0.0";

    public InstrumentationOpenTelemetry()
    {
        this.ActivitySource = new ActivitySource(ActivitySourceName, ActivitySourceVersion);
    }

    public ActivitySource ActivitySource { get; }

    public void Dispose()
    {
        this.ActivitySource.Dispose();
    }
}