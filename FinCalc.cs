using System;
using System.Drawing;
using System.Windows.Forms;

// ─────────────────────────────────────────────────────────────────────────────
//  FinCalc v1.4
//
//  LAYOUT
//  ──────
//  [N][___] [A][________________________] | [P&L][___________] | STD [32nds] [→Dec] [×10✗] [✕]
//
//  Width = half the primary taskbar width. Height = taskbar height (44px).
//
//  MODES (automatic)
//  ─────────────────
//  STD — NOM has a value:
//      P&L = NOM × 10 × PX_A          (×10 disabled if ×10✗ ticked)
//      PX A accepts a single plain price or 32nds price.
//
//  MAN — NOM is 0 / empty:
//      NOM stays visible but is dimmed and ignored.
//      PX A accepts a full inline expression:
//          normal:  2 + 2   100 - 50   200 * 3   99 / 4
//          32nds:   99-25 + 98-16   (only when "32nds" is ticked)
//      Operators: + - * / %
//
//  CHECKBOXES
//  ──────────
//  32nds  – enables Treasury 32nds parsing (digit-dash-digit). Unticked = '-' is subtraction.
//  →Dec   – converts PX A 32nds price to decimal in P&L (requires 32nds ticked)
//  ×10✗   – disables the hidden ×10 multiplier on NOM
// ─────────────────────────────────────────────────────────────────────────────

namespace FinCalc
{
    public class FinCalcForm : Form
    {
        private TextBox  txtNom, txtPxA, txtOut;
        private CheckBox chkTreasury, chkNoMult, chkConvert;
        private Label    lblNom, lblPxA, lblOut, lblMode;
        private ToolTip  tip = new ToolTip();

        private const int H  = 44;
        private const int CY = 22;
        private const int PY = 11;
        private static readonly Color PHColor  = Color.FromArgb(100, 100, 110);
        private static readonly Color ActiveFG = Color.FromArgb(230, 230, 235);

        public FinCalcForm()
        {
            BuildForm();
            BuildControls();
            WireEvents();
        }

        // ── Form shell ────────────────────────────────────────────────────────
        private void BuildForm()
        {
            Text            = "FinCalc";
            FormBorderStyle = FormBorderStyle.None;
            BackColor       = Color.FromArgb(28, 28, 32);
            ForeColor       = Color.FromArgb(210, 210, 215);
            StartPosition   = FormStartPosition.Manual;
            TopMost         = true;

            // Half the taskbar width, same height
            Rectangle wb   = Screen.PrimaryScreen!.WorkingArea;
            Rectangle full = Screen.PrimaryScreen!.Bounds;
            int taskbarH   = full.Height - wb.Height;          // actual taskbar px height
            int formH      = Math.Max(taskbarH, H);
            int formW      = full.Width / 2;

            Size     = new Size(formW, formH);
            Location = new Point(wb.Left, wb.Bottom);          // sit on top of taskbar

            bool  dragging  = false;
            Point dragStart = Point.Empty;
            MouseDown   += (s, e) => { if (e.Button == MouseButtons.Left) { dragging = true; dragStart = e.Location; } };
            MouseMove   += (s, e) => { if (dragging) Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y); };
            MouseUp     += (s, e) => dragging = false;
            DoubleClick += (s, e) => Close();
        }

