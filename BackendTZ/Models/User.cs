using System.ComponentModel.DataAnnotations;

namespace BackendTZ.Models
{
    public class User
    {
        [Key]
        public int DeviceToken { get; set; }
        public string Key { get; set; }
        public string Option { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
