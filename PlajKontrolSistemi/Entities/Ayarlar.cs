using System.ComponentModel.DataAnnotations;

namespace PlajKontrolSistemi
{
    internal class Ayarlar
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string barkodBaslik { get; set; }
        [StringLength(100)]
        public string aciklamaIlkSatir { get; set; }
        [StringLength(100)]
        public string aciklamaIkinciSatir { get; set; }

        [StringLength(100)]
        public string yaziciAdi { get; set; }
        [StringLength(50)]
        public string girisUcreti { get; set; }
        [StringLength(50)]
        public string sezlongUcret { get; set; }
        [StringLength(50)]
        public string semsiyeUcret { get; set; }
    }
}