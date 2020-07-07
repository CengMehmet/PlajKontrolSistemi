using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlajKontrolSistemi.Entities
{
    class GirisBekleyen
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string barkod { get; set; }
        public DateTime? barkodTarih { get; set; }
    }
}
