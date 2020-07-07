using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlajKontrolSistemi.Entities
{
    class MailAyar
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string kullaniciAdi { get; set; }
        [StringLength(100)]
        public string kullaniciSifre { get; set; }
        [StringLength(100)]
        public string gonderilecekMail { get; set; }
        public TimeSpan mailSaat { get; set; }
    }
}
