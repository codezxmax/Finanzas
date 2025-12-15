using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using System.Drawing;

namespace FinanzasApp
{
    public class Account
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    public class Transaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Type { get; set; } = "ingreso";
        public string AccountId { get; set; } = string.Empty;
        public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class State
    {
        public List<Account> Accounts { get; set; } = [];
        public List<Transaction> Transactions { get; set; } = [];
    }

    public class MainForm : Form
    {
        readonly CultureInfo culture = (CultureInfo)CultureInfo.GetCultureInfo("es-CL").Clone();
        static readonly JsonSerializerOptions jsonReadOptions = new() { PropertyNameCaseInsensitive = true };
        static readonly JsonSerializerOptions jsonWriteOptions = new() { WriteIndented = true };
        State state;
        readonly string dataDir;
        readonly string path;

        // UI Controls
        readonly DateTimePicker monthPicker;
        readonly Button prevMonthBtn;
        readonly Button nextMonthBtn;
        
        // Account Inputs
        readonly TextBox accName;
        readonly NumericUpDown accBal;
        readonly Button addAccBtn;
        readonly Button delAccBtn;
        readonly DataGridView accGrid;
        readonly ContextMenuStrip accMenu;

        // Transaction Inputs
        readonly ComboBox txType;
        readonly ComboBox txAcc;
        readonly DateTimePicker txDate;
        readonly NumericUpDown txAmt;
        readonly TextBox txCat;
        readonly TextBox txNotes;
        readonly Button addTxBtn;
        readonly Button delTxBtn;
        readonly Button demoBtn;
        readonly DataGridView txGrid;
        readonly ContextMenuStrip txMenu;

        // Edit & Export Controls
        readonly Button editAccBtn;
        readonly Button cancelAccEditBtn;
        readonly Button editTxBtn;
        readonly Button cancelTxEditBtn;
        readonly Button exportBtn;
        readonly ToolTip tip;

        // Editing state
        bool isEditingAcc = false;
        string editingAccId = string.Empty;
        bool isEditingTx = false;
        string editingTxId = string.Empty;

        // Filters
        readonly ComboBox filterType; // Todos/Ingresos/Gastos
        readonly ComboBox filterAcc;  // Todas o cuenta específica
        readonly DateTimePicker filterStart;
        readonly DateTimePicker filterEnd;
        readonly TextBox filterText;
        readonly List<string> filterAccIds = [];

        // Summary Labels
        readonly Label sumIncome;
        readonly Label sumExpense;
        readonly Label sumNet;
        readonly Label sumAccTotal;

        readonly List<string> accComboIds = [];

        // Colors
        readonly Color primaryColor = Color.FromArgb(59, 130, 246); // Blue-500
        readonly Color dangerColor = Color.FromArgb(239, 68, 68); // Red-500
        readonly Color successColor = Color.FromArgb(16, 185, 129); // Emerald-500
        readonly Color bgColor = Color.FromArgb(243, 244, 246); // Gray-100
        readonly Color cardColor = Color.White;
        readonly Color textColor = Color.FromArgb(31, 41, 55); // Gray-800

        public MainForm()
        {
            culture.NumberFormat.CurrencyDecimalDigits = 0;
            dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Finanzas");
            Directory.CreateDirectory(dataDir);
            path = Path.Combine(dataDir, "finanzas.json");
            
            // Form Setup
            Text = "Finanzas Personales";
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            Width = 1280; Height = 850;
            BackColor = bgColor;
            try
            {
                var icoPath = Path.Combine(AppContext.BaseDirectory, "assets", "finanzas.ico");
                Icon = File.Exists(icoPath) ? new Icon(icoPath) : CreateAppIcon();
            }
            catch { Icon = CreateAppIcon(); }

            // Main Layout
            var mainLayout = new TableLayoutPanel 
            { 
                Dock = DockStyle.Fill, 
                ColumnCount = 1, 
                RowCount = 3, 
                Padding = new Padding(20) 
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Summary Cards
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content
            Controls.Add(mainLayout);

            // 1. Header Section
            var headerPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 0, 0, 15) };
            var title = new Label { Text = "Dashboard Financiero", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = textColor, AutoSize = true, Margin = new Padding(0, 5, 20, 0) };
            headerPanel.Controls.Add(title);
            
