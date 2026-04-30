using System;
using System.Windows.Forms;
using DataAccess;

namespace UI
{
    public class MainForm : Form
    {
        private Button btnSalesMode;
        private Button btnStockMode;
        private Panel panelMain;

        public MainForm()
        {
            InitializeComponent();
            Database.Initialize();
        }

        private void InitializeComponent()
        {
            this.Text = "EZPos - Retail POS System";
            this.Width = 900;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterScreen;

            btnSalesMode = new Button { Text = "SALES MODE", Left = 10, Top = 10, Width = 120 };
            btnStockMode = new Button { Text = "STOCK MODE", Left = 140, Top = 10, Width = 120 };
            btnSalesMode.Click += (s, e) => ShowSalesMode();
            btnStockMode.Click += (s, e) => ShowStockMode();

            panelMain = new Panel { Left = 10, Top = 50, Width = 860, Height = 500, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

            this.Controls.Add(btnSalesMode);
            this.Controls.Add(btnStockMode);
            this.Controls.Add(panelMain);

            ShowSalesMode();
        }

        private void ShowSalesMode()
        {
            panelMain.Controls.Clear();
            var sales = new SalesModeControl { Dock = DockStyle.Fill };
            panelMain.Controls.Add(sales);
        }

        private void ShowStockMode()
        {
            panelMain.Controls.Clear();
            var stock = new StockModeControl { Dock = DockStyle.Fill };
            panelMain.Controls.Add(stock);
        }
    }
}
