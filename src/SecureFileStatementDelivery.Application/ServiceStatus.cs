namespace SecureFileStatementDelivery.Application;

public sealed record ServiceStatus(
    string Status,
    bool CanConnectDb,
    bool HasStatements,
    string DataDir,
    bool StatementsDirExists);
