namespace practice_2.Models
{
    public class CardDetails
    {
        public int Id { get; set; }
        public string CardHolderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string CVV { get; set; }
        public decimal CurrentBalance { get; set; } 
        public decimal SavingsBalance { get; set; } 
    }
}
