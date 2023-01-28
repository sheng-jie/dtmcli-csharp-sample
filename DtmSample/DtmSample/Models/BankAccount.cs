namespace DtmSample.Models
{
    public class BankAccount
    {
        public int Id { get; set; }
        public decimal Balance { get; set; }
    }

    public class TransferRequest
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }

        public TransferRequest(int userId, decimal amount)
        {
            UserId = userId;
            Amount = amount;
        }
    }   
}
