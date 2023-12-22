using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPS
{
    internal class GlobalVar
    {
        //連接字符串
        public static string strDBConnectionString = "";
        //登入狀態標示
        public static bool isLogin = false;
        //使用者名稱
        public static string userName = "";
        //使用者ID
        public static int userID = 0;
        //使用者權限
        public static int userPerms = 0;
        //最後一個訂單ID
        public static int lastOrderId = 0;

        
    }
}
