using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace SOPS
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            //創建資料庫連接字符串的實例
            var scsb = new SqlConnectionStringBuilder();
            //設置資料源為本地,初始目標資料庫,啟用集成安全性
            scsb.DataSource = ".";
            scsb.InitialCatalog = "SalesOrderSystem";
            scsb.IntegratedSecurity = true;

            //將生成的連接字符串分配給全域變數
            GlobalVar.strDBConnectionString = scsb.ConnectionString;
        }

        //權限字串轉換成值
        private int ConvertToPermissionCode(string title)
        {
            switch (title)
            {
                case "店長":
                    return 1; // 假設店長的權限代碼為 1
                case "職員":
                    return 2; // 假設職員的權限代碼為 2
                default:
                    return 0; // 未知或無權限
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            // 檢查欄位是否為空
            if (string.IsNullOrEmpty(txtEmployeeID.Text) || string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("請輸入員編及密碼", "登入錯誤");
                return; // 退出事件處理器，中斷執行。
            }

            // 連接字符串。
            string connectionString = GlobalVar.strDBConnectionString;

            // SQL查詢，檢查員工ID和密碼是否存於資料庫。
            string query = "SELECT EmployeeID, Title, EmployeeName FROM Employees WHERE EmployeeID=@EmployeeID AND Password=@Password";

            // 使用using語句來確保釋放SqlConnection資源。
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                // 創建SqlCommand對象，傳進查詢和連接對象。
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    // 添加參數，防止SQL注入攻擊。
                    cmd.Parameters.AddWithValue("@EmployeeID", txtEmployeeID.Text);
                    cmd.Parameters.AddWithValue("@Password", txtPassword.Text);

                    // 打開資料庫連接。
                    con.Open();

                    // 執行查詢並返回結果。結果是符合條件的行數。
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // 登入成功觸發事件。
                            GlobalVar.isLogin = true;

                            // 讀取用戶的職稱/權限/名稱
                            string userTitle = reader["Title"].ToString();
                            GlobalVar.userPerms = ConvertToPermissionCode(userTitle); // 轉換為相應的權限代碼
                            GlobalVar.userName = reader["EmployeeName"].ToString(); // 設置用戶名
                            GlobalVar.userID = Convert.ToInt32(reader["EmployeeID"].ToString());

                            this.Hide();//隱藏當前表單
                            InventoryForm inventoryForm = new InventoryForm();//創建表單實例
                            inventoryForm.ShowDialog();//顯示進銷存表單
                            con.Close();//關閉資料庫連結
                            this.Close();//關閉登入表單

                        }
                        else
                        {
                            // 登入失敗觸發事件
                            MessageBox.Show("員編或密碼輸入錯誤,請確認後再次輸入。", "登入資訊錯誤");
                            txtEmployeeID.Clear();
                            txtPassword.Clear();

                        }
                    }
                }

            }
        }
    }
}