            var monthPanel = new Panel { AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
            var lblMes = new Label { Text = "Período:", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(0, 8) };
            monthPicker = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MMMM yyyy", ShowUpDown = false, Width = 140, Location = new Point(70, 5) };
            prevMonthBtn = CreateButton("<", Color.FromArgb(107, 114, 128));
            nextMonthBtn = CreateButton(">", Color.FromArgb(107, 114, 128));
            tip = new ToolTip();
            tip.SetToolTip(prevMonthBtn, "Mes anterior");
            tip.SetToolTip(nextMonthBtn, "Mes siguiente");
            prevMonthBtn.Click += (_, __) => monthPicker.Value = monthPicker.Value.AddMonths(-1);
            nextMonthBtn.Click += (_, __) => monthPicker.Value = monthPicker.Value.AddMonths(1);
            headerPanel.Controls.AddRange([lblMes, prevMonthBtn, monthPicker, nextMonthBtn]);
            
            mainLayout.Controls.Add(headerPanel, 0, 0);

            // 2. Summary Section (KPI Cards)
            var summaryLayout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, RowCount = 1, Height = 100, Margin = new Padding(0, 0, 0, 20) };
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            
            sumIncome = CreateSummaryCard(summaryLayout, 0, "Ingresos Mensuales", successColor);
            sumExpense = CreateSummaryCard(summaryLayout, 1, "Gastos Mensuales", dangerColor);
            sumNet = CreateSummaryCard(summaryLayout, 2, "Balance Neto", primaryColor);
            sumAccTotal = CreateSummaryCard(summaryLayout, 3, "Patrimonio Total", Color.FromArgb(75, 85, 99));
            
            mainLayout.Controls.Add(summaryLayout, 0, 1);

