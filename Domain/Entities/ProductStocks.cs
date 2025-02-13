using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class ProductStocks : AuditedEntity
    {
        public Guid Id { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal SalesQty { get; set; }
        public decimal BegQty { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product ProductFk { get; set; }
        public Guid InventoryBeginningId { get; set; }
        [ForeignKey("InventoryBeginningId")]
        public InventoryBeginning InventoryBeginningFk { get; set; }
    }

}
