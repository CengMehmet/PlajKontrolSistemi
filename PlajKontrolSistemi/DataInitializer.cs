using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlajKontrolSistemi
{
    class DataInitializer : CreateDatabaseIfNotExists<PlajKontrol>
    {
       protected override void Seed(PlajKontrol context)
        {
            base.Seed(context);
        }
    }
}
