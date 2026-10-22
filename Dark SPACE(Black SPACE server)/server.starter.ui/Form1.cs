using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
//using MySQLManager.Database;
 
//using Do.Core;
//using Do.game;
//using Do.game.usersClass;
//using Do.serverGame;
 
//using System.Runtime.InteropServices;

namespace server.starter.ui
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        public Form1()
        {
            InitializeComponent();
            // this.FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            new Thread(restart).Start();
            
        }



        private void metroButton1_Click(object sender, EventArgs e)
        {
            File.Delete(Environment.CurrentDirectory + "\\1.bat");
            System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\1.bat", "taskkill /f /im server.starter.ui.exe \n start server.starter.ui.exe");
            Process.Start(Environment.CurrentDirectory + "\\1.bat");
        }
       
        private void metroButton3_Click(object sender, EventArgs e)
        {
           
        }
        public void Start(string s)
        {
            Process.Start(s);
            Do.Out.out___ = "Process " + s + " started !";
        }
        private void metroButton4_Click(object sender, EventArgs e)
        {
            Start(@"c:\xampp\apache\bin\httpd.exe");
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // try { File.Delete(Environment.CurrentDirectory + "\\1.bat"); } catch { }
            while (true)
            {
                metroTextBox3.Text = Do.Out.out___;
                //Thread.Sleep(10);
                var s = 1;

                s++;
                if (s == 10000)
                {
                    s = 1;
                }
                await Task.Delay(s);
            }
        }

      
    public async void restart()
        {

            new Thread(Do.Core.AsynchronousSocketListener.StartListening).Start();
            await Task.Delay(21600000);
            File.Delete(Environment.CurrentDirectory + "\\1.bat");
            System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\1.bat", "taskkill /f /im server.starter.ui.exe \n start server.starter.ui.exe");
            Process.Start(Environment.CurrentDirectory + "\\1.bat");
        }

        private void metroTextBox1_Click(object sender, EventArgs e)
        {

        }

        // public static DatabaseManager Manager;
        //public static string /*DataRow*/ ExecuteSql(string q)
        //{
        //    //     var set =   ReadSettings(File.ReadAllText("app.ini"));
        //    var db = new DatabaseManager(30, 10, DatabaseType.MySql);
        //    db.setServerDetails("127.0", 3306, "root2", "12345678", "darkorbit");
        //    db.init();
        //        var data = (DataTable)db.getQueryreactor().query(q);
        //    //    if (data.Rows.Count != 1) /*return;*/
        //    return data.Rows[0].ToString();
        //}
      //  private void metroButton2_Click(object sender, EventArgs e)
      //  {
      // //     var s = metroTextBox1.Text.ToString();
      ////  Do.Out.out___ = ExecuteSql(s);
      //  }

        private void metroButton5_Click(object sender, EventArgs e)
        {
            Start(@"‪D:\mysql\bin\mysqld.exe");
        }


        private void metroButton8_Click(object sender, EventArgs e)
        {
            File.Delete(Environment.CurrentDirectory + "\\mysqld.bat");
            System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\mysqld.bat", "taskkill /f /im mysqld.exe  ");
            Process.Start(Environment.CurrentDirectory + "\\mysqld.bat");
        }

        private void metroButton7_Click(object sender, EventArgs e)
        {
            File.Delete(Environment.CurrentDirectory + "\\apache.bat");
            System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\apache.bat", "taskkill /f /im httpd.exe  ");
            Process.Start(Environment.CurrentDirectory + "\\apache.bat");

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            
            Environment.Exit(0);
        }

        private void stopButton_Click_1(object sender, EventArgs e)
        {
            Process.Start("taskkill", "/F /IM server.starter.ui.exe");
        }
    }
}