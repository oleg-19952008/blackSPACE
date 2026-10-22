using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Do
{
    class Logger
    {
        public static void Log(string userid, string type, string message) { }
        //{
        //    if(userid=="0")
        //    {
        //        return;

        //    }
        //   var path = @"C:\Users\givanov\Desktop\Game Logs\" + userid + ".txt";
        //   try
        //   {
        //       File.AppendAllText(path, "[" + System.DateTime.Now + "] - [" + type + "] ->> '" + message + "'" + Environment.NewLine);
        //   }
        //   catch (Exception )
        //   {
        //   //    Console.WriteLine("" + e);

        //   }

            //if (!File.Exists(path))
            //{
            //    using (StreamWriter sw = File.CreateText(path))
            //    {
                 
            //    }
            //    return;

            //}
            //using (StreamWriter sw = new StreamWriter(path))
            //{
            //        sw.WriteLine("[" + System.DateTime.Now + "] - [" + type + "] ->> '" + message + "'" + Environment.NewLine);
            //}
        }
    }
