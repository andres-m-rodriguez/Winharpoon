namespace Winharpoon;

public class MarksOverlay : Form
{
    private readonly HarpoonState _state;
    private readonly ListBox _listBox;
    private readonly Label _titleLabel;
    private readonly Label _hintLabel;

    public MarksOverlay(HarpoonState state)
    {
        _state = state;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 30);
        Size = new Size(400, 350);
        TopMost = true;
        ShowInTaskbar = false;
        KeyPreview = true;

        _titleLabel = new Label
        {
            Text = "Winharpoon Marks",
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = false,
            Size = new Size(380, 30),
            Location = new Point(10, 10),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(_titleLabel);

        _listBox = new ListBox
        {
            Location = new Point(10, 50),
            Size = new Size(380, 250),
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Consolas", 11),
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 28
        };
        _listBox.DrawItem += ListBox_DrawItem;
        _listBox.KeyDown += ListBox_KeyDown;
        _listBox.DoubleClick += ListBox_DoubleClick;
        Controls.Add(_listBox);

        _hintLabel = new Label
        {
            Text = "Press 1-9 to switch • Enter to select • Esc to close • Del to remove",
            ForeColor = Color.FromArgb(120, 120, 120),
            Font = new Font("Segoe UI", 9),
            AutoSize = false,
            Size = new Size(380, 25),
            Location = new Point(10, 310),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(_hintLabel);

        KeyDown += Overlay_KeyDown;
        Deactivate += (_, _) => Hide();

        RefreshMarks();
    }

    public void RefreshMarks()
    {
        _listBox.Items.Clear();

        var marks = _state.GetMarks();
        if (marks.Count == 0)
        {
            _listBox.Items.Add("No windows marked");
            return;
        }

        for (int i = 1; i <= 9; i++)
        {
            if (marks.TryGetValue(i, out var mark))
            {
                string procName = Truncate(mark.ProcessName, 18);
                string title = Truncate(mark.TitlePattern, 25);
                string display = $"[{i}]  {procName,-18} {title}";
                _listBox.Items.Add(new MarkItem(i, display, mark));
            }
        }

        if (_listBox.Items.Count > 0)
            _listBox.SelectedIndex = 0;
    }

    public void ShowOverlay()
    {
        RefreshMarks();
        Show();
        Activate();
        _listBox.Focus();
    }

    private void ListBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        e.DrawBackground();

        bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        Color bgColor = isSelected ? Color.FromArgb(60, 120, 180) : Color.FromArgb(45, 45, 45);
        Color textColor = Color.FromArgb(220, 220, 220);

        using var bgBrush = new SolidBrush(bgColor);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        string text = _listBox.Items[e.Index]?.ToString() ?? "";
        using var textBrush = new SolidBrush(textColor);
        var textBounds = new RectangleF(e.Bounds.X + 5, e.Bounds.Y + 4, e.Bounds.Width - 10, e.Bounds.Height);
        e.Graphics.DrawString(text, e.Font!, textBrush, textBounds);
    }

    private void Overlay_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
        {
            int slot = e.KeyCode - Keys.D0;
            SwitchToSlot(slot);
            e.Handled = true;
        }
        else if (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9)
        {
            int slot = e.KeyCode - Keys.NumPad0;
            SwitchToSlot(slot);
            e.Handled = true;
        }
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && _listBox.SelectedItem is MarkItem item)
        {
            SwitchToSlot(item.Slot);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Delete && _listBox.SelectedItem is MarkItem deleteItem)
        {
            _state.ClearSlot(deleteItem.Slot);
            RefreshMarks();
            e.Handled = true;
        }
    }

    private void ListBox_DoubleClick(object? sender, EventArgs e)
    {
        if (_listBox.SelectedItem is MarkItem item)
        {
            SwitchToSlot(item.Slot);
        }
    }

    private void SwitchToSlot(int slot)
    {
        Hide();
        _state.SwitchToSlot(slot);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Hide();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return text[..(maxLength - 2)] + "..";
    }

    private class MarkItem
    {
        public int Slot { get; }
        public string Display { get; }
        public MarkedWindow Mark { get; }

        public MarkItem(int slot, string display, MarkedWindow mark)
        {
            Slot = slot;
            Display = display;
            Mark = mark;
        }

        public override string ToString() => Display;
    }
}
