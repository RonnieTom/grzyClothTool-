using CodeWalker.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shell;

namespace CodeWalker
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Always check the GTA folder first thing
            if (!GTAFolder.UpdateGTAFolder(Properties.Settings.Default.RememberGTAFolder))
            {
                MessageBox.Show("未选择有效的 GTA 5 文件夹，无法加载 CodeWalker。程序即将退出。", "未找到 GTA 5 文件夹", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
#if !DEBUG
            try
            {
#endif
#if !DEBUG
            }
            catch (Exception ex)
            {
                MessageBox.Show("遇到意外错误！\n" + ex.ToString());
                //this can happen if folder wasn't chosen, or in some other catastrophic error. meh.
            }
#endif
        }
    }
}
