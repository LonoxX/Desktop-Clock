using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;

namespace DesktopClock
{
    public partial class SettingsForm : Form
    {
        private Form1 mainForm;

        private Color clockColor;
        private int fontSize;
        private bool showDate;
        private bool autostart;
        private bool showBackground;
        private Color backgroundColor;
        private double backgroundOpacity; // Added missing field

        private const int SmallMoveStep = 5;
        private const int LargeMoveStep = 20;

        private Button? btnOK;
        private Button? btnCancel;
        private Panel? pnlLayout;
        private GroupBox? gbGeneral;
        private GroupBox? gbPresets;

        private Label? lblColor;
        private Panel? pnlColorPreview;
        private Label? lblFontSize;
        private NumericUpDown? numFontSize;
        private CheckBox? chkShowDate;

        private CheckBox? chkAutostart;
        private Button? btnReset;

        private ComboBox? cmbColorPresets;
        private Label? lblPresets;

        private ToolTip? toolTip;
        private System.ComponentModel.IContainer components;
        private Dictionary<string, Color> colorPresets = new Dictionary<string, Color>();

        private GroupBox? gbBackground;
        private CheckBox? chkShowBackground;
        private Panel? pnlBackgroundColorPreview;
        private Button? btnBackgroundColor;
        private TrackBar? trkBackgroundOpacity;
        private Label? lblBackgroundOpacity;

        public SettingsForm(Form1 owner)
        {
            InitializeColorPresets();
            InitializeComponent();
            mainForm = owner;

            clockColor = mainForm.TimeColor;
            fontSize = mainForm.FontSize;
            showDate = mainForm.ShowDate;
            autostart = mainForm.IsInAutostart();
            showBackground = mainForm.ShowBackground;
            backgroundColor = mainForm.BackgroundColor;
            backgroundOpacity = mainForm.BackgroundOpacity;

            LoadSettings();

            this.Icon = owner.Icon;
            this.TopMost = true;

            this.KeyPreview = true;
            this.KeyDown += SettingsForm_KeyDown;
        }

        private void SettingsForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                btnCancel_Click(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Enter && !e.Control && !e.Alt && !e.Shift)
            {
                if (!(this.ActiveControl is NumericUpDown))
                {
                    btnOK_Click(this, EventArgs.Empty);
                }
            }
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                int step = e.Shift ? LargeMoveStep : SmallMoveStep;
                MoveClockWindow(e.KeyCode, step);
            }
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            btnOK = new Button();
            btnCancel = new Button();
            pnlLayout = new Panel();
            gbGeneral = new GroupBox();
            chkAutostart = new CheckBox();
            btnReset = new Button();

            // Neue Gruppe für Hintergrund
            gbBackground = new GroupBox();
            chkShowBackground = new CheckBox();
            pnlBackgroundColorPreview = new Panel();
            btnBackgroundColor = new Button();
            trkBackgroundOpacity = new TrackBar();
            lblBackgroundOpacity = new Label();

