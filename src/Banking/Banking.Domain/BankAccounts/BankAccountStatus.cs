namespace Banking.Domain.BankAccounts;

public enum BankAccountStatus { Active, Closed }

public enum BankTransactionType { Credit, Debit }

public enum BankTransactionStatus { Unreconciled, Reconciled, Voided }
