using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;

namespace PlajKontrolSistemi
{
    public partial class Form2 : Form
    {
        [DllImport("user32.dll")]
        private static extern int FindWindow(string className, string windowText);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hwnd, int command);

        //window task barı gizler
        private const int HIDE = 0;

        //window task barı gösterir
        private const int SHOW = 1;

        private Form1 frm;
        public static int yetki;
        private string baglanti = "", key = "";
        private PlajKontrol db;

        #region windows tuşu engelleme

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardDLLStruct
        {
            public Keys key;

            public int scanCode;

            public int flags;

            public int time;

            public IntPtr extra;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string name);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short GetAsyncKeyState(Keys key);

        private IntPtr ptrHook;

        private LowLevelKeyboardProc objKeyboardProcess;

        #endregion windows tuşu engelleme

        public Form2()
        {
            InitializeComponent();

            xmlOku();
            //license Key = new license();if (Key.CPUSeriNo()+ Key.HDDserino() != key) { MessageBox.Show("Lisans Hatası"); Environment.Exit(0); return; }
            //kamerasayisi = 2;
            string session_id = "";
            db.AboneSet.ToList();
            operatorEkle();
        }

        public void operatorEkle()
        {
            if (db.OperatorSet.ToList().Count == 0)
            {
                Operator op = null;
                op = new Operator() { kullaniciAdi = "1", kullaniciSifre = "1", kullaniciYetki = 3, adSoyad = "Varsayılan Kullanıcı" };
                db.OperatorSet.Add(op);
                db.SaveChanges();
            }
        }

        public string xmlOku()
        {
            XmlTextReader oku = new XmlTextReader("config.xml");
            while (oku.Read())
            {
                if (oku.NodeType == XmlNodeType.Element)
                {
                    switch (oku.Name)
                    {
                        //SQL Bağlantı Ayarları
                        case "SqlBaglanti":
                            baglanti = oku.ReadString().ToString();
                            db = new PlajKontrol(baglanti);
                            break;

                            //case "key":
                            //    key = oku.ReadString().ToString();
                            //    break;
                    }
                }
            }
            oku.Close();
            return baglanti;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                sifrekontrol();
            }
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Bu uygulama kapanırken toolbar gizli ise
            int hwnd = FindWindow("Shell_TrayWnd", "");

            //gizli olan toolbar görünür yapma
            ShowWindow(hwnd, SHOW);
            gorevyoneticidurum(0);
            Application.Exit();
        }

        

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            if (textBoxsifre.Visible == false)
            {
                textBoxsifre.Visible = true; textBoxsifre.Focus();
            }
            else
            { textBoxsifre.Visible = false; }
        }

        private void textBoxsifre_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (textBoxsifre.Text == "recep123")
                {
                    //Bu uygulama kapanırken toolbar gizli ise
                    int hwnd = FindWindow("Shell_TrayWnd", "");

                    //gizli olan toolbar görünür yapma
                    ShowWindow(hwnd, SHOW);
                    gorevyoneticidurum(0);
                    Process.Start("taskmgr.exe");
                }
            }
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            sifrekontrol();
        }

        public void sifrekontrol()
        {
            string yetkistr = "", sifre = "", yetkili = "";
            try
            {
                var kullanici = db.OperatorSet.Where(i => i.kullaniciAdi == textBox1.Text).Select(j => new { j.kullaniciAdi, j.kullaniciSifre, j.kullaniciYetki }).FirstOrDefault();
                if (kullanici != null)
                {
                    yetkili = kullanici.kullaniciAdi;
                    sifre = kullanici.kullaniciSifre;
                    yetkistr = kullanici.kullaniciYetki.ToString();
                    if (sifre == textBox2.Text)
                    {
                        frm = new Form1();
                        frm.labelyetkili.Text = yetkili;
                        frm.operatorAdi = yetkili;
                        try
                        {
                            if (yetkistr == "0")
                            {
                                frm.panel1.Enabled = false;
                                frm.pictureBoxyetkililer.Enabled = false;
                                yetki = 0;
                            }
                            else if (yetkistr == "1")
                            {
                                frm.panel1.Enabled = true;
                                frm.pictureBoxyetkililer.Enabled = false;
                                yetki = 1;
                            }
                            else if (yetkistr == "3")
                            {
                                frm.panel1.Enabled = true;
                                frm.pictureBoxyetkililer.Enabled = true;
                                yetki = 3;
                            }
                            else { yetki = 1; }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.ToString());
                        }

                        //frm.kullaniciadi = yetkili;
                        //frm.kullaniciyetki = yetkistr;

                        frm.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Şifre Hatalı");
                    }
                }
                
            }
            catch (Exception ex)
            {
                //frm.dosyayaYaz(ex.ToString());
                MessageBox.Show(ex.ToString());
            }
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            //textBox1.Text += session_id;
            //OD_ERROR 0033 : Module Is Not Active
            //OD_ERROR 0002 : Device Not Found
            //B729F8004934AB63F12085A305DA9AC1
        }

        private IntPtr captureKey(int nCode, IntPtr wp, IntPtr lp)
        {
            if (nCode >= 0)
            {
                KeyboardDLLStruct objKeyInfo = (KeyboardDLLStruct)Marshal.PtrToStructure(lp, typeof(KeyboardDLLStruct));

                if (objKeyInfo.key == Keys.RWin || objKeyInfo.key == Keys.LWin) { return (IntPtr)1; }
            }
            return CallNextHookEx(ptrHook, nCode, wp, lp);
        }

        private void gorevyoneticidurum(int durum)
        {
            RegistryKey rkey1 = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies", true);

            rkey1.CreateSubKey("System", RegistryKeyPermissionCheck.Default);

            rkey1.Close();

            RegistryKey rkey2 = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);

            rkey2.SetValue("DisableTaskMgr", durum);

            rkey2.Close();
        }

        private void taskbargizle()
        {
            ////form pencresini bulalım (handle)
            int hwnd = FindWindow("Shell_TrayWnd", "");

            //window task bar gizli olacak
            // ShowWindow(hwnd, HIDE);

            //formun başlığı olmasın
            this.FormBorderStyle = FormBorderStyle.None;

            //pencereyi boyutunu tam ekran olacak şekilde ayarlayalım
            this.Size = new Size(SystemInformation.VirtualScreen.Width,
                                     SystemInformation.VirtualScreen.Height + this.Height - this.ClientSize.Height);

            //form penceresini ekranda ortalayalım
            CenterToScreen();
        }

        private void pictureBoxshutdown_Click(object sender, EventArgs e)
        {
            Process.Start("shutdown", "/s /t 0");
        }

        private void pictureBoxrestart_Click(object sender, EventArgs e)
        {
            Process.Start("shutdown", "/r /t 0");
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Application.ExitThread();
            Application.Exit();
        }

        private void turkceYap()
        {
            label1.Text = "Kullanıcı Adı:";
            label2.Text = "Şifre:";
        }

        private void ingilizceYap()
        {
            label1.Text = "Username:";
            label2.Text = "Password:";
        }
    }
}