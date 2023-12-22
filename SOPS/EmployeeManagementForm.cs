using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;


namespace SOPS
{
    public partial class EmployeeManagementForm : Form
    {
        public EmployeeManagementForm()
        {
            InitializeComponent();
        }

        // 當表單加載時執行
        private void EmployeeManagementForm_Load(object sender, EventArgs e)
        {
            // 載入員工資料
            this.employeesTableAdapter.Fill(this.salesOrderSystemDataSet.Employees);

            // 設定員工資料的顯示順序
            DataView view = new DataView(this.salesOrderSystemDataSet.Employees);
            view.Sort = "EmployeeID DESC"; // 根據員工ID倒序排列
            dgvEmployees.DataSource = view; // 將排序後的資料綁定到 DataGridView

            ResetForm();
        }

        // 保存變更到資料庫
        private void employeesBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            this.Validate();
            this.employeesBindingSource.EndEdit();
            this.tableAdapterManager.UpdateAll(this.salesOrderSystemDataSet);
        }

        // 員工資料驗證
        private bool ValidateEmployeeData()
        {
            // 驗證職稱是否為 '店長' 或 '職員'
            if (cmbTitle.Text != "店長" && cmbTitle.Text != "職員")
            {
                MessageBox.Show("職稱必須是 '店長' 或 '職員'。", "驗證錯誤");
                return false;
            }

            // 驗證密碼格式是否為四位數字
            if (!int.TryParse(txtPassword.Text, out int _) || txtPassword.Text.Length != 4)
            {
                MessageBox.Show("密碼必須是四位數字。", "驗證錯誤");
                return false;
            }

            return true;
        }

        // 重置按鈕的事件處理
        private void btnReset_Click(object sender, EventArgs e)
        {
            // 清空文本框並重設過濾條件
            ResetForm();
        }

        // 重設表單的功能
        private void ResetForm()
        {
            // 取消事件綁定以避免觸發選擇改變事件
            dgvEmployees.SelectionChanged -= dgvEmployees_SelectionChanged;

            // 清空輸入欄位
            txtName.Clear();
            cmbTitle.SelectedIndex = -1;
            txtPassword.Clear();

            // 重設 DataView 的過濾條件
            DataView view = (DataView)dgvEmployees.DataSource;
            view.RowFilter = "";
            view.Sort = "EmployeeID DESC";

            // 重新載入資料
            this.employeesTableAdapter.Fill(this.salesOrderSystemDataSet.Employees);

            // 重新綁定事件
            dgvEmployees.SelectionChanged += dgvEmployees_SelectionChanged;
        }

        // 搜索功能的實現
        private void btnSearch_Click(object sender, EventArgs e)
        {
            // 根據輸入條件進行資料過濾
            ApplyFilter();
        }

        // 應用過濾條件
        private void ApplyFilter()
        {
            DataView view = (DataView)dgvEmployees.DataSource;
            string filter = "EmployeeName LIKE '%" + txtName.Text + "%'";
            if (cmbTitle.SelectedIndex >= 0)
            {
                filter += " AND Title = '" + cmbTitle.Text + "'";
            }
            view.RowFilter = filter;
        }

        // 添加新員工
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateEmployeeData()) return;

            AddNewEmployee();
        }

        // 添加新員工的功能
        private void AddNewEmployee()
        {
            DataRow newRow = salesOrderSystemDataSet.Employees.NewRow();
            newRow["EmployeeName"] = txtName.Text;
            newRow["Title"] = cmbTitle.Text;
            newRow["Password"] = txtPassword.Text;

            salesOrderSystemDataSet.Employees.Rows.Add(newRow);
            employeesTableAdapter.Update(salesOrderSystemDataSet.Employees);
            MessageBox.Show("員工新增成功！","通知");
            ResetForm();
        }

        // 編輯員工資料
        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvEmployees.CurrentRow == null || !ValidateEmployeeData()) return;

            EditEmployee();
        }

        // 編輯員工的功能
        private void EditEmployee()
        {
            DataRow row = ((DataRowView)dgvEmployees.CurrentRow.DataBoundItem).Row;
            row["EmployeeName"] = txtName.Text;
            row["Title"] = cmbTitle.Text;
            row["Password"] = txtPassword.Text;

            employeesTableAdapter.Update(salesOrderSystemDataSet.Employees);
            MessageBox.Show("員工編輯成功！", "通知");
            ResetForm();
        }

        // 刪除員工
        private void btnDel_Click(object sender, EventArgs e)
        {
            if (dgvEmployees.CurrentRow == null) return;

            DeleteEmployee();
        }

        // 刪除員工的功能
        private void DeleteEmployee()
        {
            if (MessageBox.Show("確定要刪除此員工嗎？", "刪除確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                dgvEmployees.Rows.RemoveAt(dgvEmployees.CurrentRow.Index);
                employeesTableAdapter.Update(salesOrderSystemDataSet.Employees);
                MessageBox.Show("員工刪除成功！", "通知");
                ResetForm();
            }
        }

        // 返回按鈕的事件處理
        private void btnBack_Click(object sender, EventArgs e)
        {
            OpenForm(new InventoryForm());
        }

        // 離開按鈕的事件處理
        private void btnExit_Click(object sender, EventArgs e)
        {
            OpenForm(new LoginForm());
        }

        // 打開新表單並關閉當前表單
        private void OpenForm(Form form)
        {
            this.Hide();
            form.ShowDialog();
            this.Close();
        }

        // DataGridView 選擇改變的事件處理
        private void dgvEmployees_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvEmployees.CurrentRow != null)
            {
                PopulateFieldsFromSelectedRow();
            }
        }

        // 根據選中的行填充欄位
        private void PopulateFieldsFromSelectedRow()
        {
            DataRow row = ((DataRowView)dgvEmployees.CurrentRow.DataBoundItem).Row;

            txtName.Text = row["EmployeeName"].ToString();
            cmbTitle.Text = row["Title"].ToString();
            txtPassword.Text = row["Password"].ToString();

        }
    }
}
