using MecaManage.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Auth.Commands;

public record ChangePasswordCommand(string Email, string CurrentPassword, string NewPassword) : IRequest<ChangePasswordResult>;

public record ChangePasswordResult(
    bool Success,
    string Message
);

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
{
    private readonly IApplicationDbContext _context;

    public ChangePasswordCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
            return new ChangePasswordResult(false, "Utilisateur non trouvé");

        // Verify the current password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return new ChangePasswordResult(false, "Mot de passe actuel incorrect");

        // Check if new password is the same as current password
        if (request.CurrentPassword == request.NewPassword)
            return new ChangePasswordResult(false, "Le nouveau mot de passe doit être différent du mot de passe actuel");

        // Update the password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResult(true, "Mot de passe modifié avec succès");
    }
}

