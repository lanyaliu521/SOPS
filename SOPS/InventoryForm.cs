using SOPS.SalesOrderSystemDataSetTableAdapters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace SOPS
{
    public partial class InventoryForm : Form
    {
        private OrdersTableAdapter OrdersTableAdapter;
        private OrderDetailsTableAdapter OrderDetailsTableAdapter;

        public InventoryForm()
        {
            InitializeComponent();

            // 初始化 TableAdapter
            this.OrdersTableAdapter = new OrdersTableAdapter();
            this.OrderDetailsTableAdapter = new OrderDetailsTableAdapter();
        }

        private void productsBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            this.Validate();
            this.productsBindingSource.EndEdit();
            this.tableAdapterManager.UpdateAll(this.salesOrderSystemDataSet);

        }
        private void InventoryForm_Load(object sender, EventArgs e)
        {
            // TODO: 這行程式碼會將資料載入 'salesOrderSystemDataSet.Products' 資料表。您可以視需要進行移動或移除。
            this.productsTableAdapter.Fill(this.salesOrderSystemDataSet.Products);
            this.OrdersTableAdapter.Fill(this.salesOrderSystemDataSet.Orders);
            this.OrderDetailsTableAdapter.Fill(this.salesOrderSystemDataSet.OrderDetails);

            // 檢查用戶權限
            if (GlobalVar.userPerms != 1)
            {
                btnManageEmployees.BackColor = Color.Gray;
                btnManageEmployees.Click -= btnManageEmployees_Click; // 移除原有的事件處理
                btnManageEmployees.Click += (s, args) => MessageBox.Show("權限不足", "僅限店長使用"); // 新增顯示權限不足的事件處理
            }

            // 在 DataGridView 中添加一個新的列用於訂量輸入
            DataGridViewTextBoxColumn orderQuantityColumn = new DataGridViewTextBoxColumn();
            orderQuantityColumn.Name = "OrderQuantity";
            orderQuantityColumn.HeaderText = "訂量";
            dgvInventory.Columns.Add(orderQuantityColumn);
            GlobalVar.lastOrderId = GetLastUnacceptedOrderId();
            ClearOQ();

        }

        private int GetLastUnacceptedOrderId()
        {
            var lastUnacceptedOrder = salesOrderSystemDataSet.Orders
                                        .Where(order => order.OrderStatus != 1)
                                        .OrderByDescending(order => order.CreateTime)
                                        .FirstOrDefault();

            return lastUnacceptedOrder != null ? lastUnacceptedOrder.OrderID : 0; // 返回最後一筆未驗收的訂單ID，如果沒有則返回0
        }

        private void ClearOQ()
        {
            // 遍歷 DataGridView 的每一行
            foreach (DataGridViewRow row in dgvInventory.Rows)
            {
                // 檢查行是否是新行。
                if (!row.IsNewRow)
                {
                    // 訂量值設為0
                    row.Cells["OrderQuantity"].Value = 0;
                }
            }
        }

        private void ShowOrderDetails(int orderId)
        {
            var order = salesOrderSystemDataSet.Orders.FindByOrderID(orderId);
            if (order == null)
            {
                MessageBox.Show("找不到訂單。", "通知");
                return;
            }

            var orderDetails = salesOrderSystemDataSet.OrderDetails.Where(od => od.OrderID == orderId);

            StringBuilder message = new StringBuilder();
            message.AppendLine($"訂單編號: {order.OrderID}, 訂貨人: {order.EmployeeID}, 總金額: {order.TotalAmount}");

            foreach (var detail in orderDetails)
            {
                var product = salesOrderSystemDataSet.Products.FindByProductID(detail.ProductID);
                if (product != null)
                {
                    message.AppendLine($"商品名稱: {product.ProductName}, 訂量: {detail.OrderQuantity}");
                }
            }

            MessageBox.Show(message.ToString(), "訂單明細");
        }


        private void btnRefreshInventory_Click(object sender, EventArgs e)
        {
            GlobalVar.lastOrderId = GetLastUnacceptedOrderId();
            ClearOQ();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            // 檢查是否有訂量不為0的項目
            bool hasOrder = dgvInventory.Rows.Cast<DataGridViewRow>().Any(row => Convert.ToInt32(row.Cells["OrderQuantity"].Value) != 0);

            if (hasOrder)
            {
                // 創建新訂單
                DataRow orderRow = salesOrderSystemDataSet.Orders.NewRow();
                int orderId = (int)(DateTime.Now.Ticks % int.MaxValue);
                orderRow["OrderID"] = orderId;
                orderRow["EmployeeID"] = GlobalVar.userID;
                orderRow["TotalAmount"] = 0; // 初始化總金額
                orderRow["OrderStatus"] = 0; // 設置訂單狀態
                orderRow["CreateTime"] = DateTime.Now;
                salesOrderSystemDataSet.Orders.Rows.Add(orderRow);

                decimal totalAmount = 0;

                // 遍歷每個訂量不為0的行
                foreach (DataGridViewRow row in dgvInventory.Rows)
                {
                    int orderQuantity = Convert.ToInt32(row.Cells["OrderQuantity"].Value);
                    if (orderQuantity != 0)
                    {
                        int productId = Convert.ToInt32(row.Cells["ProductID"].Value);
                        decimal price = Convert.ToDecimal(row.Cells["Price"].Value);

                        // 創建訂單明細
                        DataRow detailRow = salesOrderSystemDataSet.OrderDetails.NewRow();
                        detailRow["OrderID"] = orderId;
                        detailRow["ProductID"] = productId;
                        detailRow["OrderQuantity"] = orderQuantity;
                        detailRow["CreateTime"] = DateTime.Now;
                        salesOrderSystemDataSet.OrderDetails.Rows.Add(detailRow);

                        // 更新總金額
                        totalAmount += price * orderQuantity;
                    }
                }

                // 更新訂單總金額
                orderRow["TotalAmount"] = totalAmount;

                // 將更改保存到數據庫
                this.OrdersTableAdapter.Update(salesOrderSystemDataSet.Orders);
                this.OrderDetailsTableAdapter.Update(salesOrderSystemDataSet.OrderDetails);
                GlobalVar.lastOrderId = orderId;
                ClearOQ();
                MessageBox.Show($"訂單編號為 {orderId}。\n金額為{totalAmount}元。", "訂單生成成功");
            }
            else
            {
                MessageBox.Show("沒有訂貨項目。");
            }
        }

        private void btnView_Click(object sender, EventArgs e)
        {
            GlobalVar.lastOrderId = GetLastUnacceptedOrderId();
            ShowOrderDetails(GlobalVar.lastOrderId);
        }

        private void btnCheckAccept_Click(object sender, EventArgs e)
        {
            //將訂單的值整合進資料庫對應資料行
            var lastOrder = salesOrderSystemDataSet.Orders.FindByOrderID(GlobalVar.lastOrderId);
            var orderDetails = salesOrderSystemDataSet.OrderDetails.Where(od => od.OrderID == GlobalVar.lastOrderId);

            if (lastOrder != null && lastOrder.OrderStatus != 1)
            {
                foreach (var detail in orderDetails)
                {
                    var product = salesOrderSystemDataSet.Products.FindByProductID(detail.ProductID);
                    if (product != null)
                    {
                        product.Quantity += detail.OrderQuantity; // 更新庫存
                    }
                }

                lastOrder.OrderStatus = 1; // 更新訂單狀態為已驗收

                // 將更改保存到數據庫
                productsTableAdapter.Update(salesOrderSystemDataSet.Products);
                OrdersTableAdapter.Update(salesOrderSystemDataSet.Orders);
                GlobalVar.lastOrderId = GetLastUnacceptedOrderId();
                ClearOQ();
                MessageBox.Show($"訂單 {lastOrder.OrderID} 已驗收。", "驗收成功");
            }
            else
            {
                MessageBox.Show("沒有可驗收的訂單或訂單已處理。");
            }
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            // 獲取最後一筆未驗收的訂單ID
            int lastOrderId = GetLastUnacceptedOrderId();

            if (lastOrderId != 0)
            {
                GlobalVar.lastOrderId = GetLastUnacceptedOrderId();
                ShowOrderDetails(GlobalVar.lastOrderId);
                // 確認是否要刪除
                if (MessageBox.Show("確定要刪除這筆訂單及其所有明細嗎？", "刪除確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    // 刪除所有相關的訂單明細
                    foreach (var detail in salesOrderSystemDataSet.OrderDetails.Where(od => od.OrderID == lastOrderId).ToList())
                    {
                        salesOrderSystemDataSet.OrderDetails.RemoveOrderDetailsRow(detail);
                    }

                    // 刪除訂單
                    var order = salesOrderSystemDataSet.Orders.FindByOrderID(lastOrderId);
                    if (order != null)
                    {
                        salesOrderSystemDataSet.Orders.RemoveOrdersRow(order);
                    }

                    // 將更改保存到數據庫
                    this.OrderDetailsTableAdapter.Update(salesOrderSystemDataSet.OrderDetails);
                    this.OrdersTableAdapter.Update(salesOrderSystemDataSet.Orders);
                    GlobalVar.lastOrderId = GetLastUnacceptedOrderId();
                    ClearOQ();
                    MessageBox.Show("訂單及其明細已被刪除。", "刪除成功");
                }
            }
            else
            {
                MessageBox.Show("沒有可刪除的訂單。");
            }
        }

        private void btnManageEmployees_Click(object sender, EventArgs e)
        {
            // 創建員工管理表單的實例
            EmployeeManagementForm employeeForm = new EmployeeManagementForm();
            this.Hide();
            // 顯示員工管理表單
            employeeForm.ShowDialog();
            this.Close();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Hide();

            LoginForm loginForm = new LoginForm();
            loginForm.ShowDialog();
            this.Close();
        }


    }
}
