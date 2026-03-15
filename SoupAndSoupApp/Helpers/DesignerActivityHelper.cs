using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SoupAndSoupApp.Helpers.Notifications;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers;

/// <summary>
/// Compensation transaction execution and result-to-notification conversion.
/// </summary>
public static class DesignerActivityHelper
{
    public static async Task<bool> ExecuteCompensatingTransaction(
        Func<Task<bool>> executeTask,
        Func<Task<bool>> rollbackTask,
        ILogger logger)
    {
        try
        {
            return await executeTask();
        }
        catch (Exception originalEx)
        {
            logger.LogError(originalEx, "Operation failed: {message}", originalEx.Message);

            try
            {
                var result = await rollbackTask();
                if (!result)
                    logger.LogError("Rollback did not confirm success after original failure: {message}", originalEx.Message);
            }
            catch (Exception rollbackEx)
            {
                logger.LogError(rollbackEx, "Rollback also failed after original error: {originalMessage}", originalEx.Message);
            }

            return false;
        }
    }

    public static void NotifyResult(
        bool success,
        DomainNotificationType successNotification,
        INotificationService notificationService)
    {
        var type = success
            ? successNotification
            : DomainNotificationType.ErrorWhileSaving;

        notificationService.Notify(type);
    }
}
