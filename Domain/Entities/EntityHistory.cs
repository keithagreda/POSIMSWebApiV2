namespace Domain.Entities
{
    public class EntityHistory
    {
        public int Id { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string Changes { get; set; }
        public DateTime ChangeTime { get; set; }
        public string ChangedBy { get; set; }
        public string Action { get; set; }
    }
}
