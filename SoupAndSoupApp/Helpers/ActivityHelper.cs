using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SoupAndSoupApp.Helpers;

/// <summary>
/// OpenTelemetry activity helpers: wrapping operations in spans and recording exceptions.
/// </summary>
public static class ActivityHelper
{
    public static async Task RunWithActivity(
        ActivitySource activitySource,
        string activityName,
        Func<Task> action,
        ILogger logger,
        params (string Key, object Value)[] tags)
    {
        using var activity = activitySource.StartActivity(activityName);

        try
        {
            await action();

            foreach (var (key, value) in tags)
                activity?.SetTag(key, value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {ActivityName}", activityName);
            activity?.SetTag("error", true);
            activity?.AddEvent(BuildExceptionEvent(ex));
            throw;
        }
    }

    public static void RecordInitException(Exception e, Activity? activity, ILogger logger)
    {
        logger.LogError(e, "Exception during initialization");
        if (activity is null) return;

        activity.SetStatus(ActivityStatusCode.Error);
        activity.SetTag("error", true);
        activity.AddEvent(BuildExceptionEvent(e));
    }

    private static ActivityEvent BuildExceptionEvent(Exception ex) =>
        new("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", ex.GetType().FullName },
            { "exception.message", ex.Message },
            { "exception.stacktrace", ex.StackTrace }
        });
}
