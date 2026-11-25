using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Lab1_OODP_WinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class ComboItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public override string ToString() => Text;
    }

    public class MainForm : Form
    {
        private readonly string _connStr =
            @"Server=localhost\SQLEXPRESS;Database=UniversityDB;Integrated Security=True;TrustServerCertificate=True;";

        private TabControl tabs = new TabControl();

        // --- Faculties controls ---
        private DataGridView dgvFac = new DataGridView();
        private TextBox txtFacName = new TextBox();
        private int selectedFacId = 0;

        // --- Specialties controls ---
        private DataGridView dgvSpec = new DataGridView();
        private TextBox txtSpecName = new TextBox();
        private ComboBox cmbSpecFaculty = new ComboBox();
        private int selectedSpecId = 0;

        // --- Students controls ---
        private DataGridView dgvStu = new DataGridView();
        private TextBox txtFirst = new TextBox();
        private TextBox txtLast = new TextBox();
        private NumericUpDown numAge = new NumericUpDown { Minimum = 15, Maximum = 100, Value = 20 };
        private ComboBox cmbGender = new ComboBox();
        private NumericUpDown numCourse = new NumericUpDown { Minimum = 1, Maximum = 5, Value = 1 };
        private ComboBox cmbStuSpec = new ComboBox();
        private int selectedStuId = 0;

        public MainForm()
        {
            Text = "Lab 1 - OODP CRUD (SqlDataReader & SqlParameter)";
            Size = new Size(1050, 620);
            StartPosition = FormStartPosition.CenterScreen;

            tabs.Dock = DockStyle.Fill;
            tabs.SelectedIndexChanged += (s, e) => RefreshAll();

            BuildFacultiesTab();
            BuildSpecialtiesTab();
            BuildStudentsTab();

            Controls.Add(tabs);
            RefreshAll();
        }

        private void RefreshAll()
        {
            LoadFacultiesGridAndCombo();
            LoadSpecialtiesGridAndCombo();
            LoadStudentsGrid();
        }

        // ==========================================
        // 1. FACULTIES TAB
        // ==========================================
        private void BuildFacultiesTab()
        {
            TabPage page = new TabPage("Faculties");

            dgvFac.Location = new Point(20, 20);
            dgvFac.Size = new Size(980, 340);
            dgvFac.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFac.MultiSelect = false;
            dgvFac.ReadOnly = true;
            dgvFac.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvFac.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvFac.Rows[e.RowIndex].Cells["FacultyID"].Value != null)
                {
                    selectedFacId = Convert.ToInt32(dgvFac.Rows[e.RowIndex].Cells["FacultyID"].Value);
                    txtFacName.Text = dgvFac.Rows[e.RowIndex].Cells["FacultyName"].Value.ToString();
                }
            };

            page.Controls.Add(new Label { Text = "Faculty Name:", Location = new Point(20, 385), Size = new Size(100, 25) });
            txtFacName.Location = new Point(125, 382);
            txtFacName.Size = new Size(300, 28);

            Button btnAdd = new Button { Text = "Add", Location = new Point(20, 430), Size = new Size(95, 34) };
            Button btnEdit = new Button { Text = "Update", Location = new Point(125, 430), Size = new Size(95, 34) };
            Button btnDel = new Button { Text = "Delete", Location = new Point(230, 430), Size = new Size(95, 34) };
            Button btnClear = new Button { Text = "Clear", Location = new Point(335, 430), Size = new Size(90, 34) };

            btnAdd.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFacName.Text)) return;
                ExecNonQuery("INSERT INTO Faculties (FacultyName) VALUES (@Name)",
                    new SqlParameter("@Name", txtFacName.Text.Trim()));
                ClearFac();
                RefreshAll();
            };

            btnEdit.Click += (s, e) =>
            {
                if (selectedFacId == 0 || string.IsNullOrWhiteSpace(txtFacName.Text)) return;
                ExecNonQuery("UPDATE Faculties SET FacultyName = @Name WHERE FacultyID = @Id",
                    new SqlParameter("@Name", txtFacName.Text.Trim()),
                    new SqlParameter("@Id", selectedFacId));
                ClearFac();
                RefreshAll();
            };

            btnDel.Click += (s, e) =>
            {
                if (selectedFacId == 0) return;
                ExecNonQuery("DELETE FROM Faculties WHERE FacultyID = @Id",
                    new SqlParameter("@Id", selectedFacId));
                ClearFac();
                RefreshAll();
            };

            btnClear.Click += (s, e) => ClearFac();

            page.Controls.AddRange(new Control[] { dgvFac, txtFacName, btnAdd, btnEdit, btnDel, btnClear });
            tabs.TabPages.Add(page);
        }

        private void ClearFac()
        {
            selectedFacId = 0;
            txtFacName.Clear();
        }

        private void LoadFacultiesGridAndCombo()
        {
            DataTable dt = ReadWithDataReader("SELECT FacultyID, FacultyName FROM Faculties ORDER BY FacultyID");
            dgvFac.DataSource = dt;

            cmbSpecFaculty.Items.Clear();
            foreach (DataRow r in dt.Rows)
            {
                cmbSpecFaculty.Items.Add(new ComboItem
                {
                    Id = Convert.ToInt32(r["FacultyID"]),
                    Text = r["FacultyName"].ToString()
                });
            }
            if (cmbSpecFaculty.Items.Count > 0 && cmbSpecFaculty.SelectedIndex < 0)
                cmbSpecFaculty.SelectedIndex = 0;
        }

        // ==========================================
        // 2. SPECIALTIES TAB (FK -> Faculties ComboBox)
        // ==========================================
        private void BuildSpecialtiesTab()
        {
            TabPage page = new TabPage("Specialties");

            dgvSpec.Location = new Point(20, 20);
            dgvSpec.Size = new Size(980, 340);
            dgvSpec.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSpec.MultiSelect = false;
            dgvSpec.ReadOnly = true;
            dgvSpec.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSpec.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvSpec.Rows[e.RowIndex].Cells["SpecialtyID"].Value != null)
                {
                    selectedSpecId = Convert.ToInt32(dgvSpec.Rows[e.RowIndex].Cells["SpecialtyID"].Value);
                    txtSpecName.Text = dgvSpec.Rows[e.RowIndex].Cells["SpecialtyName"].Value.ToString();
                    int facId = Convert.ToInt32(dgvSpec.Rows[e.RowIndex].Cells["FacultyID"].Value);
                    SelectComboById(cmbSpecFaculty, facId);
                }
            };

            page.Controls.Add(new Label { Text = "Specialty:", Location = new Point(20, 385), Size = new Size(80, 25) });
            txtSpecName.Location = new Point(105, 382);
            txtSpecName.Size = new Size(240, 28);

            page.Controls.Add(new Label { Text = "Faculty:", Location = new Point(365, 385), Size = new Size(65, 25) });
            cmbSpecFaculty.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSpecFaculty.Location = new Point(435, 382);
            cmbSpecFaculty.Size = new Size(280, 28);

            Button btnAdd = new Button { Text = "Add", Location = new Point(20, 430), Size = new Size(95, 34) };
            Button btnEdit = new Button { Text = "Update", Location = new Point(125, 430), Size = new Size(95, 34) };
            Button btnDel = new Button { Text = "Delete", Location = new Point(230, 430), Size = new Size(95, 34) };
            Button btnClear = new Button { Text = "Clear", Location = new Point(335, 430), Size = new Size(90, 34) };

            btnAdd.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSpecName.Text) || !(cmbSpecFaculty.SelectedItem is ComboItem fac)) return;
                ExecNonQuery("INSERT INTO Specialties (SpecialtyName, FacultyID) VALUES (@Name, @FacId)",
                    new SqlParameter("@Name", txtSpecName.Text.Trim()),
                    new SqlParameter("@FacId", fac.Id));
                ClearSpec();
                RefreshAll();
            };

            btnEdit.Click += (s, e) =>
            {
                if (selectedSpecId == 0 || string.IsNullOrWhiteSpace(txtSpecName.Text) || !(cmbSpecFaculty.SelectedItem is ComboItem fac)) return;
                ExecNonQuery("UPDATE Specialties SET SpecialtyName = @Name, FacultyID = @FacId WHERE SpecialtyID = @Id",
                    new SqlParameter("@Name", txtSpecName.Text.Trim()),
                    new SqlParameter("@FacId", fac.Id),
                    new SqlParameter("@Id", selectedSpecId));
                ClearSpec();
                RefreshAll();
            };

            btnDel.Click += (s, e) =>
            {
                if (selectedSpecId == 0) return;
                ExecNonQuery("DELETE FROM Specialties WHERE SpecialtyID = @Id",
                    new SqlParameter("@Id", selectedSpecId));
                ClearSpec();
                RefreshAll();
            };

            btnClear.Click += (s, e) => ClearSpec();

            page.Controls.AddRange(new Control[] { dgvSpec, txtSpecName, cmbSpecFaculty, btnAdd, btnEdit, btnDel, btnClear });
            tabs.TabPages.Add(page);
        }

        private void ClearSpec()
        {
            selectedSpecId = 0;
            txtSpecName.Clear();
            if (cmbSpecFaculty.Items.Count > 0) cmbSpecFaculty.SelectedIndex = 0;
        }

        private void LoadSpecialtiesGridAndCombo()
        {
            string sql = @"SELECT sp.SpecialtyID, sp.SpecialtyName, sp.FacultyID, f.FacultyName
                           FROM Specialties sp
                           JOIN Faculties f ON sp.FacultyID = f.FacultyID
                           ORDER BY sp.SpecialtyID";
            DataTable dt = ReadWithDataReader(sql);
            dgvSpec.DataSource = dt;

            cmbStuSpec.Items.Clear();
            foreach (DataRow r in dt.Rows)
            {
                cmbStuSpec.Items.Add(new ComboItem
                {
                    Id = Convert.ToInt32(r["SpecialtyID"]),
                    Text = r["SpecialtyName"].ToString()
                });
            }
            if (cmbStuSpec.Items.Count > 0 && cmbStuSpec.SelectedIndex < 0)
                cmbStuSpec.SelectedIndex = 0;
        }

        // ==========================================
        // 3. STUDENTS TAB (FK -> Specialties ComboBox)
        // ==========================================
        private void BuildStudentsTab()
        {
            TabPage page = new TabPage("Students");

            dgvStu.Location = new Point(20, 20);
            dgvStu.Size = new Size(980, 320);
            dgvStu.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvStu.MultiSelect = false;
            dgvStu.ReadOnly = true;
            dgvStu.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvStu.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvStu.Rows[e.RowIndex].Cells["StudentID"].Value != null)
                {
                    var row = dgvStu.Rows[e.RowIndex];
                    selectedStuId = Convert.ToInt32(row.Cells["StudentID"].Value);
                    txtFirst.Text = row.Cells["FirstName"].Value.ToString();
                    txtLast.Text = row.Cells["LastName"].Value.ToString();
                    numAge.Value = Convert.ToDecimal(row.Cells["Age"].Value);
                    cmbGender.Text = row.Cells["Gender"].Value.ToString();
                    numCourse.Value = Convert.ToDecimal(row.Cells["CourseYear"].Value);
                    SelectComboById(cmbStuSpec, Convert.ToInt32(row.Cells["SpecialtyID"].Value));
                }
            };

            page.Controls.Add(new Label { Text = "First Name:", Location = new Point(20, 360), Size = new Size(80, 25) });
            txtFirst.Location = new Point(105, 357);
            txtFirst.Size = new Size(160, 28);

            page.Controls.Add(new Label { Text = "Last Name:", Location = new Point(285, 360), Size = new Size(80, 25) });
            txtLast.Location = new Point(370, 357);
            txtLast.Size = new Size(160, 28);

            page.Controls.Add(new Label { Text = "Age:", Location = new Point(550, 360), Size = new Size(40, 25) });
            numAge.Location = new Point(595, 357);
            numAge.Size = new Size(70, 28);

            page.Controls.Add(new Label { Text = "Gender:", Location = new Point(685, 360), Size = new Size(55, 25) });
            cmbGender.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGender.Items.AddRange(new object[] { "Male", "Female" });
            cmbGender.SelectedIndex = 0;
            cmbGender.Location = new Point(745, 357);
            cmbGender.Size = new Size(110, 28);

            page.Controls.Add(new Label { Text = "Course:", Location = new Point(20, 400), Size = new Size(80, 25) });
            numCourse.Location = new Point(105, 397);
            numCourse.Size = new Size(70, 28);

            page.Controls.Add(new Label { Text = "Specialty:", Location = new Point(200, 400), Size = new Size(70, 25) });
            cmbStuSpec.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStuSpec.Location = new Point(275, 397);
            cmbStuSpec.Size = new Size(255, 28);

            Button btnAdd = new Button { Text = "Add", Location = new Point(20, 445), Size = new Size(95, 34) };
            Button btnEdit = new Button { Text = "Update", Location = new Point(125, 445), Size = new Size(95, 34) };
            Button btnDel = new Button { Text = "Delete", Location = new Point(230, 445), Size = new Size(95, 34) };
            Button btnClear = new Button { Text = "Clear", Location = new Point(335, 445), Size = new Size(90, 34) };

            btnAdd.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFirst.Text) || string.IsNullOrWhiteSpace(txtLast.Text) ||
                    !(cmbStuSpec.SelectedItem is ComboItem spec)) return;

                ExecNonQuery(@"INSERT INTO Students (FirstName, LastName, Age, Gender, CourseYear, SpecialtyID)
                               VALUES (@Fn, @Ln, @Age, @Gen, @Course, @SpecId)",
                    new SqlParameter("@Fn", txtFirst.Text.Trim()),
                    new SqlParameter("@Ln", txtLast.Text.Trim()),
                    new SqlParameter("@Age", (int)numAge.Value),
                    new SqlParameter("@Gen", cmbGender.Text),
                    new SqlParameter("@Course", (int)numCourse.Value),
                    new SqlParameter("@SpecId", spec.Id));
                ClearStu();
                RefreshAll();
            };

            btnEdit.Click += (s, e) =>
            {
                if (selectedStuId == 0 || string.IsNullOrWhiteSpace(txtFirst.Text) || string.IsNullOrWhiteSpace(txtLast.Text) ||
                    !(cmbStuSpec.SelectedItem is ComboItem spec)) return;

                ExecNonQuery(@"UPDATE Students
                               SET FirstName = @Fn, LastName = @Ln, Age = @Age, Gender = @Gen, CourseYear = @Course, SpecialtyID = @SpecId
                               WHERE StudentID = @Id",
                    new SqlParameter("@Fn", txtFirst.Text.Trim()),
                    new SqlParameter("@Ln", txtLast.Text.Trim()),
                    new SqlParameter("@Age", (int)numAge.Value),
                    new SqlParameter("@Gen", cmbGender.Text),
                    new SqlParameter("@Course", (int)numCourse.Value),
                    new SqlParameter("@SpecId", spec.Id),
                    new SqlParameter("@Id", selectedStuId));
                ClearStu();
                RefreshAll();
            };

            btnDel.Click += (s, e) =>
            {
                if (selectedStuId == 0) return;
                ExecNonQuery("DELETE FROM Students WHERE StudentID = @Id",
                    new SqlParameter("@Id", selectedStuId));
                ClearStu();
                RefreshAll();
            };

            btnClear.Click += (s, e) => ClearStu();

            page.Controls.AddRange(new Control[]
            {
                dgvStu, txtFirst, txtLast, numAge, cmbGender, numCourse, cmbStuSpec,
                btnAdd, btnEdit, btnDel, btnClear
            });
            tabs.TabPages.Add(page);
        }

        private void ClearStu()
        {
            selectedStuId = 0;
            txtFirst.Clear();
            txtLast.Clear();
            numAge.Value = 20;
            cmbGender.SelectedIndex = 0;
            numCourse.Value = 1;
            if (cmbStuSpec.Items.Count > 0) cmbStuSpec.SelectedIndex = 0;
        }

        private void LoadStudentsGrid()
        {
            string sql = @"SELECT s.StudentID, s.FirstName, s.LastName, s.Age, s.Gender, s.CourseYear,
                                  s.SpecialtyID, sp.SpecialtyName
                           FROM Students s
                           JOIN Specialties sp ON s.SpecialtyID = sp.SpecialtyID
                           ORDER BY s.StudentID";
            dgvStu.DataSource = ReadWithDataReader(sql);
        }

        // ==========================================
        // ADO.NET HELPERS (SqlDataReader + SqlParameter)
        // ==========================================
        private DataTable ReadWithDataReader(string query, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }
            return dt;
        }

        private int ExecNonQuery(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        private void SelectComboById(ComboBox cb, int id)
        {
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i] is ComboItem item && item.Id == id)
                {
                    cb.SelectedIndex = i;
                    return;
                }
            }
        }
    }
}