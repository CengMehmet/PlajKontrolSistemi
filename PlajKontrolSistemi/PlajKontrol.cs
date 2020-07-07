using PlajKontrolSistemi.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlajKontrolSistemi
{
    class PlajKontrol : DbContext
    {
        public PlajKontrol(string cs) : base(cs)
        {
            //Database.SetInitializer(new DataInitializer());
            Database.SetInitializer<PlajKontrol>(null);
        }
        public DbSet<Aboneler> AboneSet { get; set; }
        public DbSet<Ayarlar> AyarSet { get; set; }
        public DbSet<Barkodlar> BarkodSet { get; set; }
        public DbSet<GirisBekleyen> GirisBekleyenSet { get; set; }
        public DbSet<HareketKaydi> HareketKaydiSet { get; set; }
        public DbSet<Operator> OperatorSet { get; set; }
        public DbSet<MailAyar> MailAyarSet { get; set; }

    }
}
