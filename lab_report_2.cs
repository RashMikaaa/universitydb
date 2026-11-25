using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace labreport02
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private const string ConnectionString =
            @"Server=localhost\SQLEXPRESS;Database=UniversityDB;Integrated Security=True;TrustServerCertificate=True;";

        private readonly DataSet _ds = new DataSet("UniversityDBSet");

        // Summary / Main Page controls
        private DataGridView _dgvSummary = null!;
        private Panel _pnlChart = null!;
        private Button _btnExportTxt = null!;
        private Button _btnRefreshAll = null!;

        // CRUD + Search + Sort controls
        private ComboBox _cmbTable = null!;
        private BindingSource _bsCurrent = null!;
        private BindingNavigator _bnCurrent = null!;
        private DataGridView _dgvData = null!;

        private TextBox _txtSearch1 = null!;
        private ComboBox _cmbField1 = null!;
        private TextBox _txtSearch2 = null!;
        private ComboBox _cmbField2 = null!;
        private ComboBox _cmbSortField = null!;
        private ComboBox _cmbSortDir = null!;
        private Button _btnApplyFilter = null!;
        private Button _btnClearFilter = null!;

        // Editor inputs
        private Panel _pnlEditor = null!;
        private TextBox _txtFacName = null!;
        private TextBox _txtSpecName = null!;
        private ComboBox _cmbSpecFaculty = null!;
        private TextBox _txtFirstName = null!;
        private TextBox _txtLastName = null!;
        private TextBox _txtAge = null!;
        private ComboBox _cmbGender = null!;
        private TextBox _txtCourseYear = null!;
        private ComboBox _cmbStudSpecialty = null!;
        private Button _btnSaveChanges = null!;

        public MainForm()
        {
            Text = "Lab 2 - OODP DataSet, BindingSource, Search/Sort & Report";
            Size = new Size(1220, 780);
            StartPosition = FormStartPosition.CenterScreen;

            BuildUi();
            Load += (_, _) => LoadAllData();
        }

        private void BuildUi()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };

            // ==========================================
            // TAB 1: Main Page (Summary, Chart, .txt Export)
            // ==========================================
            var tabMain = new TabPage("Main Page (Summary & Chart)");

            var topBar = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(8) };
            _btnRefreshAll = new Button { Text = "Refresh Data", Location = new Point(10, 10), Width = 110 };
            _btnExportTxt = new Button { Text = "Save Report (.txt)", Location = new Point(130, 10), Width = 135 };
            _btnRefreshAll.Click += (_, _) => LoadAllData();
            _btnExportTxt.Click += (_, _) => ExportSummaryToTxt();
            topBar.Controls.AddRange(new Control[] { _btnRefreshAll, _btnExportTxt });

            var splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };

            _dgvSummary = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _pnlChart = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _pnlChart.Paint += DrawSummaryChart;

            splitMain.Panel1.Controls.Add(_dgvSummary);
            splitMain.Panel2.Controls.Add(_pnlChart);

            tabMain.Controls.Add(splitMain);
            tabMain.Controls.Add(topBar);

            // ==========================================
            // TAB 2: CRUD, Search (2 fields), Sort
            // ==========================================
            var tabCrud = new TabPage("Data Management (CRUD, Search & Sort)");

            var controlTop = new Panel { Dock = DockStyle.Top, Height = 82, Padding = new Padding(8) };

            controlTop.Controls.Add(new Label { Text = "Table:", Location = new Point(10, 12), AutoSize = true });
            _cmbTable = new ComboBox
            {
                Location = new Point(58, 9),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbTable.Items.AddRange(new object[] { "Faculties", "Specialties", "Students" });
            _cmbTable.SelectedIndex = 2; // Students by default
            _cmbTable.SelectedIndexChanged += (_, _) => SwitchActiveTable();

            controlTop.Controls.Add(new Label { Text = "Search 1:", Location = new Point(215, 12), AutoSize = true });
            _cmbField1 = new ComboBox { Location = new Point(275, 9), Width = 115, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtSearch1 = new TextBox { Location = new Point(395, 9), Width = 120 };

            controlTop.Controls.Add(new Label { Text = "Search 2:", Location = new Point(530, 12), AutoSize = true });
            _cmbField2 = new ComboBox { Location = new Point(590, 9), Width = 115, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtSearch2 = new TextBox { Location = new Point(710, 9), Width = 120 };

            _btnApplyFilter = new Button { Text = "Search", Location = new Point(845, 8), Width = 75 };
            _btnClearFilter = new Button { Text = "Reset", Location = new Point(926, 8), Width = 70 };
            _btnApplyFilter.Click += (_, _) => ApplySearchAndSort();
            _btnClearFilter.Click += (_, _) => ResetSearchAndSort();

            controlTop.Controls.Add(new Label { Text = "Sort By:", Location = new Point(215, 46), AutoSize = true });
            _cmbSortField = new ComboBox { Location = new Point(275, 43), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbSortDir = new ComboBox { Location = new Point(422, 43), Width = 93, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbSortDir.Items.AddRange(new object[] { "ASC", "DESC" });
            _cmbSortDir.SelectedIndex = 0;

            var btnApplySort = new Button { Text = "Sort", Location = new Point(525, 42), Width = 70 };
            btnApplySort.Click += (_, _) => ApplySearchAndSort();

            controlTop.Controls.AddRange(new Control[]
            {
                _cmbTable, _cmbField1, _txtSearch1, _cmbField2, _txtSearch2,
                _btnApplyFilter, _btnClearFilter, _cmbSortField, _cmbSortDir, btnApplySort
            });

            _bsCurrent = new BindingSource();
            _bnCurrent = new BindingNavigator(true)
            {
                Dock = DockStyle.Top,
                BindingSource = _bsCurrent
            };

            _dgvData = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = _bsCurrent,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            _dgvData.SelectionChanged += (_, _) => LoadSelectedRowToEditor();

            _pnlEditor = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 135,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };
            BuildEditorControls();

            tabCrud.Controls.Add(_dgvData);
            tabCrud.Controls.Add(_bnCurrent);
            tabCrud.Controls.Add(controlTop);
            tabCrud.Controls.Add(_pnlEditor);

            tabs.TabPages.Add(tabMain);
            tabs.TabPages.Add(tabCrud);
            Controls.Add(tabs);
        }

        private void BuildEditorControls()
        {
            // Faculties editor
            _txtFacName = new TextBox { Location = new Point(120, 14), Width = 240 };

            // Specialties editor
            _txtSpecName = new TextBox { Location = new Point(120, 14), Width = 220 };
            _cmbSpecFaculty = new ComboBox
            {
                Location = new Point(460, 14),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Students editor
            _txtFirstName = new TextBox { Location = new Point(95, 12), Width = 140 };
            _txtLastName = new TextBox { Location = new Point(325, 12), Width = 140 };
            _txtAge = new TextBox { Location = new Point(525, 12), Width = 65 };
            _cmbGender = new ComboBox
            {
                Location = new Point(665, 12),
                Width = 95,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbGender.Items.AddRange(new object[] { "Male", "Female" });

            _txtCourseYear = new TextBox { Location = new Point(95, 48), Width = 65 };
            _cmbStudSpecialty = new ComboBox
            {
                Location = new Point(255, 48),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _btnSaveChanges = new Button
            {
                Text = "Save / Apply Row to DB",
                Location = new Point(820, 15),
                Width = 185,
                Height = 45
            };
            _btnSaveChanges.Click += (_, _) => SaveCurrentTableToDatabase();

            _pnlEditor.Controls.Add(_btnSaveChanges);
        }

        private void LoadAllData()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                _ds.Tables.Clear();

                FillTable(conn, "Faculties", "SELECT FacultyID, FacultyName FROM Faculties");
                FillTable(conn, "Specialties", "SELECT SpecialtyID, SpecialtyName, FacultyID FROM Specialties");
                FillTable(conn, "Students", "SELECT StudentID, FirstName, LastName, Age, Gender, CourseYear, SpecialtyID FROM Students");

                // Bind foreign key combo boxes
                _cmbSpecFaculty.DataSource = _ds.Tables["Faculties"]!.Copy();
                _cmbSpecFaculty.DisplayMember = "FacultyName";
                _cmbSpecFaculty.ValueMember = "FacultyID";

                _cmbStudSpecialty.DataSource = _ds.Tables["Specialties"]!.Copy();
                _cmbStudSpecialty.DisplayMember = "SpecialtyName";
                _cmbStudSpecialty.ValueMember = "SpecialtyID";

                SwitchActiveTable();
                BuildSummaryReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FillTable(SqlConnection conn, string tableName, string query)
        {
            using var adapter = new SqlDataAdapter(query, conn);
            var dt = new DataTable(tableName);
            adapter.Fill(dt);
            if (dt.Columns.Count > 0)
            {
                dt.PrimaryKey = new[] { dt.Columns[0] };
            }
            _ds.Tables.Add(dt);
        }

        private void SwitchActiveTable()
        {
            string? tableName = _cmbTable.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(tableName) || !_ds.Tables.Contains(tableName))
                return;

            var dt = _ds.Tables[tableName]!;
            _bsCurrent.DataSource = dt;
            _bsCurrent.Filter = null;
            _bsCurrent.Sort = null;

            string[] fields = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            _cmbField1.Items.Clear();
            _cmbField2.Items.Clear();
            _cmbSortField.Items.Clear();

            _cmbField1.Items.AddRange(fields);
            _cmbField2.Items.AddRange(fields);
            _cmbSortField.Items.AddRange(fields);

            if (fields.Length > 0)
            {
                _cmbField1.SelectedIndex = 0;
                _cmbField2.SelectedIndex = fields.Length > 1 ? 1 : 0;
                _cmbSortField.SelectedIndex = 0;
            }

            ConfigureEditorForTable(tableName);
            LoadSelectedRowToEditor();
        }

        private void ConfigureEditorForTable(string tableName)
        {
            foreach (Control ctrl in _pnlEditor.Controls.Cast<Control>().ToList())
            {
                if (ctrl != _btnSaveChanges)
                    _pnlEditor.Controls.Remove(ctrl);
            }

            if (tableName == "Faculties")
            {
                _pnlEditor.Controls.Add(new Label { Text = "Faculty Name:", Location = new Point(15, 17), AutoSize = true });
                _pnlEditor.Controls.Add(_txtFacName);
            }
            else if (tableName == "Specialties")
            {
                _pnlEditor.Controls.Add(new Label { Text = "Specialty Name:", Location = new Point(15, 17), AutoSize = true });
                _pnlEditor.Controls.Add(_txtSpecName);
                _pnlEditor.Controls.Add(new Label { Text = "Faculty:", Location = new Point(385, 17), AutoSize = true });
                _pnlEditor.Controls.Add(_cmbSpecFaculty);
            }
            else if (tableName == "Students")
            {
                _pnlEditor.Controls.Add(new Label { Text = "First Name:", Location = new Point(15, 15), AutoSize = true });
                _pnlEditor.Controls.Add(_txtFirstName);
                _pnlEditor.Controls.Add(new Label { Text = "Last Name:", Location = new Point(248, 15), AutoSize = true });
                _pnlEditor.Controls.Add(_txtLastName);
                _pnlEditor.Controls.Add(new Label { Text = "Age:", Location = new Point(485, 15), AutoSize = true });
                _pnlEditor.Controls.Add(_txtAge);
                _pnlEditor.Controls.Add(new Label { Text = "Gender:", Location = new Point(608, 15), AutoSize = true });
                _pnlEditor.Controls.Add(_cmbGender);

                _pnlEditor.Controls.Add(new Label { Text = "Year (1-5):", Location = new Point(15, 51), AutoSize = true });
                _pnlEditor.Controls.Add(_txtCourseYear);
                _pnlEditor.Controls.Add(new Label { Text = "Specialty:", Location = new Point(185, 51), AutoSize = true });
                _pnlEditor.Controls.Add(_cmbStudSpecialty);
            }
        }

        private void LoadSelectedRowToEditor()
        {
            if (_bsCurrent.Current is not DataRowView row) return;
            string? tableName = _cmbTable.SelectedItem?.ToString();

            if (tableName == "Faculties")
            {
                _txtFacName.Text = row["FacultyName"]?.ToString() ?? "";
            }
            else if (tableName == "Specialties")
            {
                _txtSpecName.Text = row["SpecialtyName"]?.ToString() ?? "";
                if (row["FacultyID"] != DBNull.Value)
                    _cmbSpecFaculty.SelectedValue = Convert.ToInt32(row["FacultyID"]);
            }
            else if (tableName == "Students")
            {
                _txtFirstName.Text = row["FirstName"]?.ToString() ?? "";
                _txtLastName.Text = row["LastName"]?.ToString() ?? "";
                _txtAge.Text = row["Age"]?.ToString() ?? "";
                _cmbGender.SelectedItem = row["Gender"]?.ToString() ?? "Male";
                _txtCourseYear.Text = row["CourseYear"]?.ToString() ?? "";
                if (row["SpecialtyID"] != DBNull.Value)
                    _cmbStudSpecialty.SelectedValue = Convert.ToInt32(row["SpecialtyID"]);
            }
        }

        private void SaveCurrentTableToDatabase()
        {
            string? tableName = _cmbTable.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(tableName) || !_ds.Tables.Contains(tableName)) return;

            try
            {
                if (_bsCurrent.Current is DataRowView row)
                {
                    if (tableName == "Faculties")
                    {
                        if (string.IsNullOrWhiteSpace(_txtFacName.Text))
                        {
                            MessageBox.Show("Faculty Name is required.");
                            return;
                        }
                        row["FacultyName"] = _txtFacName.Text.Trim();
                    }
                    else if (tableName == "Specialties")
                    {
                        if (string.IsNullOrWhiteSpace(_txtSpecName.Text) || _cmbSpecFaculty.SelectedValue == null)
                        {
                            MessageBox.Show("Specialty Name and Faculty are required.");
                            return;
                        }
                        row["SpecialtyName"] = _txtSpecName.Text.Trim();
                        row["FacultyID"] = Convert.ToInt32(_cmbSpecFaculty.SelectedValue);
                    }
                    else if (tableName == "Students")
                    {
                        if (string.IsNullOrWhiteSpace(_txtFirstName.Text) ||
                            string.IsNullOrWhiteSpace(_txtLastName.Text) ||
                            !int.TryParse(_txtAge.Text.Trim(), out int age) ||
                            !int.TryParse(_txtCourseYear.Text.Trim(), out int year) ||
                            _cmbStudSpecialty.SelectedValue == null)
                        {
                            MessageBox.Show("Please enter valid student details (numeric Age and CourseYear 1-5).");
                            return;
                        }

                        row["FirstName"] = _txtFirstName.Text.Trim();
                        row["LastName"] = _txtLastName.Text.Trim();
                        row["Age"] = age;
                        row["Gender"] = _cmbGender.SelectedItem?.ToString() ?? "Male";
                        row["CourseYear"] = year;
                        row["SpecialtyID"] = Convert.ToInt32(_cmbStudSpecialty.SelectedValue);
                    }
                }

                _bsCurrent.EndEdit();

                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                string selectSql = tableName switch
                {
                    "Faculties" => "SELECT FacultyID, FacultyName FROM Faculties",
                    "Specialties" => "SELECT SpecialtyID, SpecialtyName, FacultyID FROM Specialties",
                    "Students" => "SELECT StudentID, FirstName, LastName, Age, Gender, CourseYear, SpecialtyID FROM Students",
                    _ => throw new InvalidOperationException("Unsupported table")
                };

                using var adapter = new SqlDataAdapter(selectSql, conn);
                using var builder = new SqlCommandBuilder(adapter);
                adapter.Update(_ds.Tables[tableName]!);

                MessageBox.Show($"{tableName} changes saved to UniversityDB!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadAllData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplySearchAndSort()
        {
            var filters = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(_txtSearch1.Text) && _cmbField1.SelectedItem != null)
            {
                string f1 = _cmbField1.SelectedItem.ToString()!;
                string val1 = _txtSearch1.Text.Trim().Replace("'", "''");
                filters.Add($"CONVERT([{f1}], 'System.String') LIKE '%{val1}%'");
            }

            if (!string.IsNullOrWhiteSpace(_txtSearch2.Text) && _cmbField2.SelectedItem != null)
            {
                string f2 = _cmbField2.SelectedItem.ToString()!;
                string val2 = _txtSearch2.Text.Trim().Replace("'", "''");
                filters.Add($"CONVERT([{f2}], 'System.String') LIKE '%{val2}%'");
            }

            _bsCurrent.Filter = filters.Count > 0 ? string.Join(" AND ", filters) : null;

            if (_cmbSortField.SelectedItem != null)
            {
                string sortCol = _cmbSortField.SelectedItem.ToString()!;
                string sortDir = _cmbSortDir.SelectedItem?.ToString() ?? "ASC";
                _bsCurrent.Sort = $"[{sortCol}] {sortDir}";
            }
        }

        private void ResetSearchAndSort()
        {
            _txtSearch1.Text = "";
            _txtSearch2.Text = "";
            _bsCurrent.Filter = null;
            _bsCurrent.Sort = null;
        }

        private void BuildSummaryReport()
        {
            if (!_ds.Tables.Contains("Faculties") ||
                !_ds.Tables.Contains("Specialties") ||
                !_ds.Tables.Contains("Students"))
                return;

            var dtFac = _ds.Tables["Faculties"]!;
            var dtSpec = _ds.Tables["Specialties"]!;
            var dtStud = _ds.Tables["Students"]!;

            var summaryTable = new DataTable("FacultySpecialtySummary");
            summaryTable.Columns.Add("Faculty", typeof(string));
            summaryTable.Columns.Add("Specialty", typeof(string));
            summaryTable.Columns.Add("Number of Students", typeof(int));
            summaryTable.Columns.Add("Average Age", typeof(double));

            var query =
                from f in dtFac.AsEnumerable()
                join sp in dtSpec.AsEnumerable()
                    on f.Field<int>("FacultyID") equals sp.Field<int>("FacultyID") into specGroup
                from sp in specGroup.DefaultIfEmpty()
                let specId = sp?.Field<int>("SpecialtyID") ?? -1
                let matchingStudents = dtStud.AsEnumerable().Where(st => st.Field<int>("SpecialtyID") == specId).ToList()
                let count = matchingStudents.Count
                let avgAge = count > 0 ? Math.Round(matchingStudents.Average(st => st.Field<int>("Age")), 1) : 0.0
                select new
                {
                    Faculty = f.Field<string>("FacultyName") ?? "",
                    Specialty = sp?.Field<string>("SpecialtyName") ?? "(No Specialties)",
                    Count = count,
                    AvgAge = avgAge
                };

            foreach (var item in query)
            {
                summaryTable.Rows.Add(item.Faculty, item.Specialty, item.Count, item.AvgAge);
            }

            _dgvSummary.DataSource = summaryTable;
            _pnlChart.Invalidate();
        }

        private void DrawSummaryChart(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            if (_dgvSummary.DataSource is not DataTable dt || dt.Rows.Count == 0)
                return;

            using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
            using var labelFont = new Font("Segoe UI", 8.5f);
            g.DrawString("Number of Students by Faculty & Specialty", titleFont, Brushes.Black, 15, 10);

            int maxCount = Math.Max(1, dt.AsEnumerable().Max(r => Convert.ToInt32(r["Number of Students"])));
            int leftMargin = 260;
            int topMargin = 42;
            int barHeight = 22;
            int spacing = 10;
            int usableWidth = Math.Max(120, _pnlChart.Width - leftMargin - 90);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int y = topMargin + i * (barHeight + spacing);
                if (y + barHeight > _pnlChart.Height - 10) break;

                string label = $"{dt.Rows[i]["Faculty"]} - {dt.Rows[i]["Specialty"]}";
                int count = Convert.ToInt32(dt.Rows[i]["Number of Students"]);
                double avgAge = Convert.ToDouble(dt.Rows[i]["Average Age"]);

                g.DrawString(label, labelFont, Brushes.Black, new RectangleF(12, y + 3, leftMargin - 20, barHeight));

                int barWidth = (int)((double)count / maxCount * usableWidth);
                g.FillRectangle(Brushes.SteelBlue, leftMargin, y, Math.Max(4, barWidth), barHeight);
                g.DrawRectangle(Pens.Navy, leftMargin, y, Math.Max(4, barWidth), barHeight);

                g.DrawString($"{count} (Avg: {avgAge})", labelFont, Brushes.DarkSlateGray, leftMargin + Math.Max(4, barWidth) + 8, y + 3);
            }
        }

        private void ExportSummaryToTxt()
        {
            if (_dgvSummary.DataSource is not DataTable dt || dt.Rows.Count == 0)
            {
                MessageBox.Show("No summary data to export.");
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                FileName = "Lab2_Faculty_Specialty_Report.txt"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            var sb = new StringBuilder();
            sb.AppendLine("UNIVERSITY ACADEMIC PROGRESS - LAB 2 SUMMARY REPORT");
            sb.AppendLine($"Generated: {DateTime.Now}");
            sb.AppendLine(new string('=', 65));

            var grouped = dt.AsEnumerable().GroupBy(r => r["Faculty"].ToString());
            foreach (var facGroup in grouped)
            {
                sb.AppendLine();
                sb.AppendLine($"Faculty: {facGroup.Key}");
                sb.AppendLine(new string('-', 65));
                sb.AppendLine($"{"Specialty",-35} {"Students",10} {"Avg Age",12}");

                foreach (var row in facGroup)
                {
                    string spec = row["Specialty"].ToString() ?? "";
                    int count = Convert.ToInt32(row["Number of Students"]);
                    double avg = Convert.ToDouble(row["Average Age"]);
                    sb.AppendLine($"{spec,-35} {count,10} {avg,12:F1}");
                }
            }

            File.WriteAllText(sfd.FileName, sb.ToString());
            MessageBox.Show("Report saved successfully!", "Export Complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}