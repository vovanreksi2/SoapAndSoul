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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed: {message}", ex.Message);

            try
            {
                var result = await rollbackTask();
                if (!result)
                    logger.LogError(ex, "Rollback transaction is failed: {message}", ex.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Rollback transaction is failed: {message}", e.Message);
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
