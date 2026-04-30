using System;
using System.Drawing;
using System.Windows.Forms;
using DataAccess;
using Models;

namespace UI
{
    public class StockModeControl : UserControl
    {
        private TextBox txtBarcode;
        private Panel panelProductInfo, panelNewProduct;
        private Label lblName, lblStock, lblPrice;
        private TextBox txtAddQty;
        private Button btnAddStock;

        // New product form
        private TextBox txtNewName, txtNewPrice, txtNewStock;
        private Button btnSaveNew;

        private Product currentProduct;

        public StockModeControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.Gainsboro;

            txtBarcode = new TextBox { Left = 10, Top = 10, Width = 200, TabIndex = 0 };
            txtBarcode.PlaceholderText = "Scan/Masukkan barcode...";
            txtBarcode.KeyDown += TxtBarcode_KeyDown;

            // Panel info produk sedia ada
            panelProductInfo = new Panel { Left = 10, Top = 50, Width = 350, Height = 150, Visible = false };
            lblName = new Label { Left = 10, Top = 10, Width = 300 };
            lblStock = new Label { Left = 10, Top = 40, Width = 300 };
            lblPrice = new Label { Left = 10, Top = 70, Width = 300 };
            txtAddQty = new TextBox { Left = 10, Top = 100, Width = 80 };
            txtAddQty.PlaceholderText = "Tambah kuantiti";
            btnAddStock = new Button { Text = "Tambah Stok", Left = 100, Top = 98, Width = 120 };
            btnAddStock.Click += BtnAddStock_Click;
            panelProductInfo.Controls.Add(lblName);
            panelProductInfo.Controls.Add(lblStock);
            panelProductInfo.Controls.Add(lblPrice);
            panelProductInfo.Controls.Add(txtAddQty);
            panelProductInfo.Controls.Add(btnAddStock);

            // Panel produk baru
            panelNewProduct = new Panel { Left = 10, Top = 220, Width = 350, Height = 180, Visible = false };
            txtNewName = new TextBox { Left = 10, Top = 10, Width = 200, PlaceholderText = "Nama produk" };
            txtNewPrice = new TextBox { Left = 10, Top = 50, Width = 200, PlaceholderText = "Harga (cth: 2.50)" };
            txtNewStock = new TextBox { Left = 10, Top = 90, Width = 200, PlaceholderText = "Stok permulaan" };
            btnSaveNew = new Button { Text = "Simpan Produk Baru", Left = 10, Top = 130, Width = 200 };
            btnSaveNew.Click += BtnSaveNew_Click;
            panelNewProduct.Controls.Add(txtNewName);
            panelNewProduct.Controls.Add(txtNewPrice);
            panelNewProduct.Controls.Add(txtNewStock);
            panelNewProduct.Controls.Add(btnSaveNew);

            this.Controls.Add(txtBarcode);
            this.Controls.Add(panelProductInfo);
            this.Controls.Add(panelNewProduct);

            this.GotFocus += (s, e) => txtBarcode.Focus();
            this.Enter += (s, e) => txtBarcode.Focus();
            this.Click += (s, e) => txtBarcode.Focus();
            this.Load += (s, e) => txtBarcode.Focus();
        }

        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtBarcode.Text))
            {
                string barcode = txtBarcode.Text.Trim();
                currentProduct = ProductRepository.GetByBarcode(barcode);
                if (currentProduct != null)
                {
                    ShowProductInfo();
                }
                else
                {
                    ShowNewProductForm(barcode);
                }
                txtBarcode.Clear();
            }
        }

        private void ShowProductInfo()
        {
            panelProductInfo.Visible = true;
            panelNewProduct.Visible = false;
            lblName.Text = $"Nama: {currentProduct.Name}";
            lblStock.Text = $"Stok semasa: {currentProduct.Stock}";
            lblPrice.Text = $"Harga: RM {currentProduct.Price:F2}";
            txtAddQty.Text = "";
        }

        private void BtnAddStock_Click(object sender, EventArgs e)
        {
            if (currentProduct == null) return;
            int qty = 0;
            if (!int.TryParse(txtAddQty.Text, out qty) || qty <= 0)
            {
                MessageBox.Show("Sila masukkan kuantiti yang sah.", "Ralat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int newStock = currentProduct.Stock + qty;
            ProductRepository.UpdateStock(currentProduct.Id, newStock);
            MessageBox.Show($"Stok berjaya ditambah. Stok baru: {newStock}", "Berjaya", MessageBoxButtons.OK, MessageBoxIcon.Information);
            currentProduct.Stock = newStock;
            ShowProductInfo();
        }

        private void ShowNewProductForm(string barcode)
        {
            panelProductInfo.Visible = false;
            panelNewProduct.Visible = true;
            txtNewName.Text = "";
            txtNewPrice.Text = "";
            txtNewStock.Text = "";
            txtNewName.Focus();
            panelNewProduct.Tag = barcode;
        }

        private void BtnSaveNew_Click(object sender, EventArgs e)
        {
            string barcode = panelNewProduct.Tag as string;
            string name = txtNewName.Text.Trim();
            decimal price = 0;
            int stock = 0;
            if (string.IsNullOrWhiteSpace(name) ||
                !decimal.TryParse(txtNewPrice.Text, out price) ||
                !int.TryParse(txtNewStock.Text, out stock) ||
                price <= 0 || stock < 0)
            {
                MessageBox.Show("Sila isi semua maklumat dengan betul.", "Ralat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var product = new Product { Barcode = barcode, Name = name, Price = price, Stock = stock };
            ProductRepository.Add(product);
            MessageBox.Show("Produk baru berjaya disimpan!", "Berjaya", MessageBoxButtons.OK, MessageBoxIcon.Information);
            panelNewProduct.Visible = false;
        }
    }
}