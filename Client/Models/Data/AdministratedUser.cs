using System.Diagnostics.CodeAnalysis;
using Client.Models.Enums;
using Common.DataStructures.Dtos;

namespace Client.Models.Data;

public class AdministratedUser
{
    private string _email;
    private string? _secondaryEmail;
    private string _firstName;
    private string _lastName;
    private string _role;

    public Guid Guid { get; set; }

    public required string Email
    {
        get => _email;
        [MemberNotNull(nameof(_email))]
        set
        {
            _email = value;
            ChangedUserData |= ChangedUserData.Email;
        }
    }

    public string? SecondaryEmail
    {
        get => _secondaryEmail;
        set
        {
            _secondaryEmail = value;
            ChangedUserData |= ChangedUserData.SecondaryEmail;
        }
    }

    public required string FirstName
    {
        get => _firstName;
        [MemberNotNull(nameof(_firstName))]
        set
        {
            _firstName = value;
            ChangedUserData |= ChangedUserData.FirstName;
        }
    }

    public required string LastName
    {
        get => _lastName;
        [MemberNotNull(nameof(_lastName))]
        set
        {
            _lastName = value;
            ChangedUserData |= ChangedUserData.LastName;
        }
    }

    public DateTimeOffset? LockoutEnd { get; set; }

    public required string Role
    {
        get => _role;
        [MemberNotNull(nameof(_role))]
        set
        {
            _role = value;
            ChangedUserData |= ChangedUserData.Role;
        }
    }

    public ChangedUserData ChangedUserData { get; set; }
    
    public bool IsLockedOut
    {
        get => LockoutEnd is not null && LockoutEnd > DateTimeOffset.Now;
        set
        {
            LockoutEnd = value ? DateTimeOffset.MaxValue : null;
            ChangedUserData |= ChangedUserData.LockoutEnd;
        }
    }

    [SetsRequiredMembers]
    public AdministratedUser(AdvanceUserDto advanceUserDto)
    {
        Guid = advanceUserDto.Guid;
        Email = advanceUserDto.Email;
        SecondaryEmail = advanceUserDto.SecondaryEmail;
        FirstName = advanceUserDto.FirstName;
        LastName = advanceUserDto.LastName;
        LockoutEnd = advanceUserDto.LockoutEnd;
        Role = advanceUserDto.Role;
        ChangedUserData = ChangedUserData.None;
    }
}