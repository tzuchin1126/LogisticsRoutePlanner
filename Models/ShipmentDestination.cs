namespace LogisticsRoutePlanner.Models
{
    public enum DeliveryStatus
    {
        Pending,    // 待送
        Delivered,  // 已送達
        Skipped     // 跳過
    }

    public class ShipmentDestination
    {
        public int Id { get; set; }
        public int ShipmentId { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        // 替换原来的 string Status
        public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
        
        public string? Note { get; set; }
        public int SortOrder { get; set; }

        public Shipment Shipment { get; set; }
        public string? ProductInfo { get; internal set; }

        // 新增的跳過原因欄位
         public string? SkipReason { get; set; }
    }
}