            // 3. Content Section (Split View)
            var contentLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); // Accounts
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); // Transactions
            mainLayout.Controls.Add(contentLayout, 0, 2);

            // Accounts Card
            var accPanel = CreateContentCard("Mis Cuentas");
            contentLayout.Controls.Add(accPanel, 0, 0);
            
            // Accounts Inputs
            var accInputPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 10, 0, 10) };
            accName = new TextBox { PlaceholderText = "Nombre cuenta", Width = 150, Font = new Font("Segoe UI", 10) };
            accBal = new NumericUpDown { DecimalPlaces = 0, Maximum = 999999999, Width = 100, Font = new Font("Segoe UI", 10) };
            addAccBtn = CreateButton("Crear", primaryColor);
            editAccBtn = CreateButton("Editar", Color.FromArgb(99, 102, 241));
            delAccBtn = CreateButton("Eliminar", dangerColor);
            cancelAccEditBtn = CreateButton("Cancelar", Color.Gray);

            accInputPanel.Controls.AddRange([accName, accBal, addAccBtn, editAccBtn, delAccBtn, cancelAccEditBtn]);
            accPanel.Controls.Add(accInputPanel);

            // Accounts Grid
            accGrid = CreateGrid();
            accGrid.Columns.AddRange([
                new DataGridViewTextBoxColumn { HeaderText = "Cuenta", DataPropertyName = "Name" },
                new DataGridViewTextBoxColumn { HeaderText = "Saldo", DataPropertyName = "Balance" }
            ]);
            foreach (DataGridViewColumn c in accGrid.Columns) c.SortMode = DataGridViewColumnSortMode.Automatic;
            accPanel.Controls.Add(accGrid);
            accGrid.BringToFront();

            // Transactions Card
            var txPanel = CreateContentCard("Transacciones Recientes");
            contentLayout.Controls.Add(txPanel, 1, 0);
            txPanel.Padding = new Padding(20, 20, 20, 20); // Extra padding for TX
            txPanel.Margin = new Padding(15, 0, 0, 0); // Gap between panels

            // Transaction Inputs (Grid Layout for neatness)
            var txInputTable = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 6, RowCount = 2, Height = 70, Padding = new Padding(0, 0, 0, 15) };
            for (int i = 0; i < 6; i++) txInputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6f));
            
            txType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            txType.Items.AddRange(["ingreso", "gasto"]); txType.SelectedIndex = 0;
            
            txAcc = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            txDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 10) };
            txAmt = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 999999999, Minimum = 1, Font = new Font("Segoe UI", 10) };
            txCat = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Categoría", Font = new Font("Segoe UI", 10) };
            txNotes = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Nota", Font = new Font("Segoe UI", 10) };

            // Row 1 Labels
            txInputTable.Controls.Add(new Label { Text = "Tipo", AutoSize = true, ForeColor = Color.Gray }, 0, 0);
            txInputTable.Controls.Add(new Label { Text = "Cuenta", AutoSize = true, ForeColor = Color.Gray }, 1, 0);
            txInputTable.Controls.Add(new Label { Text = "Fecha", AutoSize = true, ForeColor = Color.Gray }, 2, 0);
            txInputTable.Controls.Add(new Label { Text = "Monto", AutoSize = true, ForeColor = Color.Gray }, 3, 0);
            txInputTable.Controls.Add(new Label { Text = "Categoría", AutoSize = true, ForeColor = Color.Gray }, 4, 0);
            txInputTable.Controls.Add(new Label { Text = "Nota", AutoSize = true, ForeColor = Color.Gray }, 5, 0);

            // Row 2 Controls
            txInputTable.Controls.Add(txType, 0, 1);
            txInputTable.Controls.Add(txAcc, 1, 1);
            txInputTable.Controls.Add(txDate, 2, 1);
            txInputTable.Controls.Add(txAmt, 3, 1);
            txInputTable.Controls.Add(txCat, 4, 1);
            txInputTable.Controls.Add(txNotes, 5, 1);
            
            txPanel.Controls.Add(txInputTable);

            // Transaction Buttons
            var txBtnPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 10) };
            addTxBtn = CreateButton("Registrar Transacción", primaryColor);
            editTxBtn = CreateButton("Editar", Color.FromArgb(99, 102, 241));
            delTxBtn = CreateButton("Eliminar", dangerColor);
            demoBtn = CreateButton("Cargar Demo", Color.Teal);
            exportBtn = CreateButton("Exportar Excel", Color.FromArgb(255, 159, 64));
            cancelTxEditBtn = CreateButton("Cancelar", Color.Gray);
            txBtnPanel.Controls.AddRange([addTxBtn, editTxBtn, delTxBtn, demoBtn, exportBtn, cancelTxEditBtn]);
            txPanel.Controls.Add(txBtnPanel);

            // Filters Panel
            var filterPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 10) };
            filterType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
            filterType.Items.AddRange(["Todos", "Ingresos", "Gastos"]);
            filterType.SelectedIndex = 0;
            filterAcc = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
            filterStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
            filterEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
            filterText = new TextBox { Width = 160, PlaceholderText = "Categoría/Nota" };
            var clearFilters = CreateButton("Limpiar", Color.FromArgb(107, 114, 128));
            filterPanel.Controls.AddRange([
                new Label { Text = "Filtros:", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(0, 8, 8, 0) },
                new Label { Text = "Tipo", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(12, 8, 8, 0) },
                filterType,
                new Label { Text = "Cuenta", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(12, 8, 8, 0) },
                filterAcc,
                new Label { Text = "Desde", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(12, 8, 8, 0) },
                filterStart,
                new Label { Text = "Hasta", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(12, 8, 8, 0) },
                filterEnd,
                filterText,
                clearFilters
            ]);
            txPanel.Controls.Add(filterPanel);

            // Transactions Grid
            txGrid = CreateGrid();
            txGrid.Columns.AddRange([
                new DataGridViewTextBoxColumn { HeaderText = "Fecha", DataPropertyName = "Date" },
                new DataGridViewTextBoxColumn { HeaderText = "Cuenta", DataPropertyName = "AccountName" },
                new DataGridViewTextBoxColumn { HeaderText = "Cat", DataPropertyName = "Category" },
                new DataGridViewTextBoxColumn { HeaderText = "Tipo", DataPropertyName = "Type" },
                new DataGridViewTextBoxColumn { HeaderText = "Monto", DataPropertyName = "AmountDisplay" }
            ]);
            foreach (DataGridViewColumn c in txGrid.Columns) c.SortMode = DataGridViewColumnSortMode.Automatic;
            txPanel.Controls.Add(txGrid);
            txGrid.BringToFront();

            // Logic Wiring
            addAccBtn.Click += (_, __) => AddOrUpdateAccount();
            delAccBtn.Click += (_, __) => DeleteSelectedAccount();
            editAccBtn.Click += (_, __) => StartEditAccount();
            cancelAccEditBtn.Click += (_, __) => CancelEditAccount();
            addTxBtn.Click += (_, __) => AddOrUpdateTransaction();
            editTxBtn.Click += (_, __) => StartEditTransaction();
            delTxBtn.Click += (_, __) => DeleteSelectedTransaction();
            cancelTxEditBtn.Click += (_, __) => CancelEditTransaction();
            demoBtn.Click += (_, __) => DemoData();
            exportBtn.Click += (_, __) => ExportToExcel();
            accMenu = new ContextMenuStrip();
            accMenu.Items.Add("Editar", null, (_, __) => StartEditAccount());
            accMenu.Items.Add("Eliminar", null, (_, __) => DeleteSelectedAccount());
            accGrid.ContextMenuStrip = accMenu;
            txMenu = new ContextMenuStrip();
            txMenu.Items.Add("Editar", null, (_, __) => StartEditTransaction());
            txMenu.Items.Add("Eliminar", null, (_, __) => DeleteSelectedTransaction());
            txGrid.ContextMenuStrip = txMenu;
            tip.SetToolTip(addAccBtn, "Crear/Guardar cuenta");
            tip.SetToolTip(editAccBtn, "Editar cuenta seleccionada");
            tip.SetToolTip(delAccBtn, "Eliminar cuenta seleccionada");
            tip.SetToolTip(addTxBtn, "Registrar/Guardar transacción");
            tip.SetToolTip(editTxBtn, "Editar transacción seleccionada");
            tip.SetToolTip(delTxBtn, "Eliminar transacción seleccionada");
            tip.SetToolTip(exportBtn, "Exportar a Excel");
            monthPicker.ValueChanged += (_, __) => { var (start, end) = SelectedMonthRange(); filterStart.Value = start; filterEnd.Value = end; RenderTransactions(); UpdateSummary(); };

            filterType.SelectedIndexChanged += (_, __) => RenderTransactions();
            filterAcc.SelectedIndexChanged += (_, __) => RenderTransactions();
            filterStart.ValueChanged += (_, __) => RenderTransactions();
            filterEnd.ValueChanged += (_, __) => RenderTransactions();
            filterText.TextChanged += (_, __) => RenderTransactions();

            // Initial Load
            state = ReadState();
            RenderAccounts();
            RefreshAccountCombo();
            RefreshFilterAccountCombo();
            var (initStart, initEnd) = SelectedMonthRange(); filterStart.Value = initStart; filterEnd.Value = initEnd;
            RenderTransactions();
            UpdateSummary();
        }

        // --- UI Helper Methods ---

        Label CreateSummaryCard(TableLayoutPanel parent, int col, string title, Color accent)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = cardColor, Margin = new Padding(5), Padding = new Padding(15) };
            // Rounded corners hack not easy in plain WinForms without custom paint, keeping square for now but clean.
            
            var lblTitle = new Label { Text = title.ToUpper(), Dock = DockStyle.Top, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8, FontStyle.Bold), Height = 20 };
            var lblValue = new Label { Text = "$0", Dock = DockStyle.Fill, ForeColor = accent, Font = new Font("Segoe UI", 18, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            
            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);
            parent.Controls.Add(card, col, 0);
            return lblValue;
        }

        Panel CreateContentCard(string title)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = cardColor, Padding = new Padding(20), Margin = new Padding(5) };
            var header = new Label { Text = title, Dock = DockStyle.Top, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = textColor, Height = 30 };
            card.Controls.Add(header);
            card.Resize += (_, __) => { card.Region = new Region(RoundRect(new Rectangle(Point.Empty, card.Size), 12)); };
            card.Paint += (_, e) => { using var p = new Pen(Color.FromArgb(229, 231, 235)); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1); };
            return card;
        }

        static Button CreateButton(string text, Color bg)
        {
            var btn = new Button 
            { 
                Text = text, 
                AutoSize = true, 
                FlatStyle = FlatStyle.Flat, 
                BackColor = bg, 
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 3, 10, 3),
                Padding = new Padding(10, 5, 10, 5)
            };
            btn.FlatAppearance.BorderSize = 0;
            var baseColor = bg;
            btn.MouseEnter += (_, __) => btn.BackColor = Lighten(baseColor, 0.08f);
            btn.MouseLeave += (_, __) => btn.BackColor = baseColor;
            return btn;
        }

        DataGridView CreateGrid()
        {
            var grid = new DataGridView 
            { 
                Dock = DockStyle.Fill, 
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowHeadersVisible = false,
                GridColor = Color.FromArgb(229, 231, 235), // Gray-200,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(249, 250, 251), // Gray-50
                    ForeColor = Color.FromArgb(107, 114, 128), // Gray-500
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Padding = new Padding(10)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Padding = new Padding(8),
                    ForeColor = textColor,
                    SelectionBackColor = Color.FromArgb(219, 234, 254), // Blue-100
                    SelectionForeColor = textColor
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(249, 250, 251) }
            };
            grid.RowTemplate.Height = 40;
            return grid;
        }

        private static Icon CreateAppIcon()
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, 32, 32);
                using var b = new System.Drawing.Drawing2D.LinearGradientBrush(rect, Color.FromArgb(59,130,246), Color.FromArgb(16,185,129), 45f);
                g.FillEllipse(b, 0, 0, 32, 32);
                using var f = new Font("Segoe UI", 16, FontStyle.Bold);
                var s = "$";
                var size = g.MeasureString(s, f);
                g.DrawString(s, f, Brushes.White, (32 - size.Width) / 2f, (32 - size.Height) / 2f - 1f);
            }
            var h = bmp.GetHicon();
            var ic = Icon.FromHandle(h);
            return ic;
        }

        // --- Logic Methods (Kept same mostly) ---

        string Fmt(decimal n) => n.ToString("C0", culture);

        static System.Drawing.Drawing2D.GraphicsPath RoundRect(Rectangle r, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath p = new();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        static Color Lighten(Color c, float amount)
        {
            int L(int v) => Math.Min(255, (int)(v + 255 * amount));
            return Color.FromArgb(c.A, L(c.R), L(c.G), L(c.B));
        }

        State ReadState()
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<State>(json, jsonReadOptions) ?? new State();
                }
                var legacy = Path.Combine(AppContext.BaseDirectory, "finanzas.json");
                if (File.Exists(legacy))
                {
                    var json = File.ReadAllText(legacy);
                    return JsonSerializer.Deserialize<State>(json, jsonReadOptions) ?? new State();
                }
                return new State();
            }
            catch { return new State(); }
        }

        void WriteState()
        {
            var json = JsonSerializer.Serialize(state, jsonWriteOptions);
            Directory.CreateDirectory(dataDir);
            File.WriteAllText(path, json);
        }

        void RefreshAccountCombo()
        {
            txAcc.Items.Clear();
            accComboIds.Clear();
            foreach (var a in state.Accounts)
            {
                txAcc.Items.Add(a.Name);
                accComboIds.Add(a.Id);
            }
            if (txAcc.Items.Count > 0) txAcc.SelectedIndex = 0;
        }

        void RefreshFilterAccountCombo()
        {
            filterAcc.Items.Clear();
            filterAccIds.Clear();
            filterAcc.Items.Add("Todas");
            filterAccIds.Add("");
            foreach (var a in state.Accounts)
            {
                filterAcc.Items.Add(a.Name);
                filterAccIds.Add(a.Id);
            }
            filterAcc.SelectedIndex = 0;
        }

        void RenderAccounts()
        {
            accGrid.DataSource = state.Accounts.Select(a => new { a.Id, a.Name, Balance = Fmt(a.Balance) }).ToList();
            // Hide ID column if generated
            if (accGrid.Columns["Id"] != null) accGrid.Columns["Id"].Visible = false;
            UpdateTotals();
        }

        (DateTime start, DateTime end) SelectedMonthRange()
        {
            var y = monthPicker.Value.Year;
            var m = monthPicker.Value.Month;
            var start = new DateTime(y, m, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }

        void RenderTransactions()
        {
            var (startMonth, endMonth) = SelectedMonthRange();
            var list = state.Transactions.Where(t =>
            {
                if (!DateTime.TryParse(t.Date, out var dt)) return false;
                var start = filterStart?.Value ?? startMonth;
                var end = filterEnd?.Value ?? endMonth;
                if (dt < start || dt > end) return false;
                var typeSel = filterType?.SelectedItem?.ToString() ?? "Todos";
                if (typeSel == "Ingresos" && t.Type != "ingreso") return false;
                if (typeSel == "Gastos" && t.Type != "gasto") return false;
                if (filterAcc != null && filterAcc.SelectedIndex > 0)
                {
                    var fid = filterAccIds[filterAcc.SelectedIndex];
                    if (t.AccountId != fid) return false;
                }
                var q = (filterText?.Text ?? "").Trim();
                if (q.Length > 0)
                {
                    var hay = (t.Category ?? "") + " " + (t.Notes ?? "");
                    if (!hay.Contains(q, StringComparison.OrdinalIgnoreCase)) return false;
                }
                return true;
            }).OrderByDescending(t => t.Date).ToList();
            
            var rows = list.Select(t =>
            {
                var acc = state.Accounts.FirstOrDefault(a => a.Id == t.AccountId);
                var typeDisplay = t.Type == "ingreso" ? "Ingreso" : "Gasto";
                var sign = t.Type == "gasto" ? "-" : "+";
                return new { t.Id, t.Date, AccountName = acc?.Name ?? "N/A", t.Category, Type = typeDisplay, AmountDisplay = sign + Fmt(t.Amount) };
            }).ToList();
            txGrid.DataSource = rows;
            if (txGrid.Columns["Id"] != null) txGrid.Columns["Id"].Visible = false;
        }

        void UpdateSummary()
        {
            var (startMonth, endMonth) = SelectedMonthRange();
            decimal income = 0, expense = 0;
            foreach (var t in state.Transactions)
            {
                if (!DateTime.TryParse(t.Date, out var dt)) continue;
                if (dt < startMonth || dt > endMonth) continue;
                if (t.Type == "ingreso") income += t.Amount; else expense += t.Amount;
            }
            sumIncome.Text = Fmt(income);
            sumExpense.Text = Fmt(expense);
            sumNet.Text = Fmt(income - expense);
            
            // Style Net color
            sumNet.ForeColor = (income - expense) >= 0 ? successColor : dangerColor;
            
            UpdateTotals();
        }

        void UpdateTotals()
        {
            var total = state.Accounts.Sum(a => a.Balance);
            sumAccTotal.Text = Fmt(total);
        }

        void AddOrUpdateAccount()
        {
            var name = accName.Text.Trim();
            var bal = accBal.Value;
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Ingresa un nombre de cuenta."); return; }
            if (isEditingAcc)
            {
                var accEdit = state.Accounts.FirstOrDefault(a => a.Id == editingAccId);
                if (accEdit == null) { MessageBox.Show("No se encontró la cuenta a editar."); return; }
                accEdit.Name = name;
                accEdit.Balance = bal;
                isEditingAcc = false; editingAccId = string.Empty; addAccBtn.Text = "Crear";
            }
            else
            {
                var acc = new Account { Id = Guid.NewGuid().ToString("N"), Name = name, Balance = bal };
                state.Accounts.Add(acc);
            }
            WriteState();
            accName.Text = "";
            accBal.Value = 0;
            RenderAccounts();
            RefreshAccountCombo();
            RefreshFilterAccountCombo();
            RenderTransactions();
            UpdateSummary();
        }

        void StartEditAccount()
        {
            if (accGrid.SelectedRows.Count == 0) { MessageBox.Show("Selecciona una cuenta."); return; }
            var id = accGrid.SelectedRows[0].Cells["Id"].Value?.ToString();
            if (id == null) return;
            var acc = state.Accounts.FirstOrDefault(a => a.Id == id);
            if (acc == null) return;
            accName.Text = acc.Name;
            accBal.Value = acc.Balance;
            isEditingAcc = true; editingAccId = id; addAccBtn.Text = "Guardar";
        }

        void CancelEditAccount()
        {
            isEditingAcc = false; editingAccId = string.Empty; addAccBtn.Text = "Crear";
            accName.Text = ""; accBal.Value = 0;
        }

        void DeleteSelectedAccount()
        {
            if (accGrid.SelectedRows.Count == 0) return;
            var id = accGrid.SelectedRows[0].Cells["Id"].Value?.ToString();
            if (id == null) return;
            if (state.Transactions.Any(t => t.AccountId == id)) { MessageBox.Show("La cuenta tiene transacciones. Elimínalas primero."); return; }
            state.Accounts.RemoveAll(a => a.Id == id);
            WriteState();
            RenderAccounts();
            RefreshAccountCombo();
            RenderTransactions();
            UpdateSummary();
        }

        void AddOrUpdateTransaction()
        {
            if (txAcc.SelectedIndex < 0) { MessageBox.Show("Selecciona una cuenta."); return; }
            var type = txType.SelectedItem?.ToString() ?? "ingreso";
            var accId = accComboIds[txAcc.SelectedIndex];
            var acc = state.Accounts.FirstOrDefault(a => a.Id == accId);
            var date = txDate.Value.ToString("yyyy-MM-dd");
            var amount = txAmt.Value;
            var category = txCat.Text.Trim();
            var notes = txNotes.Text.Trim();
            if (acc == null) { MessageBox.Show("Selecciona una cuenta."); return; }
            if (amount <= 0) { MessageBox.Show("El monto debe ser mayor que 0."); return; }
            if (string.IsNullOrWhiteSpace(category)) { MessageBox.Show("Ingresa una categoría."); return; }
            if (isEditingTx)
            {
                var tOld = state.Transactions.FirstOrDefault(x => x.Id == editingTxId);
                if (tOld == null) { MessageBox.Show("No se encontró la transacción a editar."); return; }
                var oldAcc = state.Accounts.FirstOrDefault(a => a.Id == tOld.AccountId);
                if (oldAcc != null)
                {
                    if (tOld.Type == "ingreso") oldAcc.Balance -= tOld.Amount; else oldAcc.Balance += tOld.Amount;
                }
                tOld.Type = type;
                tOld.AccountId = acc.Id;
                tOld.Date = date;
                tOld.Amount = amount;
                tOld.Category = category;
                tOld.Notes = notes;
                if (type == "ingreso") acc.Balance += amount; else acc.Balance -= amount;
                isEditingTx = false; editingTxId = string.Empty; addTxBtn.Text = "Registrar Transacción";
            }
            else
            {
                var t = new Transaction { Id = Guid.NewGuid().ToString("N"), Type = type, AccountId = acc.Id, Date = date, Amount = amount, Category = category, Notes = notes };
                state.Transactions.Add(t);
                if (type == "ingreso") acc.Balance += amount; else acc.Balance -= amount;
            }
            
            WriteState();
            txAmt.Value = 1;
            txCat.Text = "";
            txNotes.Text = "";
            RenderAccounts();
            RenderTransactions();
            UpdateSummary();
        }

        void StartEditTransaction()
        {
            if (txGrid.SelectedRows.Count == 0) { MessageBox.Show("Selecciona una transacción."); return; }
            var id = txGrid.SelectedRows[0].Cells["Id"].Value?.ToString();
            if (id == null) return;
            var t = state.Transactions.FirstOrDefault(x => x.Id == id);
            if (t == null) return;
            txType.SelectedItem = t.Type;
            var idx = accComboIds.IndexOf(t.AccountId);
            if (idx >= 0) txAcc.SelectedIndex = idx;
            if (DateTime.TryParse(t.Date, out var d)) txDate.Value = d;
            txAmt.Value = t.Amount;
            txCat.Text = t.Category;
            txNotes.Text = t.Notes;
            isEditingTx = true; editingTxId = id; addTxBtn.Text = "Guardar";
        }

        void CancelEditTransaction()
        {
            isEditingTx = false; editingTxId = string.Empty; addTxBtn.Text = "Registrar Transacción";
            txAmt.Value = 1; txCat.Text = ""; txNotes.Text = "";
        }

        void ExportToExcel()
        {
            var sfd = new SaveFileDialog { Filter = "Excel (*.xls)|*.xls", FileName = "Finanzas_" + monthPicker.Value.ToString("yyyy_MM") + ".xls" };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            var (selStart, selEnd) = SelectedMonthRange();
            var start = filterStart?.Value ?? selStart;
            var end = filterEnd?.Value ?? selEnd;
            var typeSel = filterType?.SelectedItem?.ToString() ?? "Todos";
            var accFilterId = (filterAcc != null && filterAcc.SelectedIndex > 0) ? filterAccIds[filterAcc.SelectedIndex] : "";
            var q = (filterText?.Text ?? "").Trim();

            var txs = state.Transactions.Where(t =>
            {
                if (!DateTime.TryParse(t.Date, out var dt)) return false;
                if (dt < start || dt > end) return false;
                if (typeSel == "Ingresos" && t.Type != "ingreso") return false;
                if (typeSel == "Gastos" && t.Type != "gasto") return false;
                if (!string.IsNullOrEmpty(accFilterId) && t.AccountId != accFilterId) return false;
                if (q.Length > 0)
                {
                    var hay = (t.Category ?? "") + " " + (t.Notes ?? "");
                    if (!hay.Contains(q, StringComparison.OrdinalIgnoreCase)) return false;
                }
                return true;
            }).OrderByDescending(t => t.Date).ToList();

            using (var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("<html><head><meta charset='utf-8'></head><body>");
                sw.WriteLine("<h2>Resumen de Cuentas</h2>");
                sw.WriteLine("<table border='1' cellspacing='0' cellpadding='5'>");
                sw.WriteLine("<tr><th>Cuenta</th><th>Saldo</th></tr>");
                foreach (var a in state.Accounts)
                {
                    sw.WriteLine($"<tr><td>{System.Web.HttpUtility.HtmlEncode(a.Name)}</td><td>{Fmt(a.Balance)}</td></tr>");
                }
                sw.WriteLine("</table>");

                sw.WriteLine("<h2>Transacciones</h2>");
                sw.WriteLine("<table border='1' cellspacing='0' cellpadding='5'>");
                sw.WriteLine("<tr><th>Fecha</th><th>Cuenta</th><th>Categoría</th><th>Tipo</th><th>Monto</th></tr>");
                foreach (var t in txs)
                {
                    var accNameStr = state.Accounts.FirstOrDefault(a => a.Id == t.AccountId)?.Name ?? "N/A";
                    var sign = t.Type == "gasto" ? "-" : "+";
                    var tipo = t.Type == "ingreso" ? "Ingreso" : "Gasto";
                    sw.WriteLine($"<tr><td>{t.Date}</td><td>{System.Web.HttpUtility.HtmlEncode(accNameStr)}</td><td>{System.Web.HttpUtility.HtmlEncode(t.Category)}</td><td>{tipo}</td><td>{sign}{Fmt(t.Amount)}</td></tr>");
                }
                sw.WriteLine("</table>");
                sw.WriteLine("</body></html>");
            }
            MessageBox.Show("Exportado a Excel (.xls) correctamente.");
        }

        void DeleteSelectedTransaction()
        {
            if (txGrid.SelectedRows.Count == 0) return;
            var id = txGrid.SelectedRows[0].Cells["Id"].Value?.ToString();
            if (id == null) return;
            var t = state.Transactions.FirstOrDefault(x => x.Id == id);
            if (t == null) return;
            var acc = state.Accounts.FirstOrDefault(a => a.Id == t.AccountId);
            if (acc != null)
            {
                if (t.Type == "ingreso") acc.Balance -= t.Amount; else acc.Balance += t.Amount;
            }
            state.Transactions.RemoveAll(x => x.Id == id);
            WriteState();
            RenderAccounts();
            RenderTransactions();
            UpdateSummary();
        }

        void DemoData()
        {
            state = new State();
            var a1 = new Account { Name = "Banco Estado", Balance = 250000 };
            var a2 = new Account { Name = "Efectivo", Balance = 30000 };
            state.Accounts = [a1, a2];
            
            var now = DateTime.Now;
            string Mk(int d) => new DateTime(now.Year, now.Month, d).ToString("yyyy-MM-dd");
            
            state.Transactions =
            [ 
                new() { Type = "ingreso", AccountId = a1.Id, Date = Mk(1), Amount = 800000, Category = "Sueldo", Notes = "Mensual" },
                new() { Type = "gasto", AccountId = a1.Id, Date = Mk(5), Amount = 150000, Category = "Supermercado", Notes = "Lider" },
                new() { Type = "gasto", AccountId = a2.Id, Date = Mk(10), Amount = 5000, Category = "Transporte", Notes = "Metro" },
                new() { Type = "gasto", AccountId = a1.Id, Date = Mk(15), Amount = 45000, Category = "Internet", Notes = "VTR" }
            ];
            
            a1.Balance += 800000 - 150000 - 45000;
            a2.Balance -= 5000;
            
            WriteState();
            RenderAccounts();
            RefreshAccountCombo();
            RenderTransactions();
            UpdateSummary();
        }
    }
}
