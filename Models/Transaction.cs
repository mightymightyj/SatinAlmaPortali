namespace satinalmaportali2.Models {
    /// <summary>İşlemin yönü.</summary>
    public enum TransactionType {
        /// <summary>Alış — tedarikçiye ödenen, yani harcanan para.</summary>
        Purchase,
        /// <summary>Satış — karşı firmadan tahsil edilen para.</summary>
        Sale
    }

    /// <summary>Tek bir alış veya satış hareketi.</summary>
    public class Transaction {
        public int Id { get; set; }
        public int PartnerId { get; set; }
        public DateOnly Date { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
    }
}
