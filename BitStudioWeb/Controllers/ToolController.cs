using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace bitkanda.Controllers
{
    public class ToolController : Controller
    {
        /// <summary>
        /// 十六进制换算为十进制
        /// </summary>
        /// <param name="strColorValue"></param>
        /// <returns></returns>
        public static decimal GetHexadecimalValue(String strColorValue)
        {
            char[] nums = strColorValue.ToCharArray();
            decimal total = 0;
            try
            {
                for (int i = 0; i < nums.Length; i++)
                {
                    String strNum = nums[i].ToString().ToUpper();
                    switch (strNum)
                    {
                        case "A":
                            strNum = "10";
                            break;
                        case "B":
                            strNum = "11";
                            break;
                        case "C":
                            strNum = "12";
                            break;
                        case "D":
                            strNum = "13";
                            break;
                        case "E":
                            strNum = "14";
                            break;
                        case "F":
                            strNum = "15";
                            break;
                        default:
                            break;
                    }
                    decimal power = (decimal)pow(16, (nums.Length - i - 1));
                    total += (decimal)Convert.ToInt32(strNum) * (power);
                }

            }
            catch (System.Exception ex)
            {
                String strErorr = ex.ToString();
                return 0;
            }


            return total;
        }
        public static BigInteger GetHexadecimalValueA(String strColorValue)
        {
            char[] nums = strColorValue.ToCharArray();
            BigInteger total = 0;
            try
            {
                for (int i = 0; i < nums.Length; i++)
                {
                    String strNum = nums[i].ToString().ToUpper();
                    switch (strNum)
                    {
                        case "A":
                            strNum = "10";
                            break;
                        case "B":
                            strNum = "11";
                            break;
                        case "C":
                            strNum = "12";
                            break;
                        case "D":
                            strNum = "13";
                            break;
                        case "E":
                            strNum = "14";
                            break;
                        case "F":
                            strNum = "15";
                            break;
                        default:
                            break;
                    }
                    BigInteger power = powA(16,  (nums.Length - i - 1));
                    total += BigInteger.Parse (strNum) * (power);
                }

            }
            catch (System.Exception ex)
            {
                String strErorr = ex.ToString();
                return 0;
            }


            return total;
        }
        public static decimal Hexadecimalal(string v, int n = 18)
        {
            var t = v.TrimStart(new char[] { '0', 'x' });

            var d = GetHexadecimalValue(t);
            return d / pow(10, n);
        }

        public static BigInteger HexadecimalalA(string v, int n = 18)
        {
            var t = v.TrimStart(new char[] { '0', 'x' });

            var d = GetHexadecimalValueA(t);
            return d;

            //return d / BigInteger.Pow(10, n);
           // return d /  pow(10, n);
        }

        public static decimal pow(int a, int b)
        {
            if (b == 0)
                return 1;
            decimal r = a;
            while (b > 1)
            {
                r = r * a;
                b--;
            }
            return r;
        }
   public static BigInteger powA(int a, int b)
        {
            if (b == 0)
                return 1;
            BigInteger r = a;
            while (b > 1)
            {
                r = r * a;
                b--;
            }
            return r;
        }
        private const int defaultd = 18;

        public IActionResult Hexadecimalal(string num,string d)
        {
            int n = defaultd;
            if (!int.TryParse(d, out n))
            {
                n = defaultd;
            }
            if(n> defaultd || n<=0)
            {
                n = defaultd;
            }
            // "0x000000000000000000000000000000000000000024169cc45364831ed4410a49";
            //decimal v = Hexadecimalal(num);
            var v = HexadecimalalA(num);
            return Json(new { data = v.ToString(), success = true });
           
            
        }
    }
}