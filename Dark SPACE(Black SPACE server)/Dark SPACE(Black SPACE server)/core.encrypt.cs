using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Do
{
   public class core
    {
	  public string for_encode = "";
 
        public void out_(object a)
        {
          //  Console.WriteLine(Encoding.Default.GetString(Encoding.Default.GetBytes(a.ToString())));
        }
        public string encode(string s)
        {
            int key = 25;
            var ou_ = "";
            try { 
   
            var aa = Encoding.Default.GetBytes(s);
                //    int                key = 27;
            var encd = new byte[s.Length];
            var end_key = (key * 0x723 * 0x17F2DBB >> 2008).ToString() + "YWRkX2tleQ==";
    
            var key_bs64 = Convert.ToBase64String(Encoding.Default.GetBytes(end_key));
            var key_bs64_byte = Encoding.Default.GetBytes(key_bs64);
            for (int a = 0; a < s.Length; a++)
            {
                encd[a] = (byte)(aa[a] ^ key_bs64_byte[0]);
            }
     
     ou_= (Convert.ToBase64String(encd));
            
            }
            catch { }
            return ou_;
        }
        public string decode(string u)
        {
            int key = 25;
            var ou_ = "";
            try
            {
                var s = Convert.FromBase64String(u);
                //    int                key = 27;
                var end_key = (key * 0x723 * 0x17F2DBB >> 2008).ToString() + "YWRkX2tleQ==";

                var key_bs64 = Convert.ToBase64String(Encoding.Default.GetBytes(end_key));
                var dec = new byte[s.Length];
                for (int a = 0; a < s.Length; a++)
                {
                    dec[a] = (byte)(s[a] ^ key_bs64[0]);
                }
                ou_ = Encoding.Default.GetString(dec);
            }
            catch { }
            return ou_;
        }
    }
}
