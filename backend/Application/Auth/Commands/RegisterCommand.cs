using FluentValidation;
using MediatR;
using Darkhorse.Domain.Entities;
using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Domain.Interfaces.Services;

namespace Darkhorse.Application.Auth.Commands;

public record RegisterCommand(string Email, string Password) : IRequest<Guid>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12);
    }
}

public class RegisterCommandHandler(IUserRepository userRepository, IPasswordService passwordService) 
    : IRequestHandler<RegisterCommand, Guid>
{
    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new Exception("Email already registered."); // In a real app we'd obscure this

        var (hash, salt) = passwordService.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = false // Needs email verification
        };

        await userRepository.AddAsync(user, cancellationToken);
        
        // Output email verification integration goes here

        return user.Id;
    }
}
