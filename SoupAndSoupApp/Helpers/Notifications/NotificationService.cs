using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers.Notifications;

public class NotificationService : INotificationService
{
    private readonly Subject<DomainNotificationType> _subject = new();

    public void Notify(DomainNotificationType notificationType)
    {
        _subject.OnNext(notificationType);
    }

    public IObservable<DomainNotificationType> Notifications => _subject.AsObservable();
}