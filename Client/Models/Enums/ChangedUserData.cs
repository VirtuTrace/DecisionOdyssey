namespace Client.Models.Enums;

[Flags]
public enum ChangedUserData
{
    None            = 0,
    Email           = 1 << 0,
    SecondaryEmail  = 1 << 1,
    FirstName       = 1 << 2,
    LastName        = 1 << 3,
    LockoutEnd      = 1 << 4,
    Role            = 1 << 5
}