using Darkhorse.Domain.Entities;
using Darkhorse.Domain.Interfaces.Services;
using Darkhorse.Domain.Interfaces.Repositories;
using MediatR;

namespace Darkhorse.Application.Brokers.Commands;

public record CreateBrokerCommand(
    Guid UserId,
    string BrokerName,
    string ApiKey,
    string Secret,
    decimal FeeRate,
    decimal FundingRate,
    bool IsSandbox) : IRequest<Guid>;

public class CreateBrokerCommandHandler(
    IBrokerCredentialRepository repo,
    ICredentialEncryption encryptor)
    : IRequestHandler<CreateBrokerCommand, Guid>
{
    public async Task<Guid> Handle(CreateBrokerCommand request, CancellationToken cancellationToken)
    {
        var (keyNonce, keyCipher, keyTag) = encryptor.Encrypt(request.ApiKey);
        var (secNonce, secCipher, secTag) = encryptor.Encrypt(request.Secret);

        var credential = new BrokerCredential
        {
            UserId = request.UserId,
            BrokerName = request.BrokerName,
            ApiKeyNonce = keyNonce,
            ApiKeyCipher = keyCipher,
            ApiKeyTag = keyTag,
            SecretNonce = secNonce,
            SecretCipher = secCipher,
            SecretTag = secTag,
            FeeRate = request.FeeRate,
            FundingRate = request.FundingRate,
            IsSandbox = request.IsSandbox,
            KeyVersion = 1,
            Status = "active"
        };

        await repo.AddAsync(credential, cancellationToken);
        return credential.Id;
    }
}