        // ── Control factories ─────────────────────────────────────────────────
        private TextBox MakeTB(int x, int w, string ph)
        {
            var tb = new TextBox
            {
                Location    = new Point(x, PY),
                Size        = new Size(w, CY),
                BackColor   = Color.FromArgb(42, 42, 50),
                ForeColor   = PHColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Consolas", 8.5f),
                TextAlign   = HorizontalAlignment.Left,
                Text        = ph,
                Tag         = ph,
            };
            tb.GotFocus  += (s, e) => { if (tb.Text == (string)tb.Tag) { tb.Text = ""; tb.ForeColor = ActiveFG; } };
            tb.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = (string)tb.Tag; tb.ForeColor = PHColor; } };
            return tb;
        }

        private Label MakeLbl(int x, string t) => new Label
        {
            Location  = new Point(x, PY + 4),
            AutoSize  = true,
            Text      = t,
            Font      = new Font("Segoe UI", 7f),
            ForeColor = Color.FromArgb(130, 130, 145),
        };

        private CheckBox MakeChk(int x, string t) => new CheckBox
        {
            Location  = new Point(x, PY + 3),
            AutoSize  = true,
            Text      = t,
            Font      = new Font("Segoe UI", 7f),
            ForeColor = Color.FromArgb(170, 170, 185),
            FlatStyle = FlatStyle.Flat,
        };

        private Label MakeSep(int x) => new Label
        {
            Location  = new Point(x, PY),
            Size      = new Size(1, CY),
            BackColor = Color.FromArgb(60, 60, 70),
        };

        // ── Build controls ─────────────────────────────────────────────────────
        //  We lay out all fixed-width controls, then stretch PX A and P&L to fill.
        private void BuildControls()
        {
            int totalW = Width;

            // Fixed-width right section: | STD [32nds] [→Dec] [×10✗] [✕]
            // Measure from right side inward
            int rightPad  = 3;
            int btnW      = 20;
            int chkNoW    = 46;   // ×10✗
            int chkDecW   = 42;   // →Dec
            int chk32W    = 50;   // 32nds
            int modeW     = 30;   // STD
            int sep2W     = 10;
            int rightBlock = rightPad + btnW + 4 + chkNoW + 4 + chkDecW + 4 + chk32W + 4 + modeW + sep2W;

            // Fixed left: [N][38] [A label]
            int leftPad  = 4;
            int nomLblW  = 12;
            int nomTBW   = 44;
            int pxaLblW  = 14;
            int sep1W    = 10;
            // Fixed output: [P&L lbl][out TB]
            int outLblW  = 24;
            int outTBW   = 110;

            // Remaining width → PX A text box
            int fixedUsed = leftPad + nomLblW + 2 + nomTBW + 4 + pxaLblW + 2
                          + sep1W + outLblW + 2 + outTBW + rightBlock;
            int pxaTBW = Math.Max(80, totalW - fixedUsed);

            // ── Now place controls left → right ──────────────────────────────
            int x = leftPad;

            // NOM
            lblNom = MakeLbl(x, "N"); Controls.Add(lblNom); x += nomLblW + 2;
            txtNom = MakeTB(x, nomTBW, "0"); Controls.Add(txtNom); x += nomTBW + 4;

            // PX A
            lblPxA = MakeLbl(x, "A"); Controls.Add(lblPxA); x += pxaLblW + 2;
            txtPxA = MakeTB(x, pxaTBW, "0.00"); Controls.Add(txtPxA); x += pxaTBW;

            // Separator 1
            Controls.Add(MakeSep(x)); x += sep1W;

            // P&L output
            lblOut = MakeLbl(x, "P&L"); Controls.Add(lblOut); x += outLblW + 2;
            txtOut = MakeTB(x, outTBW, "");
            txtOut.ReadOnly  = true;
            txtOut.BackColor = Color.FromArgb(20, 20, 26);
            txtOut.ForeColor = Color.FromArgb(80, 220, 130);
            txtOut.TextAlign = HorizontalAlignment.Right;
            txtOut.Tag       = "";
            Controls.Add(txtOut); x += outTBW;

            // Separator 2
            Controls.Add(MakeSep(x)); x += sep2W;

            // Mode badge
            lblMode = new Label
            {
                Location  = new Point(x, PY + 4),
                AutoSize  = true,
                Text      = "STD",
                Font      = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 180, 120),
            };
            Controls.Add(lblMode); x += modeW;

            // Checkboxes
            chkTreasury = MakeChk(x, "32nds"); Controls.Add(chkTreasury); x += chk32W;
            chkConvert  = MakeChk(x, "→Dec");  Controls.Add(chkConvert);  x += chkDecW + 2;
            chkNoMult   = MakeChk(x, "×10✗");  Controls.Add(chkNoMult);   x += chkNoW + 2;

            // Close button
            var btnClose = new Button
            {
                Location  = new Point(x, PY - 1),
                Size      = new Size(btnW, CY),
                Text      = "✕",
                Font      = new Font("Segoe UI", 7f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(160, 40, 40),
                ForeColor = Color.White,
                Cursor    = Cursors.Hand,
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);

            // Tooltips
            tip.SetToolTip(txtNom,      "Nominal/Size.\nHas value → STD mode: P&L = NOM×10×PxA\nEmpty/0  → MAN mode: type expression in PX A\nSupports k/m: 10k=10,000  5m=5,000,000");
            tip.SetToolTip(txtPxA,      "STD: single price  e.g. 99.25  or  99-25 (with 32nds ticked)\nMAN: full expression  e.g. 2 + 2  or  99-25 + 98-16 (32nds ticked)");
            tip.SetToolTip(txtOut,      "STD: NOM × 10 × PX_A\nMAN: result of expression in PX A");
            tip.SetToolTip(lblMode,     "STD = NOM active  |  MAN = NOM ignored, expression mode");
            tip.SetToolTip(chkTreasury, "Tick to parse 32nds prices (digit-dash-digit).\nUnticked: '-' is always subtraction.");
            tip.SetToolTip(chkConvert,  "→Dec: shows decimal equivalent of 32nds price in P&L.\nRequires 32nds ticked. Result shown in blue.");
            tip.SetToolTip(chkNoMult,   "Disable the ×10 multiplier on NOM.");
        }

        // ── Events ────────────────────────────────────────────────────────────
        private void WireEvents()
        {
            txtNom.TextChanged         += OnAnyChange;
            txtPxA.TextChanged         += OnAnyChange;
            chkNoMult.CheckedChanged   += OnAnyChange;
            chkConvert.CheckedChanged  += OnAnyChange;

            chkTreasury.CheckedChanged += (s, e) =>
            {
                string ph = chkTreasury.Checked ? "99-25" : "0.00";
                ResetPH(txtPxA, ph);
                if (!chkTreasury.Checked) chkConvert.Checked = false;
                Calculate();
            };
        }

        private void ResetPH(TextBox tb, string ph)
        {
            if (tb.ForeColor == PHColor) { tb.Tag = ph; tb.Text = ph; }
        }

        private void OnAnyChange(object? sender, EventArgs e) => Calculate();

        // ── Core calculation ──────────────────────────────────────────────────
        private void Calculate()
        {
            try
            {
                bool manual = IsNomEmpty();

                lblMode.Text      = manual ? "MAN" : "STD";
                lblMode.ForeColor = manual
                    ? Color.FromArgb(210, 160, 60)
                    : Color.FromArgb(80, 180, 120);
                lblNom.ForeColor  = manual
                    ? Color.FromArgb(55, 55, 65)
                    : Color.FromArgb(130, 130, 145);

                string pxaRaw = LiveText(txtPxA);

                // →Dec: just convert PX A 32nds to decimal
                if (chkConvert.Checked && chkTreasury.Checked)
                {
                    double dec = string.IsNullOrWhiteSpace(pxaRaw) ? 0 : Parse32nds(pxaRaw);
                    txtOut.ForeColor = Color.FromArgb(120, 180, 255);
                    txtOut.Text      = dec == 0 ? "---" : dec.ToString("F6").TrimEnd('0').TrimEnd('.');
                    return;
                }

                double result;

                if (manual)
                {
                    // Expression mode — PX A drives the calculation
                    result = EvalExpression(pxaRaw);
                }
                else
                {
                    // Standard: P&L = NOM × 10 × (PX A expression)
                    // PX A accepts a single price OR an inline expression
                    // e.g. 99.25-99.125 = 0.125, then NOM×10×0.125
                    double nom = ParseNominal(LiveText(txtNom));
                    double pxa = EvalExpression(pxaRaw);
                    result = nom * pxa;
                }

                txtOut.ForeColor = result >= 0
                    ? Color.FromArgb(80, 220, 130)
                    : Color.FromArgb(220, 80, 80);

                txtOut.Text = FormatResult(result);
            }
            catch
            {
                txtOut.ForeColor = Color.FromArgb(160, 160, 170);
                txtOut.Text = "---";
            }
        }

        private bool IsNomEmpty()
        {
            string t = LiveText(txtNom);
            return string.IsNullOrWhiteSpace(t) || t == "0";
        }

        private string LiveText(TextBox tb)
            => tb.ForeColor == PHColor ? "" : tb.Text.Trim();

        private string FormatResult(double v)
        {
            if (v == Math.Floor(v) && Math.Abs(v) < 1e12) return v.ToString("N0");
            return v.ToString("N6").TrimEnd('0').TrimEnd('.');
        }

        // ── Expression evaluator ──────────────────────────────────────────────
        //  VALUE OP VALUE  where VALUE is decimal or 32nds (if 32nds ticked)
        //  A bare single value is returned directly.
        private double EvalExpression(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;

            raw = raw.Replace("−", "-").Replace("–", "-").Trim();

            // Try bare single value first
            if (TryParseValue(raw, out double single)) return single;

            // Find operator, distinguishing 32nds '-' from subtraction
            int  opPos  = -1;
            char opChar = ' ';

            for (int i = 1; i < raw.Length; i++)
            {
                char c = raw[i];

                if (c == '+' || c == '*' || c == '/' || c == '%')
                { opPos = i; opChar = c; break; }

                if (c == '-')
                {
                    // If 32nds is ticked, digit-DASH-digit is a price separator — skip it
                    if (chkTreasury.Checked)
                    {
                        bool prevD = char.IsDigit(raw[i - 1]);
                        bool nextD = i + 1 < raw.Length && char.IsDigit(raw[i + 1]);
                        if (prevD && nextD) continue;
                    }
                    opPos = i; opChar = '-'; break;
                }
            }

            if (opPos < 0) return ParseValue(raw);

            double left  = ParseValue(raw[..opPos].Trim());
            double right = ParseValue(raw[(opPos + 1)..].Trim());

            return opChar switch
            {
                '+' => left + right,
                '-' => left - right,
                '*' => left * right,
                '/' => right == 0 ? throw new DivideByZeroException() : left / right,
                '%' => right == 0 ? throw new DivideByZeroException() : (left / right) * 100.0,
                _   => throw new InvalidOperationException(),
            };
        }

        private bool TryParseValue(string s, out double v)
        {
            try { v = ParseValue(s); return true; }
            catch { v = 0; return false; }
        }

        // Parse single token — 32nds only when checkbox ticked
        private double ParseValue(string raw)
        {
            raw = raw.Trim();
            if (string.IsNullOrWhiteSpace(raw) || raw == "0") return 0;
            if (raw.Contains('-') && chkTreasury.Checked) return Parse32nds(raw);
            return double.Parse(raw);
        }

        // ── 32nds parser ──────────────────────────────────────────────────────
        //  99-16   → 99 + 16/32
        //  99-165  → 99 + 16 + (5/8)/32   (256ths)
        //  99-16+  → 99 + 16.5/32          (half-tick)
        private double Parse32nds(string raw)
        {
            int dash = raw.IndexOf('-');
            if (dash < 0) return double.Parse(raw);

            double whole    = double.Parse(raw[..dash]);
            string fracPart = raw[(dash + 1)..];

            bool half = fracPart.EndsWith("+");
            if (half) fracPart = fracPart.TrimEnd('+');

            double ticks = fracPart.Length == 3
                ? int.Parse(fracPart[..2]) + int.Parse(fracPart[2..3]) / 8.0
                : double.Parse(fracPart);

            if (half) ticks += 0.5;
            return whole + ticks / 32.0;
        }

        // ── Nominal parser ─────────────────────────────────────────────────────
        //  ×10 applied unless chkNoMult ticked. Supports k/m suffixes.
        private double ParseNominal(string raw)
        {
            raw = raw.Trim().ToLowerInvariant();
            if (raw == "" || raw == "0") return 0;

            double mult = chkNoMult.Checked ? 1.0 : 10.0;
            if      (raw.EndsWith("m")) { mult *= 1_000_000; raw = raw[..^1]; }
            else if (raw.EndsWith("k")) { mult *= 1_000;     raw = raw[..^1]; }

            return double.Parse(raw) * mult;
        }

        // Standard mode: single price value
        private double ParseSinglePrice(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == "0" || raw == "0.00") return 0;
            if (chkTreasury.Checked && raw == "99-25") return 0;
            return ParseValue(raw);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FinCalcForm());
        }
    }
}
