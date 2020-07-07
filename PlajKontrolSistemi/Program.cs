using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlajKontrolSistemi
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool kontrol;
            Mutex mutex = new Mutex(true, "Program", out kontrol); //Örnek Mutex nesnesi oluşturalım. 
            if (kontrol == false)
            {
                MessageBox.Show("Bu program zaten çalışıyor.");
                return;
            }
            Application.Run(new Form2());
            GC.KeepAlive(mutex); //Nesneyi kaldırıyoruz.
        }
    }
}
