using System;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers.Notifications;

public interface INotificationService
{
    void Notify(DomainNotificationType notificationType);
    IObservable<DomainNotificationType> Notifications { get; }
}