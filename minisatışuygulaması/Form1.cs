using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace minisatışuygulaması
{
    public partial class Form1 : Form
    {
        private SqlConnection conn;
        private string connectionString = "Server=DESKTOP-8FACNE8; Database=fidan; Integrated Security=True;";
        private int selectedFidanId;
        private string selectedFidanAdi;
        private int adet;
        private decimal toplamTutar;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.fidanTableAdapter11.Fill(this.fidanDataSet13.fidan);
            this.stokTableAdapter.Fill(this.fidanDataSet11.stok);
           
            LoadFidanData();       
            LoadSalesData();       
           
        }

        
        private void LoadFidanData()
        {
            try
            {
                conn = new SqlConnection(connectionString);
                SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM fidan", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                comboBoxfidanseç.DataSource = dt;
                comboBoxfidanseç.DisplayMember = "fidanadi";
                comboBoxfidanseç.ValueMember = "fidanid";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fidan verileri yüklenirken bir hata oluştu: " + ex.Message);
            }
        }

        private void LoadSalesData()
        {
            try
            {
                conn = new SqlConnection(connectionString);
                SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM satislar", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dataGridViewsatislar.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Satış verileri yüklenirken bir hata oluştu: " + ex.Message);
            }
        }



        private void buttonalışverişitamamla_Click(object sender, EventArgs e)
        {
            selectedFidanId = Convert.ToInt32(comboBoxfidanseç.SelectedValue);
            selectedFidanAdi = comboBoxfidanseç.Text;
            adet = Convert.ToInt32(textBoxadetgir.Text);
            decimal fidanFiyati = GetFidanPrice(selectedFidanId);

            toplamTutar = adet * fidanFiyati;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand checkStockCmd = new SqlCommand("SELECT stok FROM stok WHERE fidanid = @FidanID", conn);
                    checkStockCmd.Parameters.AddWithValue("@FidanID", selectedFidanId);
                    object result = checkStockCmd.ExecuteScalar();

                    if (result == null || Convert.ToInt32(result) < adet)
                    {
                        MessageBox.Show("Yeterli stok yok!", "Stok Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    SqlCommand insertCmd = new SqlCommand(
                        "INSERT INTO satislar (satilanfidanid, satilanfidanadeti, toplamtutar) VALUES (@FidanID, @Adet, @ToplamTutar)", conn);
                    insertCmd.Parameters.AddWithValue("@FidanID", selectedFidanId);
                    insertCmd.Parameters.AddWithValue("@Adet", adet);
                    insertCmd.Parameters.AddWithValue("@ToplamTutar", toplamTutar);
                    insertCmd.ExecuteNonQuery();

                    SqlCommand updateStockCmd = new SqlCommand("UPDATE stok SET stok = stok - @Adet WHERE fidanid = @FidanID", conn);
                    updateStockCmd.Parameters.AddWithValue("@Adet", adet);
                    updateStockCmd.Parameters.AddWithValue("@FidanID", selectedFidanId);
                    updateStockCmd.ExecuteNonQuery();
                }

                MessageBox.Show("Satış başarılı!");
                LoadSalesData();
                LoadStockData(); 
                PrintReceipt();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Satış işlemi sırasında bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private decimal GetFidanPrice(int fidanId)
        {
            decimal price = 0;
            try
            {
                conn = new SqlConnection(connectionString);
                SqlCommand cmd = new SqlCommand("SELECT fidanfiyati FROM fidan WHERE fidanid = @fidanId", conn);
                cmd.Parameters.AddWithValue("@fidanId", fidanId);

                conn.Open();
                price = Convert.ToDecimal(cmd.ExecuteScalar());
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fidan fiyatı alınırken bir hata oluştu: " + ex.Message);
            }

            return price;
        }

        private void PrintReceipt()
        {
            PrintDocument printDocument = new PrintDocument();
            printDocument.PrintPage += new PrintPageEventHandler(PrintDocument_PrintPage);
            PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();
            printPreviewDialog.Document = printDocument;
            printPreviewDialog.ShowDialog();
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            Font font = new Font("Arial", 12);
            Brush brush = Brushes.Black;

            int yPos = 20;
            int lineHeight = 25;

            g.DrawString("Fidan Satış Fişi", new Font("Arial", 16, FontStyle.Bold), brush, 100, yPos);
            yPos += lineHeight;

            g.DrawString("Fidan Adı: " + selectedFidanAdi, font, brush, 20, yPos);
            yPos += lineHeight;

            g.DrawString("Adet: " + adet, font, brush, 20, yPos);
            yPos += lineHeight;

            g.DrawString("Fiyat: " + GetFidanPrice(selectedFidanId).ToString("C"), font, brush, 20, yPos);
            yPos += lineHeight;

            g.DrawString("Toplam Tutar: " + toplamTutar.ToString("C"), font, brush, 20, yPos);
            yPos += lineHeight;

            g.DrawLine(Pens.Black, 20, yPos, 280, yPos);
            yPos += 10;

            g.DrawString("Teşekkür ederiz!", font, brush, 20, yPos);
        }

        private void buttonSatislariRaporla_Click(object sender, EventArgs e)
        {
            try
            {
                conn = new SqlConnection(connectionString);
                string query = @"
            SELECT 
                f.fidanadi AS 'Fidan Adı',
                SUM(s.satilanfidanadeti) AS 'Toplam Adet',
                SUM(s.toplamtutar) AS 'Toplam Tutar'
            FROM satislar s
            INNER JOIN fidan f ON s.satilanfidanid = f.fidanid
            GROUP BY f.fidanadi";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                Form raporForm = new Form
                {
                    Text = "Satış Raporu",
                    Size = new Size(600, 400)
                };

                DataGridView raporGridView = new DataGridView
                {
                    DataSource = dt,
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };

                raporForm.Controls.Add(raporGridView);
                raporForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rapor alınırken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonStokEkle_Click(object sender, EventArgs e)
        {
            try
            {
                int fidanID = int.Parse(textBoxFidanID.Text);
                int miktar = int.Parse(textBoxMiktar.Text);

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    SqlCommand checkCmd = new SqlCommand("SELECT stok FROM stok WHERE fidanid = @FidanID", conn);
                    checkCmd.Parameters.AddWithValue("@FidanID", fidanID);
                    object result = checkCmd.ExecuteScalar();

                    if (result != null) 
                    {
                        SqlCommand updateCmd = new SqlCommand("UPDATE stok SET stok = stok + @Miktar WHERE fidanid = @FidanID", conn);
                        updateCmd.Parameters.AddWithValue("@FidanID", fidanID);
                        updateCmd.Parameters.AddWithValue("@Miktar", miktar);
                        updateCmd.ExecuteNonQuery();
                    }
                    else 
                    {
                        SqlCommand insertCmd = new SqlCommand("INSERT INTO stok (fidanid, stok) VALUES (@FidanID, @Miktar)", conn);
                        insertCmd.Parameters.AddWithValue("@FidanID", fidanID);
                        insertCmd.Parameters.AddWithValue("@Miktar", miktar);
                        insertCmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Stok başarıyla güncellendi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                textBoxFidanID.Clear();
                textBoxMiktar.Clear();
                LoadStockData(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void LoadStockData()
        {
            try
            {
                conn = new SqlConnection(connectionString);
                SqlDataAdapter da = new SqlDataAdapter("SELECT fidanid, stok FROM stok", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dataGridView1.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Stok verileri yüklenirken bir hata oluştu: " + ex.Message);
            }
        }


        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

       
    }
}
