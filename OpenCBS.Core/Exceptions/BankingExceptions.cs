namespace OpenCBS.Core.Exceptions;

public class BankingException : Exception
{
    public BankingException(string message) : base(message) { }
}

public class InsufficientFundsException : BankingException
{
    public InsufficientFundsException(string message) : base(message) { }
}

public class AccountFrozenException : BankingException
{
    public AccountFrozenException(string message) : base(message) { }
}

public class UnbalancedJournalException : BankingException
{
    public UnbalancedJournalException(string message) : base(message) { }
}

public class InvalidContractStateException : BankingException
{
    public InvalidContractStateException(string message) : base(message) { }
}
