using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FaturaBaskiUygulamasi
{
    public partial class FrmAnaSayfa : Form
    {
        private const string MESAJ_BASLIK_UYARI = "Uyarı";
        private const string MESAJ_BASLIK_HATA = "Hata";
        private const string MESAJ_BASLIK_BILGI = "Bilgi";

        private static new readonly Font DefaultFont = new Font("Roboto", 10, FontStyle.Bold);
        private static readonly Font SmallFont = new Font("Roboto", 8);

        private string secilenSablonYolu = "";
        private Dictionary<string, Point> konumlar = new Dictionary<string, Point>();
        private Image sablonResmi;
        private PictureBox pbSablonOnizleme;
        private bool notModu = false; // Form'un üst kısmına ekleyin

        // Fatura alanları için kullanılacak TextBox'lar
        private Dictionary<string, TextBox> faturaAlanlari = new Dictionary<string, TextBox>();

        // Ürün bilgileri için kullanılacak liste
        private List<UrunBilgisi> urunListesi = new List<UrunBilgisi>();
        private const int MAKSIMUM_URUN_SAYISI = 5;


        // Fatura alanlarının isimleri - Ürün bilgileri çıkarıldı, bunlar ayrı yönetilecek
        private readonly string[] alanIsimleri = new string[]
        {
            "FaturaNo", "DuzenlemeTarihi", "AliciFirmaIsim", "AliciBilgileri",
            "AraToplam", "VergiTutari", "ToplamFiyat"
        };

        // Ürün alanlarının isimleri
        private readonly string[] urunAlanIsimleri = new string[]
        {
            "UrunAciklamalari", "UrunMiktar", "UrunKDV", "UrunBirimFiyati", "UrunToplam"
        };

        public FrmAnaSayfa()
        {
            InitializeComponent();

            // Form başlangıç boyutu ve özelliklerini ayarla
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
            this.Size = new Size(1200, 800); // Varsayılan boyut
            this.MinimumSize = new Size(1000, 600); // Minimum boyut

            // Form boyutlandırma modunu değiştir
            this.FormBorderStyle = FormBorderStyle.Sizable;
        }

        private void FrmAnaSayfa_Load(object sender, EventArgs e)
        {
            // Panel'leri oluştur
            Panel pnlKontroller = OlusturKontrolPaneli();
            Panel pnlOnizleme = OlusturOnizlemePaneli();
            this.Controls.Add(pnlOnizleme);
            this.Controls.Add(pnlKontroller);

            // Konum yönetimi panelini oluştur
            KonumYonetimiPaneliOlustur();
        }

        private Panel OlusturKontrolPaneli()
        {
            // Sol panel - kontroller için
            Panel pnlKontroller = new Panel
            {
                Name = "pnlKontroller",
                Dock = DockStyle.Left,
                Width = 300,
                MinimumSize = new Size(300, 0),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Padding = new Padding(5)
            };

            int yPosition = 10;

            // Şablon seçimi
            Button btnSablonSec = new Button
            {
                Text = "Fatura Şablonu Seç",
                Location = new Point(10, yPosition),
                Size = new Size(280, 30)
            };
            btnSablonSec.Click += BtnSablonSec_Click;
            pnlKontroller.Controls.Add(btnSablonSec);
            yPosition += 40;

            // Yazdırma seçenekleri
            GroupBox grpSablonSecenek = new GroupBox
            {
                Text = "Yazdırma Seçenekleri",
                Location = new Point(10, yPosition),
                Size = new Size(280, 70)
            };
            pnlKontroller.Controls.Add(grpSablonSecenek);

            RadioButton rdbSablonGoster = new RadioButton
            {
                Name = "rdbSablonGoster",
                Text = "Şablonla Yazdır",
                Location = new Point(10, 20),
                Size = new Size(120, 20),
                Checked = true
            };
            grpSablonSecenek.Controls.Add(rdbSablonGoster);

            RadioButton rdbSablonGosterme = new RadioButton
            {
                Name = "rdbSablonGosterme",
                Text = "Şablonsuz Yazdır",
                Location = new Point(140, 20),
                Size = new Size(120, 20)
            };
            grpSablonSecenek.Controls.Add(rdbSablonGosterme);

            rdbSablonGoster.CheckedChanged += (s, ev) =>
            {
                yazdirmadaSablonGoster = rdbSablonGoster.Checked;
                if (sablonResmi != null)
                {
                    if (notModu && pnlNot != null)
                    {
                        var txtNot = pnlNot.Controls.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtNot");
                        if (txtNot != null && konumlar.ContainsKey("Not"))
                        {
                            NotOnizle(txtNot);
                        }
                    }
                    else
                    {
                        FaturaOnizle();
                    }
                }
            };

            yPosition += 80;

            // Fatura alanları
            foreach (string alanAdi in alanIsimleri)
            {
                // Alan başlığı
                Label lbl = new Label
                {
                    Text = AlanAdiFormatla(alanAdi) + ":",
                    Location = new Point(10, yPosition),
                    Size = new Size(150, 20),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                pnlKontroller.Controls.Add(lbl);

                // TextBox
                TextBox txt = new TextBox
                {
                    Name = "txt" + alanAdi,
                    Location = new Point(10, yPosition + 25),
                    Size = new Size(180, alanAdi == "AliciBilgileri" ? 80 : 20)
                };

                if (alanAdi == "AliciBilgileri")
                {
                    txt.Multiline = true;
                    txt.ScrollBars = ScrollBars.Vertical;
                    txt.Height = 80;
                }
                else if (alanAdi == "FaturaNo")
                {
                    txt.Text = "001";
                }
                else if (alanAdi == "DuzenlemeTarihi")
                {
                    txt.Text = DateTime.Now.ToString("dd.MM.yyyy");
                }

                pnlKontroller.Controls.Add(txt);
                faturaAlanlari.Add(alanAdi, txt);

                // Konum seçme butonu
                Button btnKonumSec = new Button
                {
                    Text = "Konum Seç",
                    Name = "btnKonum" + alanAdi,
                    Location = new Point(200, yPosition + 25),
                    Size = new Size(90, 23),
                    Tag = alanAdi
                };
                btnKonumSec.Click += BtnKonumSec_Click;
                pnlKontroller.Controls.Add(btnKonumSec);

                yPosition += (alanAdi == "AliciBilgileri" ? 100 : 60);
            }

            // Ürün işlemleri bölümü
            yPosition = OlusturUrunIslemleriAlani(pnlKontroller, yPosition);

            return pnlKontroller;
        }

        private int OlusturUrunIslemleriAlani(Panel pnlKontroller, int yPosition)
        {
            // Ürünler başlığı
            Label lblUrunler = new Label
            {
                Text = "ÜRÜN BİLGİLERİ:",
                Location = new Point(10, yPosition),
                Size = new Size(280, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            pnlKontroller.Controls.Add(lblUrunler);
            yPosition += 30;

            // Ürün konumu seçme
            Button btnUrunKonumSec = new Button
            {
                Text = "İlk Ürün Konumunu Seç",
                Location = new Point(10, yPosition),
                Size = new Size(280, 30),
                Tag = "UrunSatiri"
            };
            btnUrunKonumSec.Click += BtnKonumSec_Click;
            pnlKontroller.Controls.Add(btnUrunKonumSec);
            yPosition += 40;

            // Ürün ekleme butonu
            Button btnUrunEkle = new Button
            {
                Text = "Ürün Ekle",
                Location = new Point(10, yPosition),
                Size = new Size(280, 30)
            };
            btnUrunEkle.Click += BtnUrunEkle_Click;
            pnlKontroller.Controls.Add(btnUrunEkle);
            yPosition += 40;

            // Ürün listesi paneli
            Panel pnlUrunListesi = new Panel
            {
                Name = "pnlUrunListesi",
                Location = new Point(10, yPosition),
                Size = new Size(280, 200),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Padding = new Padding(5)
            };
            pnlKontroller.Controls.Add(pnlUrunListesi);
            yPosition += 210;

            // Yazdırma ve önizleme butonları
            Button btnYazdir = new Button
            {
                Text = "Faturayı Yazdır",
                Location = new Point(10, yPosition),
                Size = new Size(280, 30)
            };
            btnYazdir.Click += BtnYazdir_Click;
            pnlKontroller.Controls.Add(btnYazdir);

            Button btnOnizle = new Button
            {
                Text = "Faturayı Önizle",
                Location = new Point(10, yPosition + 40),
                Size = new Size(280, 30)
            };
            pnlKontroller.Controls.Add(btnOnizle);

            return yPosition + 80;
        }

        private Panel OlusturOnizlemePaneli()
        {
            // Ana önizleme paneli
            Panel pnlOnizleme = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightGray
            };

            // PictureBox container panel - önizleme için ara konteyner
            Panel pnlPictureBoxContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.LightGray
            };

            // PictureBox ayarları
            pbSablonOnizleme = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(400, 10)
            };

            pnlPictureBoxContainer.Controls.Add(pbSablonOnizleme);
            pnlOnizleme.Controls.Add(pnlPictureBoxContainer);

            return pnlOnizleme;
        }

        private void BtnUrunEkle_Click(object sender, EventArgs e)
        {
            if (urunListesi.Count >= MAKSIMUM_URUN_SAYISI)
            {
                MessageBox.Show($"En fazla {MAKSIMUM_URUN_SAYISI} ürün ekleyebilirsiniz!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!konumlar.ContainsKey("UrunSatiri"))
            {
                MessageBox.Show("Önce ilk ürün konumunu seçmelisiniz!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Yeni ürün için form oluştur
            Form frmUrunEkle = new Form
            {
                Text = "Ürün Bilgileri",
                Size = new Size(400, 300),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            int yPos = 20;
            Dictionary<string, TextBox> urunKontrolleri = new Dictionary<string, TextBox>();

            foreach (string alan in urunAlanIsimleri)
            {
                Label lbl = new Label
                {
                    Text = AlanAdiFormatla(alan) + ":",
                    Location = new Point(20, yPos),
                    Size = new Size(150, 20)
                };
                frmUrunEkle.Controls.Add(lbl);

                TextBox txt = new TextBox
                {
                    Name = "txt" + alan,
                    Location = new Point(20, yPos + 25),
                    Size = new Size(350, 20)
                };

                // Sayısal alanlar için olay ekle
                if (alan == "UrunMiktar" || alan == "UrunKDV" || alan == "UrunBirimFiyati")
                {
                    txt.KeyPress += NumericTextBox_KeyPress;
                    txt.TextChanged += (s, ev) => HesaplaUrunToplam(urunKontrolleri);
                }

                if (alan == "UrunToplam")
                {
                    txt.ReadOnly = true;
                }

                frmUrunEkle.Controls.Add(txt);
                urunKontrolleri.Add(alan, txt);
                yPos += 50;
            }

            Button btnEkle = new Button
            {
                Text = "Ekle",
                Location = new Point(20, yPos),
                Size = new Size(350, 30),
                DialogResult = DialogResult.OK
            };
            frmUrunEkle.Controls.Add(btnEkle);
            frmUrunEkle.AcceptButton = btnEkle;

            if (frmUrunEkle.ShowDialog() == DialogResult.OK)
            {
                UrunBilgisi yeniUrun = new UrunBilgisi
                {
                    Aciklama = urunKontrolleri["UrunAciklamalari"].Text,
                    Miktar = ParseDecimal(urunKontrolleri["UrunMiktar"].Text),
                    KDV = ParseDecimal(urunKontrolleri["UrunKDV"].Text),
                    BirimFiyat = ParseDecimal(urunKontrolleri["UrunBirimFiyati"].Text)
                };

                urunListesi.Add(yeniUrun);

                // Ürün listesi panelini bul ve görünür yap
                var pnlKontroller = Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlKontroller");
                var pnlUrunListesi = pnlKontroller?.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlUrunListesi");
                if (pnlUrunListesi != null)
                {
                    pnlUrunListesi.Visible = true;
                }

                YenileUrunListesi();
                HesaplamalariGuncelle();
            }
        }

        private void HesaplaUrunToplam(Dictionary<string, TextBox> urunKontrolleri)
        {
            try
            {
                decimal miktar = ParseDecimal(urunKontrolleri["UrunMiktar"].Text);
                decimal birimFiyat = ParseDecimal(urunKontrolleri["UrunBirimFiyati"].Text);
                decimal kdvOrani = ParseDecimal(urunKontrolleri["UrunKDV"].Text);

                decimal toplamFiyat = miktar * birimFiyat;
                decimal kdvTutari = toplamFiyat * (kdvOrani / 100);
                decimal kdvDahilToplam = toplamFiyat + kdvTutari;

                urunKontrolleri["UrunToplam"].Text = FormatMoney(toplamFiyat); // KDV hariç toplam
                // Alternatif olarak KDV dahil toplam göstermek isterseniz:
                // urunKontrolleri["UrunToplam"].Text = FormatMoney(kdvDahilToplam);
            }
            catch
            {
                urunKontrolleri["UrunToplam"].Text = "0,00 TL";
            }
        }

        private void NumericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Sadece sayıların, virgül ve silme tuşunun girilmesine izin ver
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.' && e.KeyChar != (char)Keys.Back)
            {
                e.Handled = true;
            }

            // Nokta girişini virgüle çevir (Türkçe format için)
            if (e.KeyChar == '.')
            {
                e.KeyChar = ',';
            }

            // Bir textbox'ta sadece bir virgül olabilir
            TextBox txt = (TextBox)sender;
            if (e.KeyChar == ',' && txt.Text.Contains(","))
            {
                e.Handled = true;
            }
        }

        private decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            // TL ve diğer karakterleri temizle
            string cleanValue = value.Replace("TL", "").Replace(" ", "").Trim();

            if (decimal.TryParse(cleanValue, out decimal result))
                return result;

            return 0;
        }

        private string FormatMoney(decimal value)
        {
            return string.Format("{0:N2} TL", value);
        }

        private void YenileUrunListesi()
        {
            // Ürün listesi panelini pnlKontroller içinden bul
            var pnlKontroller = Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlKontroller");
            var pnlUrunListesi = pnlKontroller?.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlUrunListesi");

            if (pnlUrunListesi == null) return;

            pnlUrunListesi.Controls.Clear();
            pnlUrunListesi.AutoScroll = true;
            pnlUrunListesi.BorderStyle = BorderStyle.FixedSingle;
            pnlUrunListesi.BackColor = Color.White;

            int yPos = 10;

            for (int i = 0; i < urunListesi.Count; i++)
            {
                var urun = urunListesi[i];
                int currentIndex = i;

                // Ürün bilgisi etiketi
                Label lblUrun = new Label
                {
                    Text = $"{i + 1}. {urun.Aciklama}\n" +
                          $"    {urun.Miktar} Adet x {FormatMoney(urun.BirimFiyat)} = {FormatMoney(urun.ToplamFiyat)}\n" +
                          $"    KDV: %{urun.KDV} ({FormatMoney(urun.KDVTutari)})\n" +
                          $"    Toplam: {FormatMoney(urun.KDVDahilToplamFiyat)}",
                    Location = new Point(5, yPos),
                    Size = new Size(240, 60),
                    AutoSize = false,
                    BackColor = Color.WhiteSmoke
                };
                pnlUrunListesi.Controls.Add(lblUrun);

                // Silme düğmesi
                Button btnSil = new Button
                {
                    Text = "X",
                    Size = new Size(25, 25),
                    Location = new Point(250, yPos + 15),
                    Tag = currentIndex,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Red,
                    ForeColor = Color.White
                };

                btnSil.Click += (s, e) =>
                {
                    if (MessageBox.Show("Bu ürünü silmek istediğinize emin misiniz?", "Ürün Sil",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        urunListesi.RemoveAt(currentIndex);
                        YenileUrunListesi();
                        HesaplamalariGuncelle();
                    }
                };

                pnlUrunListesi.Controls.Add(btnSil);
                yPos += 70;
            }

            pnlUrunListesi.Visible = true;
        }

        private void HesaplamalariGuncelle()
        {
            decimal araToplam = urunListesi.Sum(u => u.ToplamFiyat);
            decimal toplamKDVTutari = urunListesi.Sum(u => u.KDVTutari);
            decimal genelToplam = araToplam + toplamKDVTutari;

            faturaAlanlari["AraToplam"].Text = FormatMoney(araToplam);
            faturaAlanlari["VergiTutari"].Text = FormatMoney(toplamKDVTutari);
            faturaAlanlari["ToplamFiyat"].Text = FormatMoney(genelToplam);

            FaturaOnizle();
        }

        private void BtnSablonSec_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Şablon Seçin"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                secilenSablonYolu = ofd.FileName;
                SablonYukle(secilenSablonYolu);

                // Şablon tipi seçim formunu göster
                SablonTipiSec();
            }
        }

        private void SablonTipiSec()
        {
            Form frmTipSec = new Form
            {
                Text = "Şablon Tipi Seçin",
                Size = new Size(300, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            RadioButton rdbFatura = new RadioButton
            {
                Text = "Fatura",
                Location = new Point(20, 20),
                Checked = true,
                Size = new Size(100, 20)
            };
            frmTipSec.Controls.Add(rdbFatura);

            RadioButton rdbNot = new RadioButton
            {
                Text = "Not",
                Location = new Point(150, 20),
                Size = new Size(100, 20)
            };
            frmTipSec.Controls.Add(rdbNot);

            Button btnOnayla = new Button
            {
                Text = "Tamam",
                DialogResult = DialogResult.OK,
                Location = new Point(100, 60),
                Size = new Size(100, 30)
            };
            frmTipSec.Controls.Add(btnOnayla);

            if (frmTipSec.ShowDialog() == DialogResult.OK)
            {
                // Önce mevcut panelleri temizle
                var pnlNot = Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlNot");
                var pnlKontroller = Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlKontroller");

                if (pnlNot != null)
                {
                    Controls.Remove(pnlNot);
                    pnlNot.Dispose();
                }

                if (rdbFatura.Checked)
                {
                    notModu = false;

                    // Fatura modunu etkinleştir
                    if (pnlKontroller != null)
                    {
                        pnlKontroller.Visible = true;
                    }
                    else
                    {
                        // Eğer panel yoksa form load'daki kodları yeniden çalıştır
                        FrmAnaSayfa_Load(this, EventArgs.Empty);
                    }
                }
                else
                {
                    notModu = true;

                    // Not modunu etkinleştir
                    if (pnlKontroller != null)
                    {
                        pnlKontroller.Visible = false;
                    }

                    // Not giriş panelini oluştur
                    NotPaneliOlustur();
                }

                // Şablonu göster
                if (sablonResmi != null && pbSablonOnizleme != null)
                {
                    pbSablonOnizleme.Image = sablonResmi;
                }
            }
        }

        Panel pnlNot;
        private void NotPaneliOlustur()
        {
            // Varsa eski not panelini kaldır
            var eskiPanel = Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlNot");
            if (eskiPanel != null)
                Controls.Remove(eskiPanel);

            Panel pnlNot = new Panel
            {
                Name = "pnlNot",
                Dock = DockStyle.Left,
                Width = 300,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            int yPos = 10;

            // Şablon seçim düğmesi
            Button btnYeniSablonSec = new Button
            {
                Text = "Yeni Şablon Seç",
                Location = new Point(10, yPos),
                Size = new Size(280, 30)
            };
            btnYeniSablonSec.Click += BtnSablonSec_Click;
            pnlNot.Controls.Add(btnYeniSablonSec);
            yPos += 40;

            // Not girişi için TextBox
            Label lblNot = new Label
            {
                Text = "Not:",
                Location = new Point(10, yPos),
                Size = new Size(280, 20)
            };
            pnlNot.Controls.Add(lblNot);
            yPos += 25;

            TextBox txtNot = new TextBox
            {
                Name = "txtNot",
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, yPos),
                Size = new Size(280, 200)
            };
            pnlNot.Controls.Add(txtNot);
            yPos += 210;

            // Not konumu seçme düğmesi
            Button btnNotKonumuSec = new Button
            {
                Text = "Not Konumu Seç",
                Location = new Point(10, yPos),
                Size = new Size(280, 30)
            };
            btnNotKonumuSec.Click += (s, e) =>
            {
                aktifAlanAdi = "Not";
                pbSablonOnizleme.Click += PbSablonOnizleme_Click;
            };
            pnlNot.Controls.Add(btnNotKonumuSec);
            yPos += 40;

            // Not önizleme düğmesi
            Button btnNotOnizle = new Button
            {
                Text = "Önizle",
                Location = new Point(10, yPos),
                Size = new Size(280, 30)
            };
            btnNotOnizle.Click += (s, e) => NotOnizle(txtNot);
            pnlNot.Controls.Add(btnNotOnizle);
            yPos += 40;

            // Yazdırma seçenekleri grup kutusu
            GroupBox grpSablonSecenek = new GroupBox
            {
                Text = "Yazdırma Seçenekleri",
                Location = new Point(10, yPos),
                Size = new Size(280, 70)
            };
            pnlNot.Controls.Add(grpSablonSecenek);

            RadioButton rdbSablonGoster = new RadioButton
            {
                Text = "Şablonla Yazdır",
                Location = new Point(10, 20),
                Size = new Size(120, 20),
                Checked = true
            };
            grpSablonSecenek.Controls.Add(rdbSablonGoster);

            RadioButton rdbSablonGosterme = new RadioButton
            {
                Text = "Şablonsuz Yazdır",
                Location = new Point(140, 20),
                Size = new Size(120, 20)
            };
            grpSablonSecenek.Controls.Add(rdbSablonGosterme);

            rdbSablonGoster.CheckedChanged += (s, ev) =>
            {
                yazdirmadaSablonGoster = rdbSablonGoster.Checked;
                if (sablonResmi != null)
                {
                    if (notModu && pnlNot != null)
                    {
                        if (pnlNot.Controls.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtNot") != null && konumlar.ContainsKey("Not"))
                        {
                            NotOnizle(pnlNot.Controls.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtNot"));
                        }
                    }
                    else
                    {
                        // Fatura modunda otomatik önizleme yap
                        FaturaOnizle();
                    }
                }
            };
            yPos += 80;

            // Yazdırma düğmesi
            Button btnYazdir = new Button
            {
                Text = "Yazdır",
                Location = new Point(10, yPos),
                Size = new Size(280, 30)
            };
            btnYazdir.Click += BtnYazdir_Click;
            pnlNot.Controls.Add(btnYazdir);
            yPos += 40;

            Controls.Add(pnlNot);
        }

        // Konum seçme sorununu düzeltmek için değişiklikler
        private string aktifAlanAdi = null; // Aktif konum seçimi yapılan alan

        private void BtnKonumSec_Click(object sender, EventArgs e)
        {
            if (sablonResmi == null)
            {
                MessageBox.Show("Önce bir fatura şablonu seçmelisiniz!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Button btn = sender as Button;
            string alanAdi = btn.Tag.ToString();
            aktifAlanAdi = alanAdi; // Seçilen alanı aktif alan olarak ata

            // Tüm önceki olay işleyicilerini kaldır ve yeni olay işleyicisini ekle
            pbSablonOnizleme.Click -= PbSablonOnizleme_Click;
            pbSablonOnizleme.Click += PbSablonOnizleme_Click;
        }

        private void PbSablonOnizleme_Click(object sender, EventArgs e)
        {
            if (aktifAlanAdi == null) return;

            MouseEventArgs me = e as MouseEventArgs;
            // Tıklanan konumu gerçek koordinatlara çevir
            Point gercekKonum = GercekKonum(me.Location);

            // Konumu kaydet
            if (konumlar.ContainsKey(aktifAlanAdi))
            {
                konumlar[aktifAlanAdi] = gercekKonum;
            }
            else
            {
                konumlar.Add(aktifAlanAdi, gercekKonum);
            }

            // Önizleme için şablonun bir kopyasını oluştur
            Image tempImage = new Bitmap(sablonResmi);

            using (Graphics g = Graphics.FromImage(tempImage))
            {
                foreach (var item in konumlar)
                {
                    // Konumu ölçeklendir
                    Point olcekliKonum = OlcekliKonum(item.Value);

                    // Noktayı ve etiketi çiz
                    g.FillEllipse(Brushes.Red, olcekliKonum.X - 5, olcekliKonum.Y - 5, 10, 10);
                    g.DrawString(AlanAdiFormatla(item.Key), SmallFont, Brushes.Red,
                                olcekliKonum.X, olcekliKonum.Y);
                }
            }

            pbSablonOnizleme.Image = tempImage;
            aktifAlanAdi = null;
            pbSablonOnizleme.Click -= PbSablonOnizleme_Click;
        }

        // Yazdırma metodunu düzeltmek için değişiklikler
        private bool yazdirmadaSablonGoster = true; // Yazdırmada şablonun gösterilip gösterilmeyeceğini kontrol eden değişken

        private string AlanAdiFormatla(string alanAdi)
        {
            // CamelCase formatındaki alan adını daha okunabilir hale getir
            string sonuc = alanAdi[0].ToString();

            for (int i = 1; i < alanAdi.Length; i++)
            {
                if (char.IsUpper(alanAdi[i]))
                {
                    sonuc += " " + alanAdi[i];
                }
                else
                {
                    sonuc += alanAdi[i];
                }
            }

            return sonuc;
        }

        private void BtnOnizle_Click(object sender, EventArgs e)
        {
            FaturaOnizle();
        }

        private void FaturaOnizle()
        {
            if (sablonResmi == null)
            {
                MessageBox.Show("Önce bir fatura şablonu seçmelisiniz!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Yeni bir boş bitmap oluştur (beyaz arka plan için)
                Image onizlemeResmi;
                if (yazdirmadaSablonGoster)
                {
                    onizlemeResmi = new Bitmap(sablonResmi);
                }
                else
                {
                    // Şablon gösterilmeyecekse beyaz bir arka plan oluştur
                    onizlemeResmi = new Bitmap(sablonResmi.Width, sablonResmi.Height);
                    using (Graphics g = Graphics.FromImage(onizlemeResmi))
                    {
                        g.Clear(Color.White);
                    }
                }

                using (Graphics g = Graphics.FromImage(onizlemeResmi))
                {
                    if (notModu)
                    {
                        if (konumlar.ContainsKey("Not"))
                        {
                            var notPanel = FindControl<Panel>("pnlNot");
                            var txtNot = notPanel?.Controls.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtNot");
                            if (txtNot != null)
                            {
                                Point olcekliKonum = OlcekliKonum(konumlar["Not"]);
                                g.DrawString(txtNot.Text, DefaultFont, Brushes.Black, olcekliKonum);
                            }
                        }
                    }
                    else
                    {
                        Font font = DefaultFont;
                        Brush brush = Brushes.Black;

                        // Fatura alanlarını çiz
                        foreach (var alanAdi in alanIsimleri.Where(a => konumlar.ContainsKey(a) &&
                                                                       faturaAlanlari.ContainsKey(a) &&
                                                                       !string.IsNullOrEmpty(faturaAlanlari[a].Text)))
                        {
                            Point olcekliKonum = OlcekliKonum(konumlar[alanAdi]);
                            g.DrawString(faturaAlanlari[alanAdi].Text, font, brush, olcekliKonum);
                        }

                        // Ürünleri çiz
                        if (konumlar.ContainsKey("UrunSatiri") && urunListesi.Any())
                        {
                            CizUrunListesi(g, font, brush);
                        }
                    }
                }

                pbSablonOnizleme.Image = onizlemeResmi;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Önizleme sırasında hata oluştu: {ex.Message}", MESAJ_BASLIK_HATA, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CizUrunListesi(Graphics g, Font font, Brush brush)
        {
            Point basePoint = OlcekliKonum(konumlar["UrunSatiri"]);
            const int satirYuksekligi = 25;
            float olcek = (float)pbSablonOnizleme.Width / sablonResmi.Width;
            int[] xOffsets = { 0, (int)(300 * olcek), (int)(380 * olcek), (int)(460 * olcek), (int)(580 * olcek) };

            for (int i = 0; i < urunListesi.Count; i++)
            {
                var urun = urunListesi[i];
                int yOffset = (int)(i * satirYuksekligi * olcek);

                g.DrawString(urun.Aciklama, font, brush, new Point(basePoint.X + xOffsets[0], basePoint.Y + yOffset));
                g.DrawString(urun.Miktar.ToString(), font, brush, new Point(basePoint.X + xOffsets[1], basePoint.Y + yOffset));
                g.DrawString("%" + urun.KDV.ToString(), font, brush, new Point(basePoint.X + xOffsets[2], basePoint.Y + yOffset));
                g.DrawString(FormatMoney(urun.BirimFiyat), font, brush, new Point(basePoint.X + xOffsets[3], basePoint.Y + yOffset));
                g.DrawString(FormatMoney(urun.ToplamFiyat), font, brush, new Point(basePoint.X + xOffsets[4], basePoint.Y + yOffset));
            }
        }

        private void BtnYazdir_Click(object sender, EventArgs e)
        {
            if (sablonResmi == null)
            {
                MessageBox.Show("Önce bir fatura şablonu seçmelisiniz!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (konumlar.Count == 0)
            {
                MessageBox.Show("Hiçbir alan için konum seçilmemiş!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PrintDocument pd = new PrintDocument();
            pd.PrintPage += Pd_PrintPage;

            PrintDialog printDialog = new PrintDialog
            {
                Document = pd
            };

            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                pd.Print();
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (yazdirmadaSablonGoster)
            {
                e.Graphics.DrawImage(sablonResmi, new Rectangle(0, 0, sablonResmi.Width, sablonResmi.Height));
            }

            if (notModu)
            {
                YazdirNot(e.Graphics);
                return;
            }

            YazdirFatura(e.Graphics);
        }

        private void YazdirNot(Graphics g)
        {
            if (!konumlar.ContainsKey("Not")) return;

            var notPanel = FindControl<Panel>("pnlNot");
            var txtNot = notPanel?.Controls.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtNot");
            if (txtNot != null)
            {
                Font font = new Font("Arial", 10);
                g.DrawString(txtNot.Text, font, Brushes.Black, konumlar["Not"]);
            }
        }

        private void YazdirFatura(Graphics g)
        {
            Font font = DefaultFont;
            Brush brush = Brushes.Black;

            foreach (var alanAdi in alanIsimleri.Where(a => konumlar.ContainsKey(a) &&
                                                           faturaAlanlari.ContainsKey(a) &&
                                                           !string.IsNullOrEmpty(faturaAlanlari[a].Text)))
            {
                g.DrawString(faturaAlanlari[alanAdi].Text, font, brush, konumlar[alanAdi]);
            }

            if (konumlar.ContainsKey("UrunSatiri") && urunListesi.Any())
            {
                CizUrunListesi(g, font, brush);
            }
        }

        private void BtnKonumlariKaydet_Click(object sender, EventArgs e)
        {
            if (konumlar.Count == 0)
            {
                MessageBox.Show("Kaydedilecek konum bulunamadı!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Konum Dosyası|*.loc",
                Title = "Konumları Kaydet"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    // Şablon yolunu kaydet
                    sw.WriteLine(secilenSablonYolu);

                    // Konumları kaydet
                    foreach (var konum in konumlar)
                    {
                        sw.WriteLine($"{konum.Key}|{konum.Value.X}|{konum.Value.Y}");
                    }
                }
            }
        }

        private void BtnKonumlariYukle_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Konum Dosyası|*.loc",
                Title = "Konumları Yükle"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string[] satirlar = File.ReadAllLines(ofd.FileName);

                    if (satirlar.Length < 1)
                    {
                        MessageBox.Show("Geçersiz konum dosyası!", MESAJ_BASLIK_HATA, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Şablon yolunu al ve yükle
                    secilenSablonYolu = satirlar[0];
                    if (File.Exists(secilenSablonYolu))
                    {
                        SablonYukle(secilenSablonYolu);
                    }
                    else
                    {
                        MessageBox.Show("Şablon resmi bulunamadı: " + secilenSablonYolu, MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    // Konumları yükle
                    konumlar.Clear();
                    for (int i = 1; i < satirlar.Length; i++)
                    {
                        string[] parcalar = satirlar[i].Split('|');
                        if (parcalar.Length == 3)
                        {
                            string alanAdi = parcalar[0];
                            int x = int.Parse(parcalar[1]);
                            int y = int.Parse(parcalar[2]);

                            konumlar.Add(alanAdi, new Point(x, y));
                        }
                    }

                    MessageBox.Show("Konumlar başarıyla yüklendi.", MESAJ_BASLIK_BILGI, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Konumları göster
                    FaturaOnizle();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Konumlar yüklenirken hata oluştu: " + ex.Message, MESAJ_BASLIK_HATA, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void NotOnizle(TextBox txtNot)
        {
            if (sablonResmi == null)
            {
                MessageBox.Show("Önce bir şablon seçmelisiniz!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!konumlar.ContainsKey("Not"))
            {
                MessageBox.Show("Lütfen önce not konumu seçin!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Önce eski Image'i dispose edelim
                if (pbSablonOnizleme.Image != null && pbSablonOnizleme.Image != sablonResmi)
                {
                    pbSablonOnizleme.Image.Dispose();
                }

                // Bitmap boyutlarını ayarla
                int width = sablonResmi.Width;
                int height = sablonResmi.Height;

                // Yeni bitmap oluştur
                Image tempImage = new Bitmap(width, height);

                using (Graphics g = Graphics.FromImage(tempImage))
                {
                    if (yazdirmadaSablonGoster)
                    {
                        // Şablonu çiz
                        g.DrawImage(sablonResmi, 0, 0, width, height);
                    }
                    else
                    {
                        // Beyaz arka plan
                        g.Clear(Color.White);
                    }

                    // Notu çiz
                    using (Font font = new Font("Arial", 10))
                    {
                        // Not metnini çiz
                        g.DrawString(txtNot.Text, font, Brushes.Black, konumlar["Not"]);

                        // Debug için konum noktasını göster (isteğe bağlı)
                        if (konumlar.ContainsKey("Not"))
                        {
                            Point olcekliKonum = OlcekliKonum(konumlar["Not"]); // Ölçekli konumu hesapla
                            g.FillEllipse(Brushes.Red, olcekliKonum.X - 2, olcekliKonum.Y - 2, 4, 4);
                        }
                    }
                }

                // PictureBox'ı güncelle
                pbSablonOnizleme.Image = tempImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Not önizleme sırasında hata oluştu: {ex.Message}", MESAJ_BASLIK_HATA, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private T FindControl<T>(string name) where T : Control
        {
            return Controls.OfType<T>().FirstOrDefault(c => c.Name == name);
        }

        private void KonumYonetimiPaneliOlustur()
        {
            Panel pnlKonumYonetimi = new Panel
            {
                Dock = DockStyle.Right,
                Width = 250,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            Label lblBaslik = new Label
            {
                Text = "Konum Yönetimi",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            pnlKonumYonetimi.Controls.Add(lblBaslik);

            // Yeni kayıt için panel
            Panel pnlYeniKayit = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                Padding = new Padding(5)
            };

            // "Konum Kaydetme" label'ı ekle
            Label lblKonumKaydetme = new Label
            {
                Text = "Konum Kaydetme",
                Location = new Point(5, 5),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(230, 20),
                Font = new Font(this.Font, FontStyle.Bold)
            };
            pnlYeniKayit.Controls.Add(lblKonumKaydetme);

            TextBox txtKayitAdi = new TextBox
            {
                Location = new Point(5, 25), // Label'dan sonra konumlandır
                Width = 230,
            };
            pnlYeniKayit.Controls.Add(txtKayitAdi);

            Button btnKaydet = new Button
            {
                Text = "Mevcut Konumları Kaydet",
                Location = new Point(5, 50), // TextBox'dan sonra konumlandır
                Width = 230,
                Height = 30
            };

            btnKaydet.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtKayitAdi.Text))
                {
                    MessageBox.Show("Lütfen bir kayıt adı girin!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (konumlar.Count == 0)
                {
                    MessageBox.Show("Kaydedilecek konum bulunamadı!", MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string kayitYolu = Path.Combine(Application.StartupPath, "Konumlar");
                if (!Directory.Exists(kayitYolu))
                    Directory.CreateDirectory(kayitYolu);

                string dosyaAdi = Path.Combine(kayitYolu, txtKayitAdi.Text + ".loc");

                try
                {
                    using (StreamWriter sw = new StreamWriter(dosyaAdi))
                    {
                        sw.WriteLine(secilenSablonYolu);
                        foreach (var konum in konumlar)
                        {
                            sw.WriteLine($"{konum.Key}|{konum.Value.X}|{konum.Value.Y}");
                        }
                    }
                    KonumKayitlariniListele(pnlKonumYonetimi); // Listeyi güncelle
                    MessageBox.Show("Konumlar başarıyla kaydedildi.", MESAJ_BASLIK_BILGI, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kayıt sırasında hata oluştu: {ex.Message}", MESAJ_BASLIK_HATA, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            pnlYeniKayit.Controls.Add(btnKaydet);

            pnlKonumYonetimi.Controls.Add(pnlYeniKayit);

            // Kayıtlı konumları listele
            KonumKayitlariniListele(pnlKonumYonetimi);

            this.Controls.Add(pnlKonumYonetimi);
        }

        private void KonumKayitlariniListele(Panel panel)
        {
            // Mevcut kayıtları temizle
            foreach (Control ctrl in panel.Controls)
            {
                if (ctrl is Panel p && p.Tag?.ToString() == "KayitPanel")
                    panel.Controls.Remove(ctrl);
            }

            string kayitYolu = Path.Combine(Application.StartupPath, "Konumlar");
            if (!Directory.Exists(kayitYolu))
                return;

            int yPos = 120; // Başlık ve yeni kayıt panelinden sonra başla

            foreach (string dosya in Directory.GetFiles(kayitYolu, "*.loc"))
            {
                string kayitAdi = Path.GetFileNameWithoutExtension(dosya);

                Panel pnlKayit = new Panel
                {
                    Height = 35,
                    Width = 230,
                    Location = new Point(5, yPos),
                    Tag = "KayitPanel"
                };

                Label lblKayitAdi = new Label
                {
                    Text = kayitAdi,
                    AutoEllipsis = true,
                    Width = 120,
                    Location = new Point(5, 10)
                };
                pnlKayit.Controls.Add(lblKayitAdi);

                // Yükle butonu
                Button btnYukle = new Button
                {
                    Text = "Yükle",
                    Width = 45,
                    Location = new Point(130, 5),
                    Height = 25
                };
                btnYukle.Click += (s, e) => KonumlariDosyadanYukle(dosya);
                pnlKayit.Controls.Add(btnYukle);

                // Sil butonu
                Button btnSil = new Button
                {
                    Text = "Sil",
                    Width = 45,
                    Location = new Point(180, 5),
                    Height = 25,
                    BackColor = Color.Red,
                    ForeColor = Color.White
                };
                btnSil.Click += (s, e) =>
                {
                    if (MessageBox.Show($"'{kayitAdi}' konumunu silmek istediğinize emin misiniz?",
                                      "Konum Sil",
                                      MessageBoxButtons.YesNo,
                                      MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        try
                        {
                            File.Delete(dosya);
                            KonumKayitlariniListele(panel); // Listeyi yenile
                            MessageBox.Show("Konum başarıyla silindi.", MESAJ_BASLIK_BILGI, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Konum silinirken hata oluştu: {ex.Message}",
                                          MESAJ_BASLIK_HATA,
                                          MessageBoxButtons.OK,
                                          MessageBoxIcon.Error);
                        }
                    }
                };
                pnlKayit.Controls.Add(btnSil);

                panel.Controls.Add(pnlKayit);
                yPos += 40;
            }
        }

        private void KonumlariDosyadanYukle(string dosyaYolu)
        {
            try
            {
                string[] satirlar = File.ReadAllLines(dosyaYolu);

                if (satirlar.Length < 1)
                {
                    MessageBox.Show("Geçersiz konum dosyası!", MESAJ_BASLIK_HATA, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Şablon yolunu al ve yükle
                secilenSablonYolu = satirlar[0];
                if (File.Exists(secilenSablonYolu))
                {
                    SablonYukle(secilenSablonYolu);
                }
                else
                {
                    MessageBox.Show("Şablon resmi bulunamadı: " + secilenSablonYolu, MESAJ_BASLIK_UYARI, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // Konumları yükle
                konumlar.Clear();
                for (int i = 1; i < satirlar.Length; i++)
                {
                    string[] parcalar = satirlar[i].Split('|');
                    if (parcalar.Length == 3)
                    {
                        string alanAdi = parcalar[0];
                        int x = int.Parse(parcalar[1]);
                        int y = int.Parse(parcalar[2]);
                        konumlar.Add(alanAdi, new Point(x, y));
                    }
                }

                MessageBox.Show("Konumlar başarıyla yüklendi.", MESAJ_BASLIK_BILGI, MessageBoxButtons.OK, MessageBoxIcon.Information);
                FaturaOnizle(); // Konumları göster
            }
            catch (Exception ex)
            {
                MessageBox.Show("Konumlar yüklenirken hata oluştu: " + ex.Message, MESAJ_BASLIK_HATA, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SablonYukle(string yol)
        {
            try
            {
                if (pbSablonOnizleme.Image != null && pbSablonOnizleme.Image != sablonResmi)
                {
                    pbSablonOnizleme.Image.Dispose();
                }
                if (sablonResmi != null)
                {
                    sablonResmi.Dispose();
                }
                sablonResmi = Image.FromFile(yol);
                pbSablonOnizleme.Image = sablonResmi;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Şablon yüklenirken hata: " + ex.Message);
            }
        }

        private Point OlcekliKonum(Point konum)
        {
            if (sablonResmi == null || pbSablonOnizleme.Image == null) return konum;
            return konum; // Ölçekleme yok, konumu aynen kullan
        }

        private Point GercekKonum(Point olcekliKonum)
        {
            if (sablonResmi == null || pbSablonOnizleme.Image == null) return olcekliKonum;
            return olcekliKonum; // Ölçekleme yok, konumu aynen kullan
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (sablonResmi != null)
            {
                sablonResmi.Dispose();
            }

            if (pbSablonOnizleme?.Image != null && pbSablonOnizleme.Image != sablonResmi)
            {
                pbSablonOnizleme.Image.Dispose();
            }
        }
    }
}
public class UrunBilgisi
{
    public string Aciklama { get; set; } = "";
    public decimal Miktar { get; set; }
    public decimal KDV { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal ToplamFiyat => Miktar * BirimFiyat;
    public decimal KDVTutari => ToplamFiyat * (KDV / 100);
    public decimal KDVDahilToplamFiyat => ToplamFiyat + KDVTutari;

}