using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DataAccess;
using Models;

namespace UI
{
    public class SalesModeControl : UserControl
    {
        private TextBox txtBarcode;
        private DataGridView dgvCart;
        private Label lblTotal, lblCash, lblChange;
        private TextBox txtCash;
        private Button btnPay, btnClear;

        private List<SaleItem> cart = new List<SaleItem>();
        private List<Product> cartProducts = new List<Product>();

        public SalesModeControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.WhiteSmoke;

            txtBarcode = new TextBox { Left = 10, Top = 10, Width = 200, TabIndex = 0 };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;
            txtBarcode.PlaceholderText = "Scan barcode...";

            dgvCart = new DataGridView
            {
                Left = 10,
                Top = 50,
                Width = 600,
                Height = 300,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvCart.Columns.Add("Name", "Nama Produk");
            dgvCart.Columns.Add("Qty", "Kuantiti");
            dgvCart.Columns.Add("Price", "Harga");
            dgvCart.Columns.Add("Subtotal", "Jumlah");

            lblTotal = new Label { Left = 10, Top = 360, Width = 300, Font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold) };
            lblCash = new Label { Left = 10, Top = 400, Width = 80, Text = "Tunai" };
            txtCash = new TextBox { Left = 100, Top = 395, Width = 120 };
            txtCash.TextChanged += TxtCash_TextChanged;
            lblChange = new Label { Left = 250, Top = 400, Width = 200, Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold) };

            btnPay = new Button { Text = "BAYAR", Left = 10, Top = 440, Width = 120, Height = 40, BackColor = Color.LightGreen };
            btnPay.Click += BtnPay_Click;
            btnClear = new Button { Text = "CLEAR", Left = 140, Top = 440, Width = 120, Height = 40, BackColor = Color.LightCoral };
            btnClear.Click += BtnClear_Click;

            this.Controls.Add(txtBarcode);
            this.Controls.Add(dgvCart);
            this.Controls.Add(lblTotal);
            this.Controls.Add(lblCash);
            this.Controls.Add(txtCash);
            this.Controls.Add(lblChange);
            this.Controls.Add(btnPay);
            this.Controls.Add(btnClear);

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
                var product = ProductRepository.GetByBarcode(barcode);
                if (product == null)
                {
                    MessageBox.Show("Produk tidak dijumpai!", "Ralat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    AddToCart(product);
                }
                txtBarcode.Clear();
                txtBarcode.Focus();
            }
        }

        private void AddToCart(Product product)
        {
            var idx = cart.FindIndex(x => x.ProductId == product.Id);
            if (idx >= 0)
            {
                cart[idx].Quantity++;
            }
            else
            {
                cart.Add(new SaleItem { ProductId = product.Id, Quantity = 1, Price = product.Price });
                cartProducts.Add(product);
            }
            RefreshCartGrid();
        }

        private void RefreshCartGrid()
        {
            dgvCart.Rows.Clear();
            decimal total = 0;
            for (int i = 0; i < cart.Count; i++)
            {
                var item = cart[i];
                var product = cartProducts[i];
                decimal subtotal = item.Quantity * item.Price;
                dgvCart.Rows.Add(product.Name, item.Quantity, item.Price.ToString("C"), subtotal.ToString("C"));
                total += subtotal;
            }
            lblTotal.Text = $"Jumlah: RM {total:F2}";
            UpdateChange();
        }

        private void TxtCash_TextChanged(object sender, EventArgs e)
        {
            UpdateChange();
        }

        private void UpdateChange()
        {
            decimal total = 0;
            foreach (var item in cart)
                total += item.Quantity * item.Price;
            decimal cash = 0;
            decimal.TryParse(txtCash.Text, out cash);
            decimal change = cash - total;
            lblChange.Text = $"Baki: RM {change:F2}";
        }

        private void BtnPay_Click(object sender, EventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Cart kosong!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            decimal total = 0;
            foreach (var item in cart)
                total += item.Quantity * item.Price;
            decimal cash = 0;
            if (!decimal.TryParse(txtCash.Text, out cash) || cash < total)
            {
                MessageBox.Show("Tunai tidak mencukupi!", "Ralat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Simpan jualan
            var sale = new Sale { DateTime = DateTime.Now, TotalAmount = total };
            int saleId = SaleRepository.AddSale(sale, cart);

            // Cetak resit (placeholder)
            PrintReceipt(saleId, cash, total, cash - total);

            // Reset cart
            cart.Clear();
            cartProducts.Clear();
            RefreshCartGrid();
            txtCash.Clear();
            MessageBox.Show("Transaksi berjaya!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            cart.Clear();
            cartProducts.Clear();
            RefreshCartGrid();
            txtCash.Clear();
        }

        private void PrintReceipt(int saleId, decimal cash, decimal total, decimal change)
        {
            // Guna nama kedai dari config
            string storeName = ConfigHelper.Get("StoreName", "EZPOS GROCERY");
            string receipt = $"\n=== {storeName} ===\n";
            receipt += $"Tarikh: {DateTime.Now:yyyy-MM-dd HH:mm}\n";
            receipt += "--------------------------\n";
            for (int i = 0; i < cart.Count; i++)
            {
                var item = cart[i];
                var product = cartProducts[i];
                receipt += $"{product.Name} x{item.Quantity} @ RM{item.Price:F2}\n";
            }
            receipt += "--------------------------\n";
            receipt += $"Jumlah : RM{total:F2}\n";
            receipt += $"Tunai  : RM{cash:F2}\n";
            receipt += $"Baki   : RM{change:F2}\n";
            receipt += "\nTerima kasih!\n";

            // Print ke thermal printer jika PrinterName ada dalam config
            string printerName = ConfigHelper.Get("PrinterName", "");
            if (!string.IsNullOrWhiteSpace(printerName))
            {
                try
                {
                    var pd = new System.Drawing.Printing.PrintDocument();
                    pd.PrinterSettings.PrinterName = printerName;
                    pd.PrintPage += (s, e) =>
                    {
                        e.Graphics.DrawString(receipt, new Font("Consolas", 10), Brushes.Black, 0, 0);
                    };
                    pd.Print();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal cetak resit: {ex.Message}\nResit akan dipaparkan di skrin.", "Ralat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    MessageBox.Show(receipt, "Resit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(receipt, "Resit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}