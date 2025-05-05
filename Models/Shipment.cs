using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LogisticsRoutePlanner.Models
{
    public class Shipment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "請填寫任務名稱")]
        [StringLength(100, ErrorMessage = "名稱不能超過 100 字")]
        public string ShipmentName { get; set; }

        [Required(ErrorMessage = "請填寫出貨地址")]
        public string OriginAddress { get; set; }
        
        [Required(ErrorMessage = "請填寫配送物品")]
        [StringLength(200, ErrorMessage = "配送物品描述不能超過 200 字")]
        public string ProductInfo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "請填寫出貨日期")]
        public DateTime ShipmentDate { get; set; }

        // 新增車輛相關欄位
        [Required(ErrorMessage = "請選擇配送車輛")]
        [Display(Name = "配送車輛")]
        public string VehicleNumber { get; set; }

        public ICollection<ShipmentDestination> Destinations { get; set; } = new List<ShipmentDestination>();
    }
}