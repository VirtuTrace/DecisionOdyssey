namespace Common.Enums;

public enum PasswordError
{
    None,
    PasswordTooShort,
    PasswordRequiresNonAlphanumeric,
    PasswordRequiresLower,
    PasswordRequiresUpper,
    PasswordRequiresDigit,
    PasswordRequiresUniqueChars,
    PasswordRequirementsNotMet,
    UnknownError
}