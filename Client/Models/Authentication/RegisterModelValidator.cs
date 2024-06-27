using FluentValidation;

namespace Client.Models.Authentication;

public class RegisterModelValidator : AbstractValidator<RegisterModel>
{
    public RegisterModelValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address");
        
        // Password validation rules
        RuleFor(x => x.Password)
           .NotEmpty().WithMessage("Password is required")
           .MinimumLength(15).WithMessage("Passwords must be at least 15 characters")
           .Matches(@"\d").WithMessage("Passwords must have at least one digit ('0'-'9')")
           .Matches("[A-Z]").WithMessage("Passwords must have at least one uppercase letter ('A'-'Z')")
           .Matches("[a-z]").WithMessage("Passwords must have at least one lowercase letter ('a'-'z')")
           .Matches(@"\W").WithMessage("Passwords must have at least one non alphanumeric character");
        
        // Confirm password validation
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Passwords must match");
    }
    
    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result =
            await ValidateAsync(ValidationContext<RegisterModel>.CreateWithOptions((RegisterModel)model,
                x => x.IncludeProperties(propertyName)));
        return result.IsValid ? Array.Empty<string>() : result.Errors.Select(e => e.ErrorMessage);
    };
}