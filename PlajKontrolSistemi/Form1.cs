
using PlajKontrolSistemi.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace PlajKontrolSistemi
{
    public partial class Form1 : Form
    {
        private DataGridViewPrinter MyDataGridViewPrinter;
        private int veritabaniyedeklendi = 0;
        private string baglanti;
        private PlajKontrol db;
        public string operatorAdi;
        long yazilacakBarkod;
        string mailGonderilsin = "0";
        bool pdfOlustu = false;
        MailAyar mailAyarlari = new MailAyar();
        bool mailYollandi = false;
        double _dpi = 96;
        string ptsBaglanti;
        string yaziciAdi;
        decimal sezlongUcret = 0;
        decimal semsiyeUcret = 0;
        public Form1()
        {
            InitializeComponent();
            xmlOku();
            db.AboneSet.ToList();
            comboBoxAbonelerTip.SelectedIndex = 0;
            baslangicAyarEkle();
            //istatistikGoster();
        }

        public void xmlOku()
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
                        case "MailGonder":
                            mailGonderilsin = oku.ReadString().ToString();
                            break;
                        case "PtsBaglanti":
                            ptsBaglanti = oku.ReadString().ToString();
                            break;
                        case "Yazici":
                            yaziciAdi = oku.ReadString().ToString();
                            break;
                    }
                }
            }
            oku.Close();
        }

        public void xmlYaz()
        {
            string xmlDosyasi = @"config.xml";
            XmlWriter xmlYazici = XmlWriter.Create(xmlDosyasi);
            
            xmlYazici.WriteStartDocument();
            xmlYazici.WriteStartElement("AYARLAR");
            xmlYazici.WriteElementString("SqlBaglanti", baglanti);
            xmlYazici.WriteElementString("MailGonder", mailGonderilsin);
            xmlYazici.WriteElementString("PtsBaglanti",ptsBaglanti);
            xmlYazici.WriteElementString("Yazici", comboBoxYazici.Text);
            xmlYazici.WriteEndElement();
            xmlYazici.WriteEndDocument();
            xmlYazici.Close();
        }

        public void baslangicAyarEkle()
        {
            if (db.AyarSet.ToList().Count == 0)
            {
                Ayarlar ayar = null;
                ayar = new Ayarlar() { barkodBaslik = "White Rose", aciklamaIlkSatir = "Turnike Geçiş Sistemi", aciklamaIkinciSatir = "Hoşgeldiniz", yaziciAdi = "XP-80C", girisUcreti="0,00",sezlongUcret="0,00",semsiyeUcret="0,00" };
                db.AyarSet.Add(ayar);
                db.SaveChanges();
            }
            if(db.BarkodSet.ToList().Count == 0)
            {
                Barkodlar barkod = null;
                barkod = new Barkodlar() { barkod = "100000000001" };
                db.BarkodSet.Add(barkod);
                db.SaveChanges();
            }
            ayarDoldur();
        }

        private void textBoxKayitlarBarkodFiltre_TextChanged(object sender, EventArgs e)
        {
            string girilenBarkod = textBoxKayitlarBarkodFiltre.Text;
            if (textBoxKayitlarBarkodFiltre.Text != "")
            {
                var hareketler = db.HareketKaydiSet.Where(i=>i.barkod.Contains(girilenBarkod)).
                    Select(i => new { i.barkod,i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi,i.aciklama }).
                    OrderByDescending(j => j.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = hareketler;
            }
            else
            {
                kayitlarGetir();
            }
        }

        private void pictureBoxkayitlar_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
            operatorGetir();
            kayitlarGetir();
            istatistikGoster();
        }
        private void operatorGetir()
        {
            comboBoxKayitlarOperator.Items.Clear();
            comboBoxKayitlarOperator.Items.Add("Tümü");
            var operatorler = db.OperatorSet.Select(i => i.kullaniciAdi).ToList();

            foreach (var item in operatorler)
            {
                comboBoxKayitlarOperator.Items.Add(item);
            }
        }
        
        private void kayitlarGetir()
        {
            var hareketler = db.HareketKaydiSet.Select(i=> new { i.barkod,i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi,i.aciklama }).OrderByDescending(j=>j.barkodTarih).ToList();
            dataGridViewKayitlar.DataSource = hareketler;
            comboBoxKayitlarOperator.SelectedIndex = 0;
        }

        private void buttonKayitlarBiletliGirisleriGetir_Click(object sender, EventArgs e)
        {
            string operatorAdi = comboBoxKayitlarOperator.SelectedItem.ToString();
            DateTime baslangic = Convert.ToDateTime(dateTimePickerKayitlarFitreBaslangic.Text);
            DateTime bitis = Convert.ToDateTime(dateTimePickerKayitlarFiltreBitis.Text);
            if (baslangic == bitis) { MessageBox.Show("Lütfen Öncelikle Tarih Aralığını Seçiniz!", "Tarih Aralığı Seçilmedi...");return; }
            if (comboBoxKayitlarOperator.SelectedItem.ToString() != "Tümü")
            {
                var operatorKayitList = db.HareketKaydiSet.Where(i => i.barkod.Length==12 && i.operatorAdi == operatorAdi && i.barkodTarih >= baslangic && i.barkodTarih <= bitis).
                    Select(i => new { i.barkod, i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi, i.aciklama }).OrderByDescending(i => i.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = operatorKayitList;
            }
            else
            {
                var operatorKayitList = db.HareketKaydiSet.Where(i => i.barkod.Length == 12 && i.barkodTarih >= baslangic && i.barkodTarih <= bitis).
                    Select(i => new { i.barkod, i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi, i.aciklama }).OrderByDescending(i => i.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = operatorKayitList;
            }
        }

        private void buttonKayitlarAboneGirislerGetir_Click(object sender, EventArgs e)
        {
            string operatorAdi = comboBoxKayitlarOperator.SelectedItem.ToString();
            DateTime baslangic = Convert.ToDateTime(dateTimePickerKayitlarFitreBaslangic.Text);
            DateTime bitis = Convert.ToDateTime(dateTimePickerKayitlarFiltreBitis.Text);
            if (baslangic == bitis) { MessageBox.Show("Lütfen Öncelikle Tarih Aralığını Seçiniz!", "Tarih Aralığı Seçilmedi..."); return; }
            if (comboBoxKayitlarOperator.SelectedItem.ToString() != "Tümü")
            {
                var operatorKayitList = db.HareketKaydiSet.Where(i => i.barkod.Length == 10 && i.operatorAdi == operatorAdi && i.barkodTarih >= baslangic && i.barkodTarih <= bitis).
                    Select(i => new { i.barkod, i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi, i.aciklama }).OrderByDescending(i => i.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = operatorKayitList;
            }
            else
            {
                var operatorKayitList = db.HareketKaydiSet.Where(i => i.barkod.Length == 10 && i.barkodTarih >= baslangic && i.barkodTarih <= bitis).
                    Select(i => new { i.barkod, i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi, i.aciklama }).OrderByDescending(i => i.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = operatorKayitList;
            }
        }

        private void comboBoxKayitlarOperator_SelectedIndexChanged(object sender, EventArgs e)
        {
            string operatorAdi = comboBoxKayitlarOperator.SelectedItem.ToString();
            DateTime today = DateTime.Now.Date;
            if (comboBoxKayitlarOperator.SelectedItem.ToString() != "Tümü")
            {
                var operatorKayitList = db.HareketKaydiSet.Where(i => i.operatorAdi == operatorAdi && i.barkodTarih > today).
                    Select(i => new { i.barkod, i.barkodTarih, i.girisTarih,i.ucret, i.girisNoktasi, i.durum, i.aboneAdi, i.aciklama }).OrderByDescending(i=>i.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = operatorKayitList;
            }
            else
            {
                var operatorKayitList = db.HareketKaydiSet.Where(i => i.barkodTarih > today).
                    Select(i => new { i.barkod, i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi, i.aciklama }).OrderByDescending(i => i.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = operatorKayitList;
            }
        }

        private void buttonKayitlarOperatorTarihleFiltrele_Click(object sender, EventArgs e)
        {
            string operatorAdi = comboBoxKayitlarOperator.SelectedItem.ToString();
            DateTime baslangic = Convert.ToDateTime(dateTimePickerKayitlarFitreBaslangic.Text);
            DateTime bitis = Convert.ToDateTime(dateTimePickerKayitlarFiltreBitis.Text);
            if (comboBoxKayitlarOperator.SelectedItem.ToString() != "Tümü")
            {
                var operatorKayitList = db.HareketKaydiSet.Where(i => i.operatorAdi == operatorAdi && i.barkodTarih >= baslangic && i.barkodTarih <= bitis).
                    Select(i => new { i.barkod,i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi, i.aciklama }).OrderByDescending(i=>i.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = operatorKayitList;
            }
            else
            {
                var operatorKayitList = db.HareketKaydiSet.Where(i => i.barkodTarih >= baslangic && i.barkodTarih <= bitis).
                    Select(i => new { i.barkod,i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi, i.aciklama }).OrderByDescending(i => i.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = operatorKayitList;
            }
        }

        private void pictureBoxKayitlarFiltrele_Click(object sender, EventArgs e)
        {
            DateTime baslangic = Convert.ToDateTime(dateTimePickerKayitlarFitreBaslangic.Text);
            DateTime bitis = Convert.ToDateTime(dateTimePickerKayitlarFiltreBitis.Text);
            if (radioButtonKayitlarFiltreTumIslemler.Checked)
            {
                var tumHareketlerKayitList = db.HareketKaydiSet.
                    Where(i => i.barkodTarih >= baslangic && i.barkodTarih <= bitis).
                    Select(i=> new { i.barkod, i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi,i.aciklama }).OrderByDescending(i => i.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = tumHareketlerKayitList;
                if (dataGridViewKayitlar.Rows.Count > 0)
                {
                    int biletliGirisSayisi = db.HareketKaydiSet.Where(i => i.barkod.Length == 12 && i.barkodTarih >= baslangic && i.barkodTarih <= bitis && i.aciklama != "AbonelikYenileme").Count();
                    labelKayitlarToplamBiletliGiris.Text = biletliGirisSayisi.ToString() + " Kişi";
                    int aboneGirisSayisi = db.HareketKaydiSet.Where(i => i.barkod.Length == 10 && i.barkodTarih >= baslangic && i.barkodTarih <= bitis && i.aciklama != "AbonelikYenileme").Count();
                    labelKayitlarToplamAboneGiris.Text = aboneGirisSayisi.ToString() + " Kişi";
                    int toplamSezlong = db.HareketKaydiSet.Where(i => i.barkod == "Sezlong" && i.barkodTarih >= baslangic && i.barkodTarih <= bitis && i.aciklama != "AbonelikYenileme").ToList().Sum(i=> Convert.ToInt32(i.aciklama));
                    labelToplamSezlongFiltre.Text = toplamSezlong.ToString() + " Adet";
                    int toplamSemsiye = db.HareketKaydiSet.Where(i => i.barkod == "Semsiye" && i.barkodTarih >= baslangic && i.barkodTarih <= bitis && i.aciklama != "AbonelikYenileme").ToList().Sum(i => Convert.ToInt32(i.aciklama));
                    labelToplamSemsiyeFiltre.Text = toplamSemsiye.ToString() + " Adet";
                    decimal hasilat = db.HareketKaydiSet.Where(i => i.barkodTarih >= baslangic && i.barkodTarih <= bitis).Sum(j => j.ucret).Value;
                    labelKayitlarToplamHasilat.Text = hasilat.ToString() + " TL";
                }              
            }
            else if (radioButtonKayitlarFiltreAbonelikIslemler.Checked)
            {
                var abonelikYenilemeKayitList = db.HareketKaydiSet.Where(i => i.barkodTarih >= baslangic && i.barkodTarih <= bitis && i.aciklama=="AbonelikYenileme").
                    Select(i => new { i.barkod, i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi,i.aciklama }).OrderByDescending(i => i.barkodTarih).ToList();
                dataGridViewKayitlar.DataSource = abonelikYenilemeKayitList;
                labelKayitlarToplamBiletliGiris.Text = "";
                labelKayitlarToplamAboneGiris.Text = "";
                labelToplamSezlongFiltre.Text = "";
                labelToplamSemsiyeFiltre.Text = "";
            }
            
        }
        #region ABONE İŞLEMLERİ
        private void pictureBoxaboneler_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 2;
            aboneleriGetir();
        }
        private void aboneleriGetir()
        {
            var aboneler = db.AboneSet.
                Select(j => new {
                    j.aboneKart,
                    j.aboneAdSoyad,
                    j.aboneTipi,
                    j.aboneBitisTarih
                }).
                    OrderBy(i => i.aboneBitisTarih).ToList();
            dataGridViewAbone.DataSource = aboneler;
        }


        private void textBoxAboneKartNoAra_TextChanged(object sender, EventArgs e)
        {
            var aboneler = db.AboneSet.
                Where(i=>i.aboneKart.Contains(textBoxAboneKartNo.Text)).
                Select(j => new {
                    j.aboneKart,
                    j.aboneAdSoyad,
                    j.aboneTipi,
                    j.aboneBitisTarih
                }).
                    OrderBy(i => i.aboneBitisTarih).ToList();
            dataGridViewAbone.DataSource = aboneler;
        }

        private void textBoxAbonelerAboneAdiylaAra_TextChanged(object sender, EventArgs e)
        {
            var aboneler = db.AboneSet.
                Where(i => i.aboneAdSoyad.Contains(textBoxAboneAdSoyad.Text)).
                Select(j => new {
                    j.aboneKart,
                    j.aboneAdSoyad,
                    j.aboneTipi,
                    j.aboneBitisTarih
                }).
                    OrderBy(i => i.aboneBitisTarih).ToList();
            dataGridViewAbone.DataSource = aboneler;
        }

        #endregion

        private void dataGridViewAbone_SelectionChanged(object sender, EventArgs e)
        {
            string kartNo = "";
            DataGridView dgv = sender as DataGridView;
            if (dgv != null && dgv.SelectedRows.Count > 0)
            {
                DataGridViewRow row = dgv.SelectedRows[0];
                if (row != null)
                {
                    kartNo = row.Cells[0].Value.ToString();
                    var id = db.AboneSet.Where(i => i.aboneKart == kartNo).FirstOrDefault();
                    labelid.Text = id.Id.ToString();
                }
            }
        }

        private void labelid_TextChanged(object sender, EventArgs e)
        {
            if (labelid.Text != "ID")
            {
                int id = Convert.ToInt32(labelid.Text);
                var abone = db.AboneSet.Where(i => i.Id == id).FirstOrDefault();
                textBoxAboneAdSoyad.Text = abone.aboneAdSoyad;
                if(abone.aboneTipi == "BitisTarih") { comboBoxAbonelerTip.Text = "Bitiş Tarihli"; }
                else if(abone.aboneTipi == "Bakiye") { comboBoxAbonelerTip.Text = "Bakiyeli"; }
                else if(abone.aboneTipi == "KalanGiris") { comboBoxAbonelerTip.Text = "Giriş Sayılı"; }
                textBoxAboneKartNo.Text = abone.aboneKart;
                textBoxAboneGuncellemeKartNo.Text = abone.aboneKart;
                textBoxAbonelerAboneTipi.Text = comboBoxAbonelerTip.Text;
                buttonAboneGuncelle.Enabled = true;
                buttonAboneYeniKaydet.Enabled = false;
            }
            else if (labelid.Text == "ID")
            {
                comboBoxAbonelerTip.SelectedIndex = 0;
                textBoxAboneAdSoyad.Clear();
                comboBoxAbonelerTip.Text = "";
                textBoxAboneKartNo.Clear();
                textBoxAbonelerAboneTipi.Clear();
                textBoxAboneGuncellemeKartNo.Clear();
                buttonAboneGuncelle.Enabled = false;
                buttonAboneYeniKaydet.Enabled = true;
            }
        }

        private void textBoxAbonelerAboneTipi_TextChanged(object sender, EventArgs e)
        {
            if (textBoxAbonelerAboneTipi.Text == "Bitiş Tarihli")
            {
                dateTimePickerAboneGuncellemeTarih.Enabled = true;
                textBoxAboneGuncellemeYuklenecekBakiye.Enabled = false;
                textBoxAboneGuncellemeGirisSayi.Enabled = false;
            }
            else if (textBoxAbonelerAboneTipi.Text == "Bakiyeli")
            {
                dateTimePickerAboneGuncellemeTarih.Enabled = false;
                textBoxAboneGuncellemeYuklenecekBakiye.Enabled = true;
                textBoxAboneGuncellemeGirisSayi.Enabled = false;
            }
            else if (textBoxAbonelerAboneTipi.Text == "Giriş Sayılı")
            {
                dateTimePickerAboneGuncellemeTarih.Enabled = false;
                textBoxAboneGuncellemeYuklenecekBakiye.Enabled = false;
                textBoxAboneGuncellemeGirisSayi.Enabled = true;
            }
            else
            {
                dateTimePickerAboneGuncellemeTarih.Enabled = false;
                textBoxAboneGuncellemeYuklenecekBakiye.Enabled = false;
                textBoxAboneGuncellemeGirisSayi.Enabled = false;
            }
        }

        private void buttonAboneBakiyeGuncelle_Click(object sender, EventArgs e)
        {
            string kartNo = textBoxAboneGuncellemeKartNo.Text;
            HareketKaydi hareketKaydi = null;
            Aboneler abone = null;
            abone = db.AboneSet.FirstOrDefault(i => i.aboneKart == kartNo);
            
            if (abone.aboneTipi == "BitisTarih")
            {
                if (abone.aboneBitisTarih > Convert.ToDateTime(abone.aboneBitisTarih))
                {
                    DialogResult res = MessageBox.Show("Seçilen Tarih Aboneliğin Bitiş Tarihinden Erken Bir Tarih Seçildi!!" +
                        " Daha Erken Bir Tarihi Onaylıyor Musunuz?",
                        "Erken Tarih Seçildi", MessageBoxButtons.YesNo);
                    if (res == DialogResult.No)
                    {
                        return;
                    }
                    else
                    {
                        abone.aboneBitisTarih = Convert.ToDateTime(dateTimePickerAboneGuncellemeTarih.Text);
                        db.SaveChanges();                       
                    }
                }                  
            }           
            else if (abone.aboneTipi == "Bakiye")
            {
                if (Convert.ToDecimal(textBoxAboneGuncellemeYuklenecekBakiye.Text) < 0)
                {
                    DialogResult res = MessageBox.Show("Abone Bakiyesi Azaltılacaktır!!" +
                    " Onaylıyor Musunuz?",
                    "Eksi Bakiye Seçildi...", MessageBoxButtons.YesNo);
                    if (res == DialogResult.No)
                    {
                        return;
                    }
                    else
                    {
                        abone.aboneBakiye = abone.aboneBakiye + float.Parse(textBoxAboneGuncellemeYuklenecekBakiye.Text);
                        db.SaveChanges();                     
                    }
                }
                
            }
            else if (abone.aboneTipi == "KalanGiris")
            {
                if (Convert.ToInt32(textBoxAboneGuncellemeGirisSayi.Text) < 0)
                {
                    DialogResult res = MessageBox.Show("Abone Giriş Sayısı Azaltılacaktır!!" +
                    " Onaylıyor Musunuz?",
                    "Eksi Giriş Sayısı Seçildi...", MessageBoxButtons.YesNo);
                    if (res == DialogResult.No)
                    {
                        return;
                    }
                    else
                    {
                        abone.aboneKalanGiris = abone.aboneKalanGiris + Convert.ToInt32(textBoxAboneGuncellemeGirisSayi.Text);
                        db.SaveChanges();
                    }
                }

            }
            hareketKaydi = new HareketKaydi()
            {
                barkod = abone.aboneKart,
                aboneAdi = abone.aboneAdSoyad,
                aciklama = "AbonelikYenileme",
                ucret = Convert.ToDecimal(textBoxAboneGuncellemeYuklenecekBakiye.Text),
                durum = 2,
                girisNoktasi = "Gişe",
                girisTarih = DateTime.Now,
                operatorAdi = operatorAdi
            };
            db.HareketKaydiSet.Add(hareketKaydi);
            db.SaveChanges();
            aboneleriGetir();

        }

        private void buttonAboneYeniKaydet_Click(object sender, EventArgs e)
        {
            Aboneler abone = null;
            abone = db.AboneSet.FirstOrDefault(i => i.aboneKart == textBoxAboneKartNo.Text);
            if (abone != null) { MessageBox.Show("Bu Kart Numarası ile Sistemde Kayıtlı Bir Abone Bulunmaktadır." +
                "Sadece Güncelleme İşlemi Yapılabilir."); return; }
            if(comboBoxAbonelerTip.Text == "" || textBoxAboneAdSoyad.Text=="" || textBoxAboneKartNo.Text=="")
            {
                MessageBox.Show("Lütfen Tüm Bilgileri Eksiksiz Doldurup Tekrar Deneyin", "Bilgi Eksik"); return;
            }
            string aboneTip = "";
            if(comboBoxAbonelerTip.Text == "Bitiş Tarihli") { aboneTip = "BitisTarih"; }
            else if (comboBoxAbonelerTip.Text == "Bakiyeli") { aboneTip = "Bakiye"; }
            else if (comboBoxAbonelerTip.Text == "Giriş Sayılı") { aboneTip = "KalanGiris"; }
            abone = new Aboneler()
            {
                aboneKart = textBoxAboneKartNo.Text,
                aboneAdSoyad = textBoxAboneAdSoyad.Text,
                aboneTipi = aboneTip,
                aboneSinirsizErisim = false,
                aboneBitisTarih = Convert.ToDateTime("2099-12-30"),
                aboneBakiye=0,
                aboneKalanGiris=0
            };
            db.AboneSet.Add(abone);
            db.SaveChanges();
            aboneleriGetir();
        }

        private void buttonAboneBilgiTemizle_Click(object sender, EventArgs e)
        {
            labelid.Text = "ID";
        }

        private void textBoxAboneGuncellemeYuklenecekBakiye_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ','))
            {
                e.Handled = true;
            }
        }

        private void textBoxAboneGuncellemeGirisSayi_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void textBoxAboneGuncelleTahsilEdilenUcret_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ','))
            {
                e.Handled = true;
            }
        }

        private void buttonAboneGuncelle_Click(object sender, EventArgs e)
        {
            if (comboBoxAbonelerTip.Text == "") { MessageBox.Show("Abone Tipi Seçimi Yapılmadan Güncelleme Yapılamaz.", "Seçim Yapılmadı.");return; }
            int aboneId = Convert.ToInt32(labelid.Text);
            Aboneler abone = null;
            abone = db.AboneSet.FirstOrDefault(i => i.Id == aboneId);
            DialogResult res = MessageBox.Show(abone.aboneAdSoyad + " isimli abonenin bilgileri girilen bilgiler doğrultusunda değiştirilecektir.Onaylıyor Musunuz?", "Güncelleme Onay", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                string aboneTip = "";
                if (comboBoxAbonelerTip.Text == "Bitiş Tarihli") { aboneTip = "BitisTarih"; }
                else if (comboBoxAbonelerTip.Text == "Bakiyeli") { aboneTip = "Bakiye"; }
                else if (comboBoxAbonelerTip.Text == "Giriş Sayılı") { aboneTip = "KalanGiris"; }
                abone.aboneKart = textBoxAboneKartNo.Text;
                abone.aboneAdSoyad = textBoxAboneAdSoyad.Text;
                abone.aboneTipi = aboneTip;
                db.SaveChanges();
            }
            labelid.Text = "ID";
            aboneleriGetir();

        }

       

        private void siradakiBarkoduVer()
        {

            PrintDocument pdPrint = new PrintDocument();
            pdPrint.PrintPage += new PrintPageEventHandler(pdPrint_PrintPage);
            pdPrint.PrinterSettings.PrinterName = comboBoxYazici.Text;
            pdPrint.Print();
        }

        private void pdPrint_PrintPage(object sender, PrintPageEventArgs e)
        {
            float x, y, lineOffset;
            double toplam = 0;
            // Font seçimi
            System.Drawing.Font printFont = new System.Drawing.Font("Calibri", (float)9, FontStyle.Regular, GraphicsUnit.Point); // Substituted to FontA Font
            System.Drawing.Font tarihFont = new System.Drawing.Font("Calibri", (float)9, FontStyle.Regular, GraphicsUnit.Point);
            System.Drawing.Font ucretFont = new System.Drawing.Font("Calibri", (float)14, FontStyle.Regular, GraphicsUnit.Point);
            System.Drawing.Font aciklamaFont = new System.Drawing.Font("Calibri", (float)7, FontStyle.Regular, GraphicsUnit.Point);
            e.Graphics.PageUnit = GraphicsUnit.Point;
           
            // Fişi hazırla
            lineOffset = 7 + printFont.GetHeight(e.Graphics) - (float)3.5;
            x = 0;
            y = 0;
            //ThermalLabel tLabel = new ThermalLabel(UnitType.Mm, 40, 30);


            ////Define a BarcodeItem object
            //BarcodeItem bcItem = new BarcodeItem(10, 2, 31.35, 22.85, BarcodeSymbology.Ean13, yazilacakBarkod.ToString());
            ////Set bars height to .75inch
            //bcItem.BarHeight = 25.93;
            ////Set bars width to 0.0104inch
            //bcItem.BarWidth = 0.33;
            //bcItem.DisplayCode = false;
            ////Add items to ThermalLabel object...
            //tLabel.Items.Add(bcItem);
            ////BarcodeItem barcode = new BarcodeItem(x, y,Convert.ToInt32(e.PageSettings.PrintableArea.Width), 100, BarcodeSymbology.Ean13, yazilacakBarkod.ToString());
            Image img;
            //using (PrintJob pj = new PrintJob())
            //{
            //    pj.ThermalLabel = tLabel;
            //    System.IO.MemoryStream ms = new System.IO.MemoryStream();
            //    pj.ExportToImage(ms, new ImageSettings(ImageFormat.Jpeg), _dpi);
            //    img = Image.FromStream(ms);
            //}
            Zen.Barcode.CodeEan13BarcodeDraw brCode =
            Zen.Barcode.BarcodeDrawFactory.CodeEan13WithChecksum;
            img = brCode.Draw(yazilacakBarkod.ToString(), 60);
            y = 20;
            e.Graphics.DrawImage(img, x+30, y);

            y += 50;
            e.Graphics.DrawString(yazilacakBarkod.ToString(), printFont, Brushes.Black, Convert.ToInt32(58 - yazilacakBarkod.ToString().Length), y);
            y += lineOffset;
            e.Graphics.DrawString("______________________________________", printFont, Brushes.Black, x, y);
            y += lineOffset;
            e.Graphics.DrawString(textBoxFirmaBaslik.Text, printFont, Brushes.Black, Convert.ToInt32(58-(textBoxFirmaBaslik.Text).Length), y);
            y += lineOffset;
            e.Graphics.DrawString(DateTime.Now.ToString("dd-MM-yyyy HH:mm"), tarihFont, Brushes.Black, Convert.ToInt32(52 - (DateTime.Now.ToString("yyyy-MM-dd HH:mm").ToString().Length)), y);
            y += lineOffset;
            toplam = Convert.ToDouble(textBoxAyarlarGirisUcreti.Text);
            e.Graphics.DrawString(toplam + " TL",ucretFont,Brushes.Black, Convert.ToInt32(58 - ((toplam + " TL").Length)), y);
            y += lineOffset;
            e.Graphics.DrawString(textBoxBarkodAciklamaIlkSatir.Text, aciklamaFont, Brushes.Black, Convert.ToInt32(58 - (textBoxBarkodAciklamaIlkSatir.Text.Length)), y);
            y += lineOffset;
            e.Graphics.DrawString(textBoxBarkodAciklamaIkinciSatir.Text, aciklamaFont, Brushes.Black, Convert.ToInt32(58 - (textBoxBarkodAciklamaIkinciSatir.Text.Length)), y);
            e.HasMorePages = false;     //yazdırma işlemi tamamlandı
        }


        private void sezlongBiletVer()
        {
            PrintDocument pdPrint = new PrintDocument();
            pdPrint.PrintPage += new PrintPageEventHandler(sezlongPrint_PrintPage);
            pdPrint.PrinterSettings.PrinterName = comboBoxYazici.Text;
            pdPrint.Print();
        }


        private void sezlongPrint_PrintPage(object sender, PrintPageEventArgs e)
        {
            float x, y, lineOffset;
            double toplam = 0;
            decimal sezlongAdet = 0;
            // Font seçimi
            System.Drawing.Font printFont = new System.Drawing.Font("Calibri", (float)9, FontStyle.Regular, GraphicsUnit.Point); // Substituted to FontA Font
            System.Drawing.Font tarihFont = new System.Drawing.Font("Calibri", (float)9, FontStyle.Regular, GraphicsUnit.Point);
            System.Drawing.Font ucretFont = new System.Drawing.Font("Calibri", (float)14, FontStyle.Regular, GraphicsUnit.Point);
            System.Drawing.Font aciklamaFont = new System.Drawing.Font("Calibri", (float)7, FontStyle.Regular, GraphicsUnit.Point);
            e.Graphics.PageUnit = GraphicsUnit.Point;

            // Fişi hazırla
            lineOffset = 7 + printFont.GetHeight(e.Graphics) - (float)3.5;
            x = 0;
            y = 0;
            e.Graphics.DrawString("______________________________________", printFont, Brushes.Black, x, y);
            y += lineOffset;
            sezlongAdet = numericUpDownSezlong.Value;
            e.Graphics.DrawString(sezlongAdet + " Adet Şezlong", printFont, Brushes.Black, Convert.ToInt32(58 - ((sezlongAdet + " Adet Şezlong").Length)), y);
            y += lineOffset;
            e.Graphics.DrawString(textBoxFirmaBaslik.Text, printFont, Brushes.Black, Convert.ToInt32(58 - (textBoxFirmaBaslik.Text).Length), y);
            y += lineOffset;
            e.Graphics.DrawString(DateTime.Now.ToString("dd-MM-yyyy HH:mm"), tarihFont, Brushes.Black, Convert.ToInt32(52 - (DateTime.Now.ToString("yyyy-MM-dd HH:mm").ToString().Length)), y);
            y += lineOffset;
            toplam = Convert.ToDouble(textBoxSezlongUcret.Text)*Convert.ToDouble(numericUpDownSezlong.Value);
            e.Graphics.DrawString(toplam + " TL", ucretFont, Brushes.Black, Convert.ToInt32(58 - ((toplam + " TL").Length)), y);
            e.HasMorePages = false;     //yazdırma işlemi tamamlandı
        }

        private void semsiyeBiletVer()
        {
            PrintDocument pdPrint = new PrintDocument();
            pdPrint.PrintPage += new PrintPageEventHandler(semsiyePrint_PrintPage);
            pdPrint.PrinterSettings.PrinterName = comboBoxYazici.Text;
            pdPrint.Print();
        }


        private void semsiyePrint_PrintPage(object sender, PrintPageEventArgs e)
        {
            float x, y, lineOffset;
            double toplam = 0;
            decimal semsiyeAdet = 0;
            // Font seçimi
            System.Drawing.Font printFont = new System.Drawing.Font("Calibri", (float)9, FontStyle.Regular, GraphicsUnit.Point); // Substituted to FontA Font
            System.Drawing.Font tarihFont = new System.Drawing.Font("Calibri", (float)9, FontStyle.Regular, GraphicsUnit.Point);
            System.Drawing.Font ucretFont = new System.Drawing.Font("Calibri", (float)14, FontStyle.Regular, GraphicsUnit.Point);
            System.Drawing.Font aciklamaFont = new System.Drawing.Font("Calibri", (float)7, FontStyle.Regular, GraphicsUnit.Point);
            e.Graphics.PageUnit = GraphicsUnit.Point;

            // Fişi hazırla
            lineOffset = 7 + printFont.GetHeight(e.Graphics) - (float)3.5;
            x = 0;
            y = 0;
            e.Graphics.DrawString("______________________________________", printFont, Brushes.Black, x, y);
            y += lineOffset;
            semsiyeAdet = numericUpDownSemsiye.Value;
            e.Graphics.DrawString(semsiyeAdet + " Adet Şemsiye", printFont, Brushes.Black, Convert.ToInt32(58 - ((semsiyeAdet + " Adet Şemsiye").Length)), y);
            y += lineOffset;
            e.Graphics.DrawString(textBoxFirmaBaslik.Text, printFont, Brushes.Black, Convert.ToInt32(58 - (textBoxFirmaBaslik.Text).Length), y);
            y += lineOffset;
            e.Graphics.DrawString(DateTime.Now.ToString("dd-MM-yyyy HH:mm"), tarihFont, Brushes.Black, Convert.ToInt32(52 - (DateTime.Now.ToString("yyyy-MM-dd HH:mm").ToString().Length)), y);
            y += lineOffset;
            toplam = Convert.ToDouble(textBoxSemsiyeUcret.Text) * Convert.ToDouble(numericUpDownSemsiye.Value);
            e.Graphics.DrawString(toplam + " TL", ucretFont, Brushes.Black, Convert.ToInt32(58 - ((toplam + " TL").Length)), y);
            e.HasMorePages = false;     //yazdırma işlemi tamamlandı
        }

        private void textBoxAyarlarGirisUcreti_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ','))
            {
                e.Handled = true;
            }
        }

        private void operatorDoldur()
        {
            var operatorler = db.OperatorSet.OrderBy(i => i.Id).ToList();
            dataGridViewOperator.DataSource = operatorler;
        }

        private void buttonOperatorOlustur_Click(object sender, EventArgs e)
        {
            int kullaniciYetki = 1;
            if (checkBoxOperatorAdmin.Checked) { kullaniciYetki = 3; }
            Operator yeniOperator = null;
            yeniOperator = db.OperatorSet.FirstOrDefault(i => i.kullaniciAdi == textBoxOperatorKullaniciAdi.Text);
            if (yeniOperator != null) { MessageBox.Show("Bu kullanıcı adı ile kayırlı kullanıcı bulunuyor.Lütfen değiştirip tekrar deneyiniz.", "Kullanıcı Var"); return; }
            yeniOperator = new Operator()
            {
                kullaniciAdi = textBoxOperatorKullaniciAdi.Text,
                kullaniciSifre = textBoxOperatorSifre.Text,
                kullaniciYetki = kullaniciYetki,
                adSoyad = textBoxOperatorAdSoyad.Text
            };
            db.OperatorSet.Add(yeniOperator);
            db.SaveChanges();
            operatorDoldur();
        }

        private void pictureBoxyetkililer_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 3;
            operatorDoldur();
        }

        private void labelOperatorId_TextChanged(object sender, EventArgs e)
        {
            if (labelOperatorId.Text == "ID")
            {
                textBoxOperatorKullaniciAdi.Clear();
                textBoxOperatorSifre.Clear();
                checkBoxOperatorAdmin.Checked = false;
                textBoxOperatorAdSoyad.Clear();
            }
            else
            {
                int operatorId = Convert.ToInt32(labelOperatorId.Text);
                Operator seciliOperator = db.OperatorSet.FirstOrDefault(i => i.Id == operatorId);
                textBoxOperatorKullaniciAdi.Text = seciliOperator.kullaniciAdi;
                textBoxOperatorSifre.Text = seciliOperator.kullaniciSifre;
                textBoxOperatorAdSoyad.Text = seciliOperator.adSoyad;
                if (seciliOperator.kullaniciYetki == 3) { checkBoxOperatorAdmin.Checked = true; }
            }
        }

        private void buttonOperatorTemizle_Click(object sender, EventArgs e)
        {
            labelOperatorId.Text = "ID";
        }

        private void buttonOperatorGuncelle_Click(object sender, EventArgs e)
        {
            int kullaniciYetki = 1;
            if (checkBoxOperatorAdmin.Checked) { kullaniciYetki = 3; }
            int operatorId = Convert.ToInt32(labelOperatorId.Text);
            Operator guncellenecekOperator = db.OperatorSet.FirstOrDefault(i => i.Id == operatorId);
            DialogResult res = MessageBox.Show(guncellenecekOperator.kullaniciAdi + " kullanıcı adı ile kayıtlı operator bilgileri güncellenecektir.Onaylıyor Musunuz?", "Operator Guncelle", MessageBoxButtons.YesNo);
            if(res == DialogResult.Yes)
            {
                guncellenecekOperator.kullaniciAdi = textBoxOperatorKullaniciAdi.Text;
                guncellenecekOperator.kullaniciSifre = textBoxOperatorSifre.Text;
                guncellenecekOperator.kullaniciYetki = kullaniciYetki;
                guncellenecekOperator.adSoyad = textBoxOperatorAdSoyad.Text;
                db.SaveChanges();
                labelOperatorId.Text = "ID";
                operatorDoldur();
            }
        }

        private void dataGridViewOperator_SelectionChanged(object sender, EventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv != null && dgv.SelectedRows.Count > 0)
            {
                DataGridViewRow row = dgv.SelectedRows[0];
                if (row != null)
                {
                    labelOperatorId.Text = row.Cells[0].Value.ToString();
                    textBoxOperatorSil.Text = row.Cells[1].Value.ToString();
                }
            }
        }

        private void buttonYaziciAyarGuncelle_Click(object sender, EventArgs e)
        {
            Ayarlar ayar = new Ayarlar();
            ayar = db.AyarSet.FirstOrDefault();
            ayar.barkodBaslik = textBoxFirmaBaslik.Text;
            ayar.aciklamaIlkSatir = textBoxBarkodAciklamaIlkSatir.Text;
            ayar.aciklamaIkinciSatir = textBoxBarkodAciklamaIkinciSatir.Text;
            ayar.yaziciAdi = comboBoxYazici.Text;
            ayar.girisUcreti = textBoxAyarlarGirisUcreti.Text;
            ayar.sezlongUcret = textBoxSezlongUcret.Text;
            ayar.semsiyeUcret = textBoxSemsiyeUcret.Text;
            db.SaveChanges();

            xmlYaz();
        }

        private void pictureBoxayarlar_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 4;
            ayarDoldur();
            YaziciDoldur();
        }

        public void ayarDoldur()
        {
            Ayarlar ayar = new Ayarlar();
            ayar = db.AyarSet.FirstOrDefault();
            YaziciDoldur();
            if (ayar != null)
            {
                textBoxFirmaBaslik.Text = ayar.barkodBaslik;
                textBoxBarkodAciklamaIlkSatir.Text = ayar.aciklamaIlkSatir;
                textBoxBarkodAciklamaIkinciSatir.Text = ayar.aciklamaIkinciSatir;
                textBoxAyarlarGirisUcreti.Text = ayar.girisUcreti.ToString();
                textBoxSezlongUcret.Text = ayar.sezlongUcret.ToString();
                textBoxSemsiyeUcret.Text = ayar.semsiyeUcret.ToString();
                comboBoxYazici.Text = yaziciAdi;
                sezlongUcret = Convert.ToDecimal(ayar.sezlongUcret);
                semsiyeUcret = Convert.ToDecimal(ayar.semsiyeUcret);
            }
            else
            {
                ayar = new Ayarlar();
                ayar.barkodBaslik = "";
                ayar.aciklamaIlkSatir = "";
                ayar.aciklamaIkinciSatir = "";
                ayar.girisUcreti = "0";
                ayar.sezlongUcret = "0";
                ayar.semsiyeUcret = "0";
                db.AyarSet.Add(ayar);
                db.SaveChanges();
            }

            MailAyar mailAyar = new MailAyar();
            mailAyar = db.MailAyarSet.FirstOrDefault();
            if (mailAyar != null)
            {
                textBoxMailKullaniciAdi.Text = mailAyar.kullaniciAdi;
                textBoxMailSifre.Text = mailAyar.kullaniciSifre;
                textBoxMailGonderilecekAdres.Text = mailAyar.gonderilecekMail;
                textBoxMailSaat.Text = mailAyar.mailSaat.ToString();
            }
            else
            {
                mailAyar = new MailAyar();
                mailAyar.kullaniciAdi = "a@mail.com";
                mailAyar.kullaniciSifre = "sifre";
                mailAyar.gonderilecekMail = "gonderilecekadres@mail.com";
                mailAyar.mailSaat = TimeSpan.Parse("00:00:00");
                db.MailAyarSet.Add(mailAyar);
                db.SaveChanges();
            }
            mailAyarlari = mailAyar;
        }

        public void YaziciDoldur()
        {
            comboBoxYazici.Items.Clear();
            foreach (String yazici in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                comboBoxYazici.Items.Add(yazici);
            }
        }

        private void pictureBoxbarkod_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 0;
            istatistikGoster();
        }

        private void pictureBoxYeniBarkod_Click(object sender, EventArgs e)
        {
            try
            {
                var suankiBarkod = db.BarkodSet.FirstOrDefault();
                long siradakiBarkod = Convert.ToInt64(suankiBarkod.barkod) + 1;
                yazilacakBarkod = siradakiBarkod;
                DateTime yazdirilacakBarkodTarih = DateTime.Now;
                Ayarlar ayar = db.AyarSet.FirstOrDefault();
                Barkodlar barkod = new Barkodlar()
                {
                    barkod = siradakiBarkod.ToString(),
                };
                db.BarkodSet.Remove(suankiBarkod);
                db.BarkodSet.Add(barkod);               
                GirisBekleyen girisBekleyen = new GirisBekleyen()
                {
                    barkod = yazilacakBarkod.ToString(),
                    barkodTarih = yazdirilacakBarkodTarih
                };
                db.GirisBekleyenSet.Add(girisBekleyen);
                HareketKaydi yeniKayit = new HareketKaydi()
                {
                    barkod = yazilacakBarkod.ToString(),
                    barkodTarih = yazdirilacakBarkodTarih,
                    ucret = Convert.ToDecimal(ayar.girisUcreti),
                    operatorAdi = operatorAdi,
                    durum = 0,
                    aboneAdi = "Barkod",
                    aciklama = "Barkod Verildi"
                };
                db.HareketKaydiSet.Add(yeniKayit);
                db.SaveChanges();
                siradakiBarkoduVer();
                istatistikGoster();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void istatistikGoster()
        {
            try
            {
                DateTime bugun = DateTime.Now.Date;
                DateTime haftalik = bugun.Date.AddDays(-7);
                DateTime aylik = bugun.Date.AddDays(-DateTime.Now.Date.Day).AddHours(24);
                decimal hasilat = 0;
                int gunlukBiletliGiris = db.HareketKaydiSet.Where(i => i.barkod.Length == 12 && i.barkodTarih.Value > bugun).ToList().Count;
                int gunlukSezlong = db.HareketKaydiSet.Where(i => i.barkod == "Sezlong" && i.barkodTarih.Value > bugun).ToList().Sum(i => Convert.ToInt32(i.aciklama));
                int gunlukSemsiye = db.HareketKaydiSet.Where(i => i.barkod == "Semsiye" && i.barkodTarih.Value > bugun).ToList().Sum(i => Convert.ToInt32(i.aciklama));
                labelGunlukBiletliGiris.Text = gunlukBiletliGiris.ToString() + " KİŞİ";
                labelKayitlarGunlukBiletliGiris.Text = gunlukBiletliGiris.ToString() + " KİŞİ";
                labelGunlukSezlong.Text = gunlukSezlong.ToString() + " ADET";
                labelGunlukSemsiye.Text = gunlukSemsiye.ToString() + " ADET";
                labelGunlukSezlongHareket.Text = labelGunlukSezlong.Text;
                labelGunlukSemsiyeHareket.Text = labelGunlukSemsiye.Text;
                int haftalikBiletliGiris = db.HareketKaydiSet.Where(i => i.barkod.Length == 12 && i.barkodTarih.Value > haftalik).ToList().Count;
                int haftalikSezlong = db.HareketKaydiSet.Where(i => i.barkod == "Sezlong" && i.barkodTarih.Value > haftalik).ToList().Sum(i => Convert.ToInt32(i.aciklama));
                int haftalikSemsiye = db.HareketKaydiSet.Where(i => i.barkod == "Semsiye" && i.barkodTarih.Value > haftalik).ToList().Sum(i => Convert.ToInt32(i.aciklama));
                labelHaftalikSezlong.Text = haftalikSezlong.ToString() + " ADET";
                labelHaftalikSemsiye.Text = haftalikSemsiye.ToString() + " ADET";
                labelHaftalıkBiletliGiris.Text = haftalikBiletliGiris.ToString() + " KİŞİ";
                int aylikBiletliGiris = db.HareketKaydiSet.Where(i => i.barkod.Length == 12 && i.barkodTarih.Value > aylik).ToList().Count;
                int aylikSezlong = db.HareketKaydiSet.Where(i => i.barkod == "Sezlong" && i.barkodTarih.Value > aylik).ToList().Sum(i => Convert.ToInt32(i.aciklama));
                int aylikSemsiye = db.HareketKaydiSet.Where(i => i.barkod == "Semsiye" && i.barkodTarih.Value > aylik).ToList().Sum(i => Convert.ToInt32(i.aciklama));
                labelAylikSezlong.Text = aylikSezlong.ToString() + " ADET";
                labelAylikSemsiye.Text = aylikSemsiye.ToString() + " ADET";
                labelAylikBiletliGiris.Text = aylikBiletliGiris.ToString() + " KİŞİ";
                int gunlukAboneGiris = db.HareketKaydiSet.Where(i => i.barkod.Length == 10 && i.barkodTarih.Value > bugun).ToList().Count;
                labelGunlukAboneGiris.Text = gunlukAboneGiris.ToString() + " KİŞİ";
                labelKayitlarGunlukAboneGiris.Text = gunlukAboneGiris.ToString() + " KİŞİ";
                int haftalikAboneGiris = db.HareketKaydiSet.Where(i => i.barkod.Length == 10 && i.barkodTarih.Value > haftalik).ToList().Count;
                labelHaftalikAboneGiris.Text = haftalikAboneGiris.ToString() + " KİŞİ";
                int aylikAboneGiris = db.HareketKaydiSet.Where(i => i.barkod.Length == 10 && i.barkodTarih.Value > aylik).ToList().Count;
                labelAylikAboneGiris.Text = aylikAboneGiris.ToString() + " KİŞİ";
                labelGunlukToplamGiris.Text = (gunlukBiletliGiris + gunlukAboneGiris).ToString() + " KİŞİ";
                labelHaftalikToplamGiris.Text = (haftalikBiletliGiris + haftalikAboneGiris).ToString() + " KİŞİ";
                labelAylikToplamGiris.Text = (aylikBiletliGiris + aylikAboneGiris).ToString() + " KİŞİ";
                labelKayitlarGunlukToplamGiris.Text = (gunlukBiletliGiris + gunlukAboneGiris).ToString() + " KİŞİ";
                var gunlukHasilat = db.HareketKaydiSet.Where(i => i.barkodTarih.Value > bugun).Select(i => i.ucret).ToList();
                foreach (var item in gunlukHasilat)
                {
                    decimal d1 = item.HasValue ? item.Value : 0;
                    hasilat += d1;
                }
                labelKayitlarGunlukHasilat.Text = hasilat.ToString() + " TL";
                chartGunlukGrafik.Series["Girisler"].Points.Clear();
                chartGunlukGrafik.Series["Girisler"].IsValueShownAsLabel = true;
                chartGunlukGrafik.Series["Girisler"].Points.AddXY("Biletli", gunlukBiletliGiris);
                chartGunlukGrafik.Series["Girisler"].Points.AddXY("Abone", gunlukAboneGiris);
            }
            catch (Exception)
            {
            }
            
        }

        private void iceridekileriSil()
        {
            DateTime bugun = DateTime.Now;
            var list = db.GirisBekleyenSet.Where(i => i.barkodTarih < bugun.Date).ToList();
            if (list.Count != 0)
            {
                db.GirisBekleyenSet.RemoveRange(list);
                db.SaveChanges();
            }
            
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE[GirisBekleyens]");
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            labelsaat.Text = DateTime.Now.ToString("HH:mm");
            labeltarih.Text = DateTime.Now.ToString("MM-dd-yyyy");
            //if (DateTime.Now.ToString("HH:mm") == "20:00")
            //{
                iceridekileriSil();
            //}
            
            if(TimeSpan.Parse(labelsaat.Text) == mailAyarlari.mailSaat && pdfOlustu==false)
            {
                pdfOlustur();
            }
            if (TimeSpan.Parse(labelsaat.Text) == mailAyarlari.mailSaat.Add(TimeSpan.Parse("00:01")) && mailGonderilsin == "1" && mailYollandi == false)
            {
                gunlukMailGonder();
            }
            if (DateTime.Now.ToString("HH:mm") == "20:00" && veritabaniyedeklendi == 0)
            {
                veritabaniYedekle();
            }
        }

        private void pictureBoxKayitlarYazdir_Click(object sender, EventArgs e)
        {
            if (SetupThePrinting())
            {
                PrintPreviewDialog MyPrintPreviewDialog = new PrintPreviewDialog();
                MyPrintPreviewDialog.Document = printDocument1;
                MyPrintPreviewDialog.ShowDialog();
            }
        }

        private bool SetupThePrinting()
        {
            string baslik = "*" + dateTimePickerKayitlarFitreBaslangic.Text + " İLE " + dateTimePickerKayitlarFiltreBitis.Text + " ARASI HAREKET KAYITLARI";
            PrintDialog MyPrintDialog = new PrintDialog();
            MyPrintDialog.AllowCurrentPage = false;
            MyPrintDialog.AllowPrintToFile = false;
            MyPrintDialog.AllowSelection = true;
            MyPrintDialog.AllowSomePages = false;
            MyPrintDialog.PrintToFile = false;
            MyPrintDialog.ShowHelp = false;
            MyPrintDialog.ShowNetwork = false;
            MyPrintDialog.PrinterSettings.PrinterName = "Microsoft Print to PDF";

            //if (MyPrintDialog.ShowDialog() != DialogResult.OK)
            //    return false;

            printDocument1.DocumentName = "HAREKET KAYITLARI";
            printDocument1.PrinterSettings = MyPrintDialog.PrinterSettings;
            printDocument1.DefaultPageSettings = MyPrintDialog.PrinterSettings.DefaultPageSettings;
            printDocument1.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(40, 40, 80, 40);
            
            MyDataGridViewPrinter = new DataGridViewPrinter(dataGridViewKayitlar,
            printDocument1, true, true, "*" + dateTimePickerKayitlarFitreBaslangic.Text + " İLE " + dateTimePickerKayitlarFiltreBitis.Text + " ARASI HAREKET KAYITLARI", new System.Drawing.Font("Tahoma", 12,
            FontStyle.Bold, GraphicsUnit.Point), System.Drawing.Color.Black, true);           
            return true;
        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            bool more = MyDataGridViewPrinter.DrawDataGridView(e.Graphics);
            if (more == true)
                e.HasMorePages = true;
        }

        private void pictureBoxcikis_Click(object sender, EventArgs e)
        {
            Application.ExitThread();
            try
            {
                Application.Exit();
                Environment.Exit(1);
            }
            catch (Exception)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        private void pictureBoxAboneYazdir_Click(object sender, EventArgs e)
        {
            if (SetupAbonePrinting())
            {
                PrintPreviewDialog MyPrintPreviewDialog = new PrintPreviewDialog();
                MyPrintPreviewDialog.Document = printDocument1;
                MyPrintPreviewDialog.ShowDialog();
            }
        }
        private bool SetupAbonePrinting()
        {
            string baslik = "*ABONE KAYITLARI - Tarih :  " + DateTime.Now.ToString("dd-MM-yyyy");
            PrintDialog MyPrintDialog = new PrintDialog();
            MyPrintDialog.AllowCurrentPage = false;
            MyPrintDialog.AllowPrintToFile = false;
            MyPrintDialog.AllowSelection = true;
            MyPrintDialog.AllowSomePages = false;
            MyPrintDialog.PrintToFile = false;
            MyPrintDialog.ShowHelp = false;
            MyPrintDialog.ShowNetwork = false;
            MyPrintDialog.PrinterSettings.PrinterName = "Microsoft Print to PDF";

            //if (MyPrintDialog.ShowDialog() != DialogResult.OK)
            //    return false;

            printDocument2.DocumentName = "ABONE KAYITLARI";
            printDocument2.PrinterSettings = MyPrintDialog.PrinterSettings;
            printDocument2.DefaultPageSettings = MyPrintDialog.PrinterSettings.DefaultPageSettings;
            printDocument2.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(40, 40, 80, 40);

            MyDataGridViewPrinter = new DataGridViewPrinter(dataGridViewAbone,
            printDocument2, true, true, "*ABONE KAYITLARI - Tarih :  " + DateTime.Now.ToString("dd-MM-yyyy"), new System.Drawing.Font("Calibri", 16,
            FontStyle.Bold, GraphicsUnit.Point), System.Drawing.Color.Black, true);
            return true;
        }

        private void printDocument2_PrintPage(object sender, PrintPageEventArgs e)
        {
            bool more = MyDataGridViewPrinter.DrawDataGridView(e.Graphics);
            if (more == true)
                e.HasMorePages = true;
        }

        private void buttonOperatorSil_Click(object sender, EventArgs e)
        {
            var op = db.OperatorSet.FirstOrDefault(i => i.kullaniciAdi == textBoxOperatorSil.Text);
            if (op != null)
            {
                DialogResult res = MessageBox.Show(textBoxOperatorSil.Text + " kullanıcı adıyla kayıtlı kullanıcı silinecektir.Onaylıyor musunuz?", "Kullanıcı Silme", MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes)
                {
                    db.OperatorSet.Remove(op);
                    db.SaveChanges();
                }
            }
        }

        private void buttonMailAyarGuncelle_Click(object sender, EventArgs e)
        {
            if(!textBoxMailKullaniciAdi.Text.Contains("@") || !textBoxMailGonderilecekAdres.Text.Contains("@"))
            {
                MessageBox.Show("Lütfen kullanıcı adı ve gönderilecek mail kısımlarına geçerli bir mail adresi giriniz.Mail Adresleri @ içermelidir..", "Adres Yanlış");
                return;
            }
            try
            {
                TimeSpan.Parse(textBoxMailSaat.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Lütfen Geçerli Bir Saat Giriniz...", "Geçersiz Saat");
                return;
            }
            MailAyar mail = db.MailAyarSet.FirstOrDefault();
            if (mail != null)
            {
                db.MailAyarSet.Remove(mail);
                db.SaveChanges();
            }
            MailAyar mailAyar = new MailAyar()
            {
                kullaniciAdi = textBoxMailKullaniciAdi.Text,
                kullaniciSifre = textBoxMailSifre.Text,
                gonderilecekMail = textBoxMailGonderilecekAdres.Text,
                mailSaat = TimeSpan.Parse(textBoxMailSaat.Text)
            };
            db.MailAyarSet.Add(mailAyar);
            db.SaveChanges();
            ayarDoldur();
        }

        private void textBoxMailSaat_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ':'))
            {
                e.Handled = true;
            }
        }

        private void buttonMailSifre_Click(object sender, EventArgs e)
        {
            if(buttonMailSifre.Text == "Şifre Göster")
            {
                buttonMailSifre.Text = "Şifre Gizle";
                textBoxMailSifre.PasswordChar = '\0';
            }
            else if(buttonMailSifre.Text == "Şifre Gizle")
            {
                buttonMailSifre.Text = "Şifre Göster";
                textBoxMailSifre.PasswordChar = '*';
            }
        }

        private void gunlukMailGonder()
        {
            var hareketMailThread = new Thread(() =>
            {
                System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();
                try
                {
                    MailMessage ePosta = new MailMessage();

                    ePosta.From = new MailAddress(mailAyarlari.kullaniciAdi);
                    ePosta.To.Add(mailAyarlari.gonderilecekMail);

                    SmtpClient smtp = new SmtpClient();
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new System.Net.NetworkCredential(mailAyarlari.kullaniciAdi, mailAyarlari.kullaniciSifre);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.EnableSsl = true;
                    string[] mailUzanti = mailAyarlari.kullaniciAdi.Split('@');

                    if (mailUzanti[1] == "gmail.com")
                    {
                        smtp.Port = 587;
                        smtp.EnableSsl = true;
                        smtp.Host = "smtp.gmail.com";
                    }
                    else
                    {
                        smtp.Port = 587;
                        smtp.EnableSsl = true;
                        smtp.Timeout = 15000;
                        smtp.Host = "smtp-mail.outlook.com";
                    }

                    object userState = ePosta;

                    ePosta.Subject = "Günlük Giriş-Çıkış Hareketleri Bilgilendirmesi";

                    ePosta.Body = DateTime.Today.Date.ToString("dd-MM-yyyy") + " tarihindeki giriş hareketleri ile ilgili rapor aşağıdaki gibidir.\n\n";

                    ePosta.Attachments.Add(new Attachment("C:\\Kayitlar\\" + DateTime.Today.ToString("dd-MM-yyyy") + " HareketKayitlari" + ".pdf"));
                    ePosta.IsBodyHtml = true;

                    try
                    {
                        ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
                        smtp.Send(ePosta);
                    }
                    catch (SmtpException ex)
                    {
                        dosyayaYaz(ex.ToString());
                    }
                }
                catch (Exception ex) { dosyayaYaz(ex.ToString()); }
            });
            hareketMailThread.Start();
            mailYollandi = true;
        }

        public void dosyayaYaz(string log)
        {
            try
            {
                string dosya_yolu = @"hatalog.txt";
                FileStream fs = new FileStream(dosya_yolu, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(DateTime.Now);
                sw.WriteLine(log);
                sw.Flush();
                sw.Close();
                fs.Close();
            }
            catch (Exception) { }
        }

        private void pdfOlustur()
        {
            pdfOlustu = true;
            var pdfOlusturThread = new Thread(() =>
            {
                try
                {
                    DateTime baslangic = DateTime.Today.Date;
                    DateTime bitis = DateTime.Now;
                    decimal hasilat = 0;
                    var mailKayitList = db.HareketKaydiSet.Where(i => i.barkodTarih >= baslangic && i.barkodTarih <= bitis).
                        Select(i => new { i.barkod, i.barkodTarih, i.girisTarih, i.ucret, i.girisNoktasi, i.durum, i.aboneAdi, i.aciklama }).ToList();
                    dataGridViewMailHareket.DataSource = mailKayitList;
                    int girenAboneSayisi = db.HareketKaydiSet.Where(i => i.barkodTarih >= baslangic && i.barkodTarih <= bitis && i.barkod.Length == 10).Count();
                    int girenBiletliSayisi = db.HareketKaydiSet.Where(i => i.barkodTarih >= baslangic && i.barkodTarih <= bitis && i.barkod.Length == 12).Count();
                    int toplamGiris = girenAboneSayisi + girenBiletliSayisi;
                    var gunlukHasilat = db.HareketKaydiSet.Where(i => i.barkodTarih.Value > baslangic).Select(i => i.ucret).ToList();
                    foreach (var item in gunlukHasilat)
                    {
                        decimal d1 = item.HasValue ? item.Value : 0;
                        hasilat += d1;
                    }
                    labelToplamGiris.Text = toplamGiris.ToString();
                    labelAboneGiris.Text = girenAboneSayisi.ToString();
                    labelBiletliGiris.Text = girenBiletliSayisi.ToString();
                    labelHasilat.Text = hasilat.ToString();
                }
                catch (Exception ex) { dosyayaYaz(ex.ToString()); }
                SetupMailPrinting();
            });
            pdfOlusturThread.Start();
        }
        private void SetupMailPrinting()
        {
            
            string baslik = "*" + DateTime.Today.ToString("dd-MM-yyyy") + " GİRİŞ/ÇIKIŞ KAYITLARI" + "\n" + "\n" + label32.Text + " " + labelToplamGiris.Text + " KİŞİ" + "\n" + label35.Text + " " + labelAboneGiris.Text + " KİŞİ" + "\n" + label36.Text + " " + labelBiletliGiris.Text + " KİŞİ" + "\n" + label37.Text + " " + labelHasilat.Text + " TL" + "\n";

            //if (MyPrintDialog.ShowDialog() != DialogResult.OK)
            //    return false;

            printDocument3.DocumentName = "GİRİŞ/ÇIKIŞ KAYITLARI";
            printDocument3.PrinterSettings.PrinterName = "Microsoft Print to PDF";
            printDocument3.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(40, 40, 80, 40);
            IEnumerable<PaperSize> paperSizes = printDocument3.PrinterSettings.PaperSizes.Cast<PaperSize>();
            PaperSize sizeA4 = paperSizes.First<PaperSize>(size => size.Kind == PaperKind.A4);
            printDocument3.DefaultPageSettings.PaperSize = sizeA4;
            int a = dataGridViewMailHareket.Rows.Count;
            MyDataGridViewPrinter = new DataGridViewPrinter(dataGridViewMailHareket,
            printDocument3, true, true, baslik, new System.Drawing.Font("Tahoma", 12,
            FontStyle.Bold, GraphicsUnit.Point), System.Drawing.Color.Black, true);
            
            
            printDocument3.PrinterSettings.PrintToFile = true;
            if (!File.Exists("C:\\Kayitlar\\" + DateTime.Today.ToString("dd-MM-yyyy") + " HareketKayitlari" + ".pdf"))
            {
                printDocument3.PrinterSettings.PrintFileName = "C:\\Kayitlar\\" + DateTime.Today.ToString("dd-MM-yyyy") + " HareketKayitlari" + ".pdf";
                printDocument3.Print();
            }
        }

        private void printDocument3_PrintPage(object sender, PrintPageEventArgs e)
        {          
            bool more = MyDataGridViewPrinter.DrawDataGridView(e.Graphics);
            if (more == true)
                e.HasMorePages = true;
        }

        private void pictureBoxSezlongBilet_Click(object sender, EventArgs e)
        {
            if (numericUpDownSezlong.Value != 0)
            {
                try
                {
                    DateTime yazdirilacakBarkodTarih = DateTime.Now;
                    Ayarlar ayar = db.AyarSet.FirstOrDefault();
                    HareketKaydi yeniKayit = new HareketKaydi()
                    {
                        barkod = "Sezlong",
                        barkodTarih = yazdirilacakBarkodTarih,
                        girisTarih = yazdirilacakBarkodTarih,
                        ucret = Convert.ToDecimal(ayar.sezlongUcret) * numericUpDownSezlong.Value,
                        operatorAdi = operatorAdi,
                        durum = 0,
                        aboneAdi = "",
                        aciklama = numericUpDownSezlong.Value.ToString()
                    };
                    db.HareketKaydiSet.Add(yeniKayit);
                    db.SaveChanges();
                    sezlongBiletVer();
                    numericUpDownSezlong.Value = 0;
                    istatistikGoster();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                
            }
            else
            {
                MessageBox.Show("Lütfen Öncelikle Şezlong Adeti Seçiniz...", "Adet Seçilmedi..");
            }
        }

        private void pictureBoxSemsiyeBilet_Click(object sender, EventArgs e)
        {
            if (numericUpDownSemsiye.Value != 0)
            {
                try
                {
                    DateTime yazdirilacakBarkodTarih = DateTime.Now;
                    Ayarlar ayar = db.AyarSet.FirstOrDefault();
                    HareketKaydi yeniKayit = new HareketKaydi()
                    {
                        barkod = "Semsiye",
                        barkodTarih = yazdirilacakBarkodTarih,
                        girisTarih = yazdirilacakBarkodTarih,
                        ucret = Convert.ToDecimal(ayar.semsiyeUcret) * numericUpDownSemsiye.Value,
                        operatorAdi = operatorAdi,
                        durum = 0,
                        aboneAdi = "",
                        aciklama = numericUpDownSemsiye.Value.ToString()
                    };
                    db.HareketKaydiSet.Add(yeniKayit);
                    db.SaveChanges();
                    semsiyeBiletVer();
                    numericUpDownSemsiye.Value = 0;
                    istatistikGoster();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else
            {
                MessageBox.Show("Lütfen Öncelikle Şemsiye Adeti Seçiniz...", "Adet Seçilmedi..");
            }
        }

        private void textBoxSezlongUcret_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ','))
            {
                e.Handled = true;
            }
        }

        private void textBoxSemsiyeUcret_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ','))
            {
                e.Handled = true;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //this.lstDemos.SelectedIndex = 0;
            //this.cboDpi.SelectedIndex = 0;
        }

        private void pictureBoxOtoparkOdeme_Click(object sender, EventArgs e)
        {

        }

        private void buttonSorgula_Click(object sender, EventArgs e)
        {
            if (buttonSorgula.Text == "Sorgula")
            {
                
                textBoxplaka.Enabled = false;
                buttonSorgula.Text = "YeniSorgu";
                DateTime girisZaman = new DateTime();
                TimeSpan ts = new TimeSpan();
                using (var conn = new SqlConnection(ptsBaglanti))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand("SELECT Id, Barkod, Plaka, GirisNoktasi, Giris, Cikis, Ucret, OdemeTuru," +
                        " KullaniciAdi, Durum, IslemTuru, Adsoyad, Aciklama FROM Kayits WHERE Plaka='" + textBoxplaka.Text + "'", conn);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        girisZaman = Convert.ToDateTime(reader["Giris"]);
                        labelGirisZamani.Text = girisZaman.ToString();
                        labelCikisZamani.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    }
                    if (girisZaman != null && girisZaman.Date>DateTime.Now.Date.AddDays(-30))
                    {
                        ts = DateTime.Now - girisZaman;
                        labelGecenSure.Text = ts.Days.ToString() + " Gün " + ts.Hours.ToString() + " Saat " + ts.Minutes.ToString() + " Dakika";
                        labelUcret.Text = tarifehesabiyap(Convert.ToInt32(ts.TotalMinutes)).ToString();
                        buttonOdemeYap.Enabled = true;
                    }
                    
                }
            }else if (buttonSorgula.Text == "YeniSorgu")
            {
                buttonSorgula.Text = "Sorgula";
                textBoxplaka.Clear();
                textBoxplaka.Enabled = true;
                buttonOdemeYap.Enabled = false;
                labelGirisZamani.Text = "";
                labelCikisZamani.Text = "";
                labelGecenSure.Text = "";
                labelUcret.Text = "";
            }
            

        }
        private decimal tarifehesabiyap(int gecensure)
        {
            int saat = gecensure;
            decimal sonuc = 0;
            List<Tarife> ucretler = new List<Tarife>();
            using (var conn = new SqlConnection(ptsBaglanti))
            {
                conn.Open();
                SqlCommand command = new SqlCommand("Select Id,saat,ucret FROM Tarifes Order BY saat", conn);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Tarife tarife = new Tarife();
                    tarife.saat = Convert.ToInt32(reader["saat"].ToString());
                    tarife.ucret = Convert.ToDecimal(reader["ucret"].ToString());
                    ucretler.Add(tarife);
                }              
            }
            foreach (var tarife in ucretler)
            {
                if (saat < tarife.saat) { sonuc = tarife.ucret; break; } else { continue; }
            }
            //if (sonuc == 0) { sonuc = saat * saatlikucret; }
            return sonuc;
        }

        private void buttonOdemeYap_Click(object sender, EventArgs e)
        {
            string barkod = "";
            try
            {
                using (var conn = new SqlConnection(ptsBaglanti))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand("SELECT Barkod, Plaka FROM Otoparks WHERE Plaka='" + textBoxplaka.Text + "'", conn);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        barkod = reader["Barkod"].ToString();
                    }
                }
                if (barkod == "") { MessageBox.Show("Ödeme Başarısız...", "Başarısız"); return; }
                using (SqlConnection conn = new SqlConnection(ptsBaglanti))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand("UPDATE Kayits SET Cikis='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "'," + " Ucret='" + Convert.ToDouble(labelUcret.Text.Replace(".", ",")) + "'," + " OdemeTuru='" + "Nakit" + "'," + " KullaniciAdi='" + "Turnike" + "'," + " Durum='" + 1 + "' WHERE Barkod='" + barkod + "'", conn);
                    command.ExecuteNonQuery();
                }
                using (SqlConnection conn = new SqlConnection(ptsBaglanti))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand("UPDATE Otoparks SET Cikis='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "'," + " Ucret='" + Convert.ToDouble(labelUcret.Text.Replace(".",",")) + "'," + " OdemeTuru='" + "Nakit" + "'," + " KullaniciAdi='" + "Turnike" + "'," + " Durum='" + 1 + "' WHERE Barkod='" + barkod + "'", conn);
                    command.ExecuteNonQuery();
                }
                textBoxplaka.Clear();
                textBoxplaka.Enabled = true;
                buttonSorgula.Text = "Sorgula";
                labelGirisZamani.Text = "";
                labelCikisZamani.Text = "";
                labelGecenSure.Text = "";
                buttonOdemeYap.Enabled = false;
                labelUcret.Text = "";
                MessageBox.Show("Ödeme Başarılı...", "Ödeme Onay");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            

        }

        private void pictureBoxAbonelikYenile_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 2;
            aboneleriGetir();
        }

        private void sezlongSemsiyeAdetYaz()
        {
            List<HareketKaydi> hareketKaydi = null;
            hareketKaydi = db.HareketKaydiSet.ToList();
            foreach (var item in hareketKaydi)
            {
                if (item.barkod == "Sezlong")
                {
                    item.aciklama = (item.ucret / sezlongUcret).ToString();
                }
                else if (item.barkod == "Semsiye")
                {
                    item.aciklama = (item.ucret / semsiyeUcret).ToString();
                }
                db.SaveChanges();
            }
            MessageBox.Show("Bitti");
        }


        public void veritabaniYedekle()
        {
            if (!Directory.Exists("C:\\VeritabaniYedekler\\PlajKontrolBackUps"))
            {
                Directory.CreateDirectory("C:\\VeritabaniYedekler\\PlajKontrolBackUps");
            }
            if (!File.Exists("C:\\VeritabaniYedekler\\PlajKontrolBackUps\\PlajBackUp" + DateTime.Now.Date.ToString("yyyy-MM-dd") + ".bak"))
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection("Data Source=.; User Id=sa; Password=Recep123"))
                    {
                        conn.Open();
                        SqlCommand command = new SqlCommand("BACKUP DATABASE [PlajKontrol] TO DISK = N'C:\\VeritabaniYedekler\\PlajKontrolBackUps\\PlajBackUp" + DateTime.Now.Date.ToString("yyyy-MM-dd") + ".bak' WITH INIT", conn);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception)
                {
                }
            }
            try
            {
                DirectoryInfo d = new DirectoryInfo(@"C:\\VeritabaniYedekler\\PlajKontrolBackUps");//Assuming Test is your Folder
                FileInfo[] Files = d.GetFiles("*.bak"); //Getting Text files
                foreach (FileInfo file in Files)
                {
                    if (file.CreationTime < DateTime.Now.AddDays(-7))
                    {
                        file.Delete();
                    }
                }
            }
            catch (Exception) { }
            veritabaniyedeklendi = 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            sezlongSemsiyeAdetYaz();
        }
    }
}
