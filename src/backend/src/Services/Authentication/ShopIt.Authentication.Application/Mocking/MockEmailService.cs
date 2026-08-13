using System.Collections.Concurrent;

namespace ShopIt.Authentication.Application.Mocking;

/// <summary>
/// A single email as it would be delivered by a real email provider.
/// </summary>
/// <param name="To">The recipient address.</param>
/// <param name="Subject">The email subject.</param>
/// <param name="Body">The email body.</param>
/// <param name="ReceivedAt">UTC timestamp of delivery.</param>
public record MockEmail(string To, string Subject, string Body, DateTime ReceivedAt);

public interface IMockEmailService
{
    /// <summary>
    /// Stores an email so it can be displayed in the dev "mock inbox".
    /// </summary>
    void Deliver(MockEmail email);

    /// <summary>
    /// Returns the most recently delivered emails for the given address (newest first).
    /// </summary>
    IReadOnlyList<MockEmail> GetInbox(string email);
}

/// <summary>
/// In-memory stand-in for a real email provider. Emails are held per recipient
/// address so the Account views can display a "mock inbox" during development.
/// </summary>
public class MockEmailService : IMockEmailService
{
    private const int MaxEmailsPerInbox = 25;

    private readonly ConcurrentDictionary<string, List<MockEmail>> _inboxes = new(StringComparer.OrdinalIgnoreCase);

    public void Deliver(MockEmail email)
    {
        ArgumentNullException.ThrowIfNull(email);

        _inboxes.AddOrUpdate(
            email.To,
            _ => [email],
            (_, existing) =>
            {
                existing.Insert(0, email);

                if (existing.Count > MaxEmailsPerInbox)
                {
                    existing.RemoveRange(MaxEmailsPerInbox, existing.Count - MaxEmailsPerInbox);
                }

                return existing;
            });
    }

    public IReadOnlyList<MockEmail> GetInbox(string email) =>
        _inboxes.TryGetValue(email, out var inbox) ? inbox : [];
}
