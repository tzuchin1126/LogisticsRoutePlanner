using System.ComponentModel.DataAnnotations;

namespace LogisticsRoutePlanner.Models.ViewModels
{
    public class AddDestinationViewModel
    {
        [Required(ErrorMessage = "配送任務 ID 是必填的")]
        public int ShipmentId { get; set; }
        


        [Required(ErrorMessage = "客戶名稱是必填的")]
        [Display(Name = "客戶名稱")]
        public string CustomerName { get; set; }
        


        [Required(ErrorMessage = "地址是必填的")]
        [Display(Name = "地址")]
        public string Address { get; set; }
        

        
        [Display(Name = "備註（可選）")]
        public string Note { get; set; }
    }
}