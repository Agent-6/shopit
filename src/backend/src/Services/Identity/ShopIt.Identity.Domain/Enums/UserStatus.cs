namespace ShopIt.Identity.Domain.Enums;

public enum UserStatus
{
    Active,
    Inactive,
    Suspended,

    /// <summary>
    /// User was invited by an admin but has not yet clicked the activation link
    /// and chosen a password. Such users cannot sign in until they activate.
    /// </summary>
    PendingActivation,
}
