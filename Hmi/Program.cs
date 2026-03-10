using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Hmi
{
    static class Program
    {
        /// <summary>
        /// Ponto de entrada principal para o aplicativo.
        /// </summary>
        /// 
        private static Mutex mutex = new Mutex(true, "{CPNTeck - HMI}");

        [STAThread]
        static void Main()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmMain());
                mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show("O programa já esta aberto.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new frmMain());
            //Application.Run(new frmMain());


        }


    }



}
