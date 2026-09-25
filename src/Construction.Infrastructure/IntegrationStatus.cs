using Construction.Application.Common.Interfaces;
using Construction.Infrastructure.Email;
using Construction.Infrastructure.Notifications;
using Microsoft.Extensions.Options;

namespace Construction.Infrastructure;

public class IntegrationStatus : IIntegrationStatus
{
    private readonly IOptions<EmailSettings> _email;
    private readonly IOptions<FirebaseSettings> _firebase;

    public IntegrationStatus(IOptions<EmailSettings> email, IOptions<FirebaseSettings> firebase)
    {
        _email = email;
        _firebase = firebase;
    }

    public bool EmailConfigured => _email.Value.IsConfigured;

    public bool PushConfigured => _firebase.Value.IsConfigured;
}