            gbPresets = new GroupBox();
            lblFontSize = new Label();
            lblPresets = new Label();
            cmbColorPresets = new ComboBox();
            chkShowDate = new CheckBox();
            numFontSize = new NumericUpDown();
            pnlColorPreview = new Panel();
            toolTip = new ToolTip(components);
            lblColor = new Label();
            pnlLayout.SuspendLayout();
            gbGeneral.SuspendLayout();
            gbBackground.SuspendLayout();
            gbPresets.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numFontSize).BeginInit();
            SuspendLayout();
            //
            // btnOK
            //
            btnOK.Location = new Point(216, 470);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 28);
            btnOK.TabIndex = 0;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            //
            // btnCancel
            //
            btnCancel.Location = new Point(297, 470);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 28);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            //
            // pnlLayout
            //
            pnlLayout.AutoScroll = true;
            pnlLayout.BackColor = Color.FromArgb(32, 32, 32);
            pnlLayout.Controls.Add(gbGeneral);
            pnlLayout.Controls.Add(gbBackground);
            pnlLayout.Controls.Add(gbPresets);
            pnlLayout.Dock = DockStyle.Fill;
            pnlLayout.ForeColor = Color.White;
            pnlLayout.Location = new Point(0, 0);
            pnlLayout.Name = "pnlLayout";
            pnlLayout.Padding = new Padding(10);
            pnlLayout.Size = new Size(384, 460);
            pnlLayout.TabIndex = 0;
            //
            // gbGeneral
            //
            gbGeneral.AutoSize = true;
            gbGeneral.Controls.Add(chkAutostart);
            gbGeneral.Controls.Add(btnReset);
            gbGeneral.Dock = DockStyle.Top;
            gbGeneral.ForeColor = Color.White;
            gbGeneral.Location = new Point(10, 111);
            gbGeneral.Name = "gbGeneral";
            gbGeneral.Padding = new Padding(10);
            gbGeneral.Size = new Size(364, 114);
            gbGeneral.TabIndex = 0;
            gbGeneral.TabStop = false;
            gbGeneral.Text = "General";
            //
            // chkAutostart
            //
            chkAutostart.AutoSize = true;
            chkAutostart.Location = new Point(20, 25);
            chkAutostart.Name = "chkAutostart";
            chkAutostart.Size = new Size(203, 19);
            chkAutostart.TabIndex = 0;
            chkAutostart.Text = "Start automatically with Windows";
            toolTip.SetToolTip(chkAutostart, "Start the Desktop Clock automatically with Windows");
            chkAutostart.CheckedChanged += chkAutostart_CheckedChanged;
            //
            // btnReset
            //
            btnReset.ForeColor = Color.Black;
            btnReset.Location = new Point(20, 55);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(310, 30);
            btnReset.TabIndex = 1;
            btnReset.Text = "Reset all settings";
            toolTip.SetToolTip(btnReset, "Reset all settings to default values");
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += btnReset_Click;
            //
            // gbBackground
            //
            gbBackground.AutoSize = true;
            gbBackground.Controls.Add(chkShowBackground);
            gbBackground.Controls.Add(pnlBackgroundColorPreview);
            gbBackground.Controls.Add(btnBackgroundColor);
            gbBackground.Controls.Add(trkBackgroundOpacity);
            gbBackground.Controls.Add(lblBackgroundOpacity);
            gbBackground.Dock = DockStyle.Top;
            gbBackground.ForeColor = Color.White;
            gbBackground.Location = new Point(10, 225);
            gbBackground.Name = "gbBackground";
            gbBackground.Padding = new Padding(10);
            gbBackground.Size = new Size(364, 140);
            gbBackground.TabIndex = 0;
            gbBackground.TabStop = false;
            gbBackground.Text = "Background";
            //
            // chkShowBackground
            //
            chkShowBackground.AutoSize = true;
            chkShowBackground.Location = new Point(20, 25);
            chkShowBackground.Name = "chkShowBackground";
            chkShowBackground.Size = new Size(203, 19);
            chkShowBackground.TabIndex = 0;
            chkShowBackground.Text = "Show Background";
            toolTip.SetToolTip(chkShowBackground, "Show the background color");
            chkShowBackground.CheckedChanged += chkShowBackground_CheckedChanged;
            //
            // pnlBackgroundColorPreview
            //
            pnlBackgroundColorPreview.BorderStyle = BorderStyle.FixedSingle;
            pnlBackgroundColorPreview.Location = new Point(20, 55);
            pnlBackgroundColorPreview.Name = "pnlBackgroundColorPreview";
            pnlBackgroundColorPreview.Size = new Size(50, 21);
            pnlBackgroundColorPreview.TabIndex = 2;
            pnlBackgroundColorPreview.Paint += pnlBackgroundColorPreview_Paint;
            //
            // btnBackgroundColor
            //
            btnBackgroundColor.ForeColor = Color.Black;
            btnBackgroundColor.Location = new Point(80, 55);
            btnBackgroundColor.Name = "btnBackgroundColor";
            btnBackgroundColor.Size = new Size(150, 23);
            btnBackgroundColor.TabIndex = 1;
            btnBackgroundColor.Text = "Change Background Color";
            toolTip.SetToolTip(btnBackgroundColor, "Change the background color");
            btnBackgroundColor.UseVisualStyleBackColor = true;
            btnBackgroundColor.Click += btnBackgroundColor_Click;
            //
            // trkBackgroundOpacity
            //
            trkBackgroundOpacity.Location = new Point(20, 85);
            trkBackgroundOpacity.Name = "trkBackgroundOpacity";
            trkBackgroundOpacity.Size = new Size(210, 45);
            trkBackgroundOpacity.TabIndex = 3;
            trkBackgroundOpacity.Minimum = 0;
            trkBackgroundOpacity.Maximum = 100;
            trkBackgroundOpacity.TickFrequency = 10;
            trkBackgroundOpacity.Value = 50; // Default value
            toolTip.SetToolTip(trkBackgroundOpacity, "Adjust the background opacity");
            trkBackgroundOpacity.Scroll += trkBackgroundOpacity_Scroll;
            //
            // lblBackgroundOpacity
            //
            lblBackgroundOpacity.AutoSize = true;
            lblBackgroundOpacity.Location = new Point(240, 85);
            lblBackgroundOpacity.Name = "lblBackgroundOpacity";
            lblBackgroundOpacity.Size = new Size(110, 15);
            lblBackgroundOpacity.TabIndex = 4;
            lblBackgroundOpacity.Text = "Transparency:";
            //
            // gbPresets
            //
            gbPresets.AutoSize = true;
            gbPresets.Controls.Add(lblFontSize);
            gbPresets.Controls.Add(lblPresets);
            gbPresets.Controls.Add(cmbColorPresets);
            gbPresets.Controls.Add(chkShowDate);
            gbPresets.Controls.Add(numFontSize);
            gbPresets.Controls.Add(pnlColorPreview);
            gbPresets.Dock = DockStyle.Top;
            gbPresets.ForeColor = Color.White;
            gbPresets.Location = new Point(10, 10);
            gbPresets.Margin = new Padding(0, 0, 0, 10);
            gbPresets.Name = "gbPresets";
            gbPresets.Padding = new Padding(10);
            gbPresets.Size = new Size(364, 101);
            gbPresets.TabIndex = 3;
            gbPresets.TabStop = false;
            gbPresets.Text = "Text Color";
            gbPresets.Enter += gbPresets_Enter;
            //
            // lblFontSize
            //
            lblFontSize.AutoSize = true;
            lblFontSize.Location = new Point(120, 56);
            lblFontSize.Name = "lblFontSize";
            lblFontSize.Size = new Size(56, 15);
            lblFontSize.TabIndex = 0;
            lblFontSize.Text = "Font size:";
            //
            // lblPresets
            //
            lblPresets.AutoSize = true;
            lblPresets.Location = new Point(20, 25);
            lblPresets.Name = "lblPresets";
            lblPresets.Size = new Size(89, 15);
            lblPresets.TabIndex = 0;
            lblPresets.Text = "Color Selection:";
            //
            // cmbColorPresets
            //
            cmbColorPresets.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbColorPresets.Location = new Point(120, 22);
            cmbColorPresets.Name = "cmbColorPresets";
            cmbColorPresets.Size = new Size(150, 23);
            cmbColorPresets.TabIndex = 1;
            toolTip.SetToolTip(cmbColorPresets, "Choose a color for the clock text");
            cmbColorPresets.SelectedIndexChanged += cmbColorPresets_SelectedIndexChanged;
            //
            // chkShowDate
            //
            chkShowDate.AutoSize = true;
            chkShowDate.Location = new Point(20, 52);
            chkShowDate.Name = "chkShowDate";
            chkShowDate.Size = new Size(81, 19);
            chkShowDate.TabIndex = 2;
            chkShowDate.Text = "Show date";
            toolTip.SetToolTip(chkShowDate, "Also display the current date");
            chkShowDate.CheckedChanged += chkShowDate_CheckedChanged;
            //
            // numFontSize
            //
            numFontSize.Location = new Point(280, 49);
            numFontSize.Maximum = new decimal(new int[] { 72, 0, 0, 0 });
            numFontSize.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
            numFontSize.Name = "numFontSize";
            numFontSize.Size = new Size(50, 23);
            numFontSize.TabIndex = 1;
            toolTip.SetToolTip(numFontSize, "Set the size of the clock display (8-72)");
            numFontSize.Value = new decimal(new int[] { 24, 0, 0, 0 });
            numFontSize.ValueChanged += numFontSize_ValueChanged;
            //
            // pnlColorPreview
            //
            pnlColorPreview.BorderStyle = BorderStyle.FixedSingle;
            pnlColorPreview.Location = new Point(280, 22);
            pnlColorPreview.Name = "pnlColorPreview";
            pnlColorPreview.Size = new Size(50, 21);
            pnlColorPreview.TabIndex = 2;
            pnlColorPreview.Paint += pnlColorPreview_Paint;
            //
            // toolTip
            //
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 1000;
            toolTip.ReshowDelay = 500;
            //
            // lblColor
            //
            lblColor.Location = new Point(0, 0);
            lblColor.Name = "lblColor";
            lblColor.Size = new Size(100, 23);
            lblColor.TabIndex = 0;
            //
            // SettingsForm
            //
            AcceptButton = btnOK;
            CancelButton = btnCancel;
            ClientSize = new Size(384, 460);
            Controls.Add(pnlLayout);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Desktop Clock Settings";
            FormClosing += SettingsForm_FormClosing;
            Load += SettingsForm_Load;
            pnlLayout.ResumeLayout(false);
            pnlLayout.PerformLayout();
            gbGeneral.ResumeLayout(false);
            gbGeneral.PerformLayout();
            gbBackground.ResumeLayout(false);
            gbBackground.PerformLayout();
            gbPresets.ResumeLayout(false);
            gbPresets.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numFontSize).EndInit();
            ResumeLayout(false);
        }

        private void LoadSettings()
        {
            // Farbvoreinstellungen zur ComboBox hinzufügen
            if (cmbColorPresets != null && cmbColorPresets.Items.Count == 0)
            {
                foreach (var preset in colorPresets.Keys)
                {
                    cmbColorPresets.Items.Add(preset);
                }
            }

            SelectColorPreset(clockColor);

            pnlColorPreview?.Invalidate();

            if (numFontSize != null)
                numFontSize.Value = fontSize;

            if (chkShowDate != null)
                chkShowDate.Checked = showDate;

            if (chkAutostart != null)
                chkAutostart.Checked = autostart;

            // Neue Hintergrundeinstellungen laden
            if (chkShowBackground != null)
                chkShowBackground.Checked = showBackground;

            if (trkBackgroundOpacity != null)
                trkBackgroundOpacity.Value = (int)(backgroundOpacity * 100);

            pnlBackgroundColorPreview?.Invalidate();
        }

        private void MoveClockWindow(Keys direction, int stepSize)
        {
            if (mainForm == null) return;

            Point newLocation = mainForm.Location;

            switch (direction)
            {
                case Keys.Up:
                    newLocation.Y -= stepSize;
                    break;
                case Keys.Down:
                    newLocation.Y += stepSize;
                    break;
                case Keys.Left:
                    newLocation.X -= stepSize;
                    break;
                case Keys.Right:
                    newLocation.X += stepSize;
                    break;
            }

            Rectangle screenBounds = Screen.FromControl(mainForm).WorkingArea;
            if (newLocation.X < screenBounds.Left - mainForm.Width / 2)
                newLocation.X = screenBounds.Left - mainForm.Width / 2;
            if (newLocation.X > screenBounds.Right - mainForm.Width / 2)
                newLocation.X = screenBounds.Right - mainForm.Width / 2;
            if (newLocation.Y < screenBounds.Top - mainForm.Height / 2)
                newLocation.Y = screenBounds.Top - mainForm.Height / 2;
            if (newLocation.Y > screenBounds.Bottom - mainForm.Height / 2)
                newLocation.Y = screenBounds.Bottom - mainForm.Height / 2;

            mainForm.Location = newLocation;
        }

        private void SelectColorPreset(Color targetColor)
        {
            if (cmbColorPresets == null) return;

            string foundPreset = null;
            foreach (var preset in colorPresets)
            {
                if (preset.Value.ToArgb() == targetColor.ToArgb())
                {
                    foundPreset = preset.Key;
                    break;
                }
            }

            if (foundPreset != null)
            {
                cmbColorPresets.SelectedItem = foundPreset;
            }
            else
            {
                if (!cmbColorPresets.Items.Contains("Custom"))
                {
                    cmbColorPresets.Items.Add("Custom");
                }
                cmbColorPresets.SelectedItem = "Custom";
            }
        }

        private void cmbColorPresets_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? selectedPreset = cmbColorPresets?.SelectedItem?.ToString();

            if (!string.IsNullOrEmpty(selectedPreset) && colorPresets.TryGetValue(selectedPreset, out Color presetColor))
            {
                clockColor = presetColor;
                pnlColorPreview?.Invalidate();

                if (mainForm != null)
                {
                    mainForm.TimeColor = clockColor;
                }
            }
        }

        private void btnChangeColor_Click(object? sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = clockColor;
                colorDialog.FullOpen = true;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    clockColor = colorDialog.Color;
                    SelectColorPreset(clockColor);
                    pnlColorPreview?.Invalidate();

                    if (mainForm != null)
                    {
                        mainForm.TimeColor = clockColor;
                    }
                }
            }
        }

        private void pnlColorPreview_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is Panel panel)
            {
                e.Graphics.FillRectangle(new SolidBrush(clockColor), 0, 0, panel.Width, panel.Height);
            }
        }

        private void numFontSize_ValueChanged(object? sender, EventArgs e)
        {
            if (numFontSize != null)
            {
                fontSize = (int)numFontSize.Value;

                if (mainForm != null)
                {
                    mainForm.FontSize = fontSize;
                }
            }
        }

        private void chkShowDate_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkShowDate != null)
            {
                showDate = chkShowDate.Checked;

                if (mainForm != null)
                {
                    mainForm.ShowDate = showDate;
                }
            }
        }

        private void chkAutostart_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkAutostart != null)
            {
                autostart = chkAutostart.Checked;
            }
        }

        private void btnSaveCurrentPosition_Click(object? sender, EventArgs e)
        {
            mainForm?.SavePosition();
            MessageBox.Show(this,
                "The current position has been saved.",
                "Position saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnResetPosition_Click(object? sender, EventArgs e)
        {
            mainForm?.ResetPosition();
            MessageBox.Show(this,
                "The position has been reset to the center of the screen.",
                "Position reset",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnReset_Click(object? sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(this,
                "Do you really want to reset all settings to default values?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                clockColor = Color.White;
                fontSize = 24;
                showDate = false;
                autostart = false;

                LoadSettings();

                MessageBox.Show(this,
                    "All settings have been reset to default values.",
                    "Reset complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void btnOK_Click(object? sender, EventArgs e)
        {
            ApplySettings();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && DialogResult != DialogResult.OK && DialogResult != DialogResult.Cancel)
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        private void ApplySettings()
        {
            if (mainForm != null)
            {
                mainForm.TimeColor = clockColor;
                mainForm.FontSize = fontSize;
                mainForm.ShowDate = showDate;
                mainForm.ShowBackground = showBackground;
                mainForm.BackgroundColor = backgroundColor;
                mainForm.SetAutostart(autostart);

                mainForm.ApplySettings();
            }
        }

        private void InitializeColorPresets()
        {
            colorPresets = new Dictionary<string, Color>
            {
                {"White", Color.White},
                {"Red", Color.Red},
                {"Green", Color.Green},
                {"Blue", Color.Blue},
                {"Yellow", Color.Yellow},
                {"Orange", Color.Orange},
                {"Purple", Color.Purple},
                {"Pink", Color.Pink},
                {"Cyan", Color.Cyan},
                {"Magenta", Color.Magenta},
                {"Light Blue", Color.LightBlue},
                {"Light Green", Color.LightGreen},
                {"Light Yellow", Color.LightYellow},
                {"Gray", Color.Gray},
            };

            colorPresets = colorPresets.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            MoveClockWindow(Keys.Up, SmallMoveStep);
        }

        private void btnMoveLeft_Click(object sender, EventArgs e)
        {
            MoveClockWindow(Keys.Left, SmallMoveStep);
        }

        private void btnMoveRight_Click(object sender, EventArgs e)
        {
            MoveClockWindow(Keys.Right, SmallMoveStep);
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            MoveClockWindow(Keys.Down, SmallMoveStep);
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {

        }

        private void gbPresets_Enter(object sender, EventArgs e)
        {

        }

        private void chkShowBackground_CheckedChanged(object? sender, EventArgs e)
        {
            if (chkShowBackground != null)
            {
                showBackground = chkShowBackground.Checked;

                if (mainForm != null)
                {
                    mainForm.ShowBackground = showBackground;
                }
            }
        }

        private void pnlBackgroundColorPreview_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is Panel panel)
            {
                e.Graphics.FillRectangle(new SolidBrush(backgroundColor), 0, 0, panel.Width, panel.Height);
            }
        }

        private void btnBackgroundColor_Click(object? sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = backgroundColor;
                colorDialog.FullOpen = true;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    backgroundColor = colorDialog.Color;
                    pnlBackgroundColorPreview?.Invalidate();

                    if (mainForm != null)
                    {
                        mainForm.BackgroundColor = backgroundColor;
                    }
                }
            }
        }

        private void trkBackgroundOpacity_Scroll(object? sender, EventArgs e)
        {
            if (trkBackgroundOpacity != null)
            {
                // Calculate opacity value from slider (0.1 to 1.0)
                backgroundOpacity = trkBackgroundOpacity.Value / 100.0;

                // Update label to show percentage
                if (lblBackgroundOpacity != null)
                {
                    lblBackgroundOpacity.Text = $"Transparency: {trkBackgroundOpacity.Value}%";
                }

                // Update the form's background opacity (text stays 100% opaque)
                if (mainForm != null)
                {
                    mainForm.BackgroundOpacity = backgroundOpacity;
                }
            }
        }
    }
}
