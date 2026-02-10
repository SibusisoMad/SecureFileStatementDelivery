using SecureFileStatementDelivery.Application.Downloads;

namespace SecureFileStatementDelivery.Application.Interfaces;

public interface IDownloadTokenService
{
    string CreateToken(CreateDownloadTokenRequest request);
    bool TryValidate(string token, out ValidatedDownloadToken validated);
}
