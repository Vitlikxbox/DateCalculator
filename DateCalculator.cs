using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

class DateCalculator : Form
{
    static readonly CultureInfo Ru = new CultureInfo("ru-RU");

    DateTimePicker fromPicker, toPicker, basePicker, birthPicker;
    CheckBox includeEndBox;
    NumericUpDown amountBox;
    ComboBox unitBox, directionBox;
    Label betweenResult, betweenDetails, addResult, ageResult;

    DateCalculator()
    {
        Text = "DateCalculator";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(480, 340);
        Font = new Font("Segoe UI", 10);
        StartPosition = FormStartPosition.CenterScreen;

        var tabs = new TabControl { Location = new Point(12, 12), Size = new Size(456, 316) };
        Controls.Add(tabs);

        var between = new TabPage("Между датами");
        tabs.TabPages.Add(between);
        between.Controls.Add(new Label { Text = "С", Location = new Point(14, 22), AutoSize = true });
        fromPicker = Picker(between, 50, 18, DateTime.Today);
        between.Controls.Add(new Label { Text = "по", Location = new Point(14, 62), AutoSize = true });
        toPicker = Picker(between, 50, 58, new DateTime(DateTime.Today.Year, 12, 31));
        Small(between, "Сегодня", 270, 17, (s, e) => fromPicker.Value = DateTime.Today);
        Small(between, "Новый год", 270, 57, (s, e) => toPicker.Value = new DateTime(DateTime.Today.Year + 1, 1, 1));
        includeEndBox = new CheckBox { Text = "Включая последний день", Location = new Point(14, 98), AutoSize = true };
        includeEndBox.CheckedChanged += (s, e) => UpdateBetween();
        between.Controls.Add(includeEndBox);
        betweenResult = Result(between, 132);
        betweenDetails = new Label { Location = new Point(14, 184), Size = new Size(420, 120), ForeColor = Color.DimGray };
        between.Controls.Add(betweenDetails);
        fromPicker.ValueChanged += (s, e) => UpdateBetween();
        toPicker.ValueChanged += (s, e) => UpdateBetween();

        var add = new TabPage("Прибавить срок");
        tabs.TabPages.Add(add);
        add.Controls.Add(new Label { Text = "От даты", Location = new Point(14, 22), AutoSize = true });
        basePicker = Picker(add, 90, 18, DateTime.Today);
        Small(add, "Сегодня", 310, 17, (s, e) => basePicker.Value = DateTime.Today);
        directionBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(14, 62), Width = 100 };
        directionBox.Items.AddRange(new object[] { "плюс", "минус" });
        directionBox.SelectedIndex = 0;
        add.Controls.Add(directionBox);
        amountBox = new NumericUpDown { Location = new Point(122, 62), Width = 80, Maximum = 100000, Value = 90 };
        add.Controls.Add(amountBox);
        unitBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(210, 62), Width = 170 };
        unitBox.Items.AddRange(new object[] { "календарных дней", "рабочих дней", "недель", "месяцев", "лет" });
        unitBox.SelectedIndex = 0;
        add.Controls.Add(unitBox);
        addResult = Result(add, 110);
        add.Controls.Add(new Label { Location = new Point(14, 170), Size = new Size(420, 60), ForeColor = Color.Gray, Text = "Рабочие дни считаются как пн–пт, праздники не учитываются. Если месяц короче, берётся его последний день (31 января + 1 месяц = 28 или 29 февраля)" });
        EventHandler updateAdd = (s, e) => UpdateAdd();
        basePicker.ValueChanged += updateAdd;
        directionBox.SelectedIndexChanged += updateAdd;
        amountBox.ValueChanged += updateAdd;
        unitBox.SelectedIndexChanged += updateAdd;

        var age = new TabPage("Возраст");
        tabs.TabPages.Add(age);
        age.Controls.Add(new Label { Text = "Дата рождения", Location = new Point(14, 22), AutoSize = true });
        birthPicker = Picker(age, 130, 18, new DateTime(1995, 6, 15));
        birthPicker.ValueChanged += (s, e) => UpdateAge();
        ageResult = new Label { Location = new Point(14, 60), Size = new Size(420, 220), Font = new Font("Segoe UI", 11) };
        age.Controls.Add(ageResult);

        UpdateBetween();
        UpdateAdd();
        UpdateAge();
    }

    static DateTimePicker Picker(Control parent, int x, int y, DateTime value)
    {
        var p = new DateTimePicker { Location = new Point(x, y), Width = 210, Format = DateTimePickerFormat.Long, Value = value };
        parent.Controls.Add(p);
        return p;
    }

    static Button Small(Control parent, string text, int x, int y, EventHandler click)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(100, 29) };
        b.Click += click;
        parent.Controls.Add(b);
        return b;
    }

    static Label Result(Control parent, int y)
    {
        var l = new Label { Location = new Point(14, y), Size = new Size(420, 44), Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(40, 90, 160) };
        parent.Controls.Add(l);
        return l;
    }

    void UpdateBetween()
    {
        DateTime a = fromPicker.Value.Date, b = toPicker.Value.Date;
        bool reversed = b < a;
        if (reversed) { var t = a; a = b; b = t; }
        int days = (b - a).Days + (includeEndBox.Checked ? 1 : 0);
        DateTime end = includeEndBox.Checked ? b.AddDays(1) : b;
        int workdays = WorkDays(a, end);

        int y, m, d;
        Diff(a, end, out y, out m, out d);
        betweenResult.Text = days.ToString("N0", Ru) + " " + Plural(days, "день", "дня", "дней") + (reversed ? " назад" : "");
        betweenDetails.Text =
            "= " + Ymd(y, m, d) + "\n" +
            "= " + (days / 7) + " " + Plural(days / 7, "неделя", "недели", "недель") + (days % 7 > 0 ? " и " + days % 7 + " " + Plural(days % 7, "день", "дня", "дней") : "") + "\n" +
            "Рабочих дней (пн–пт): " + workdays + ", выходных: " + (days - workdays) + "\n" +
            "Часов: " + ((long)days * 24).ToString("N0", Ru) + ", минут: " + ((long)days * 1440).ToString("N0", Ru) + "\n\n" +
            Capitalize(a.ToString("dddd", Ru)) + " → " + b.ToString("dddd", Ru);
    }

    void UpdateAdd()
    {
        DateTime start = basePicker.Value.Date;
        int n = (int)amountBox.Value * (directionBox.SelectedIndex == 0 ? 1 : -1);
        DateTime result;
        try
        {
            switch (unitBox.SelectedIndex)
            {
                case 0: result = start.AddDays(n); break;
                case 1: result = AddWorkDays(start, n); break;
                case 2: result = start.AddDays(n * 7); break;
                case 3: result = start.AddMonths(n); break;
                default: result = start.AddYears(n); break;
            }
        }
        catch (ArgumentOutOfRangeException) { addResult.Text = "Слишком далеко"; return; }
        addResult.Text = result.ToString("d MMMM yyyy", Ru) + ", " + result.ToString("ddd", Ru);
    }

    void UpdateAge()
    {
        DateTime birth = birthPicker.Value.Date, today = DateTime.Today;
        if (birth > today) { ageResult.Text = "Дата рождения в будущем"; return; }
        int y, m, d;
        Diff(birth, today, out y, out m, out d);
        DateTime next = birth.AddYears(y + 1);
        int toBirthday = (next - today).Days;
        int days = (today - birth).Days;
        ageResult.Text =
            "Возраст: " + Ymd(y, m, d) + "\n\n" +
            "Прожито дней: " + days.ToString("N0", Ru) + "\n" +
            "Недель: " + (days / 7).ToString("N0", Ru) + "\n" +
            "Часов: " + ((long)days * 24).ToString("N0", Ru) + "\n\n" +
            (toBirthday == 0 || (m == 0 && d == 0) ? "🎉 Сегодня день рождения!" :
             "До дня рождения: " + toBirthday + " " + Plural(toBirthday, "день", "дня", "дней") + " (" + next.ToString("d MMMM, dddd", Ru) + ")") + "\n" +
            "Родился(ась) в " + birth.ToString("dddd", Ru) + "\n" +
            "10 000-й день жизни: " + birth.AddDays(10000).ToString("d MMMM yyyy", Ru);
    }

    static void Diff(DateTime a, DateTime b, out int years, out int months, out int days)
    {
        int total = (b.Year - a.Year) * 12 + b.Month - a.Month;
        if (a.AddMonths(total) > b) total--;
        years = total / 12;
        months = total % 12;
        days = (b - a.AddMonths(total)).Days;
    }

    static string Ymd(int y, int m, int d)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (y > 0) parts.Add(y + " " + Plural(y, "год", "года", "лет"));
        if (m > 0) parts.Add(m + " " + Plural(m, "месяц", "месяца", "месяцев"));
        if (d > 0 || parts.Count == 0) parts.Add(d + " " + Plural(d, "день", "дня", "дней"));
        return string.Join(" ", parts);
    }

    static int WorkDays(DateTime from, DateTime toExclusive)
    {
        int total = (toExclusive - from).Days;
        if (total <= 0) return 0;
        int weeks = total / 7, result = weeks * 5;
        for (var d = from.AddDays(weeks * 7); d < toExclusive; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) result++;
        return result;
    }

    static DateTime AddWorkDays(DateTime start, int n)
    {
        int step = n >= 0 ? 1 : -1;
        var d = start;
        for (int left = Math.Abs(n); left > 0;)
        {
            d = d.AddDays(step);
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) left--;
        }
        return d;
    }

    static string Plural(int n, string one, string few, string many)
    {
        n = Math.Abs(n);
        int m10 = n % 10, m100 = n % 100;
        if (m10 == 1 && m100 != 11) return one;
        if (m10 >= 2 && m10 <= 4 && (m100 < 12 || m100 > 14)) return few;
        return many;
    }

    static string Capitalize(string s)
    {
        return s.Length == 0 ? s : char.ToUpper(s[0]) + s.Substring(1);
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new DateCalculator());
    }
}
