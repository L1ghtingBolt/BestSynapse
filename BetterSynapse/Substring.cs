using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace SynapseX
{
    public class Substring
    {
        public static string GetStringBetween(string token, string first, string second)
        {
            if (!token.Contains(first))
            {
                return "";
            }
            string text = token.Split(new string[]
            {
                first
            }, StringSplitOptions.None)[1];
            if (!text.Contains(second))
            {
                return "";
            }
            return text.Split(new string[]
            {
                second
            }, StringSplitOptions.None)[0];
        }
    }
}
