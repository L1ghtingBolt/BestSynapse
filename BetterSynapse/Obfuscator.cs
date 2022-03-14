using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SynapseX
{
    public class Obfuscator
    {
        public List<Variable> variables = new List<Variable> { };
        public int varIdx = 0;
        public Random random = new Random();
        public string GetRandomAlphaNumeric(int ammount)
        {
            // Lua functions can't start with numbers & I'm too lazy to add a check.
            var chars = "abcdefghijklmnopqrstuvwxyz" + "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(chars.Select(c => chars[random.Next(chars.Length)]).Take(ammount).ToArray());
        }

        public string getVarName()
        {
            bool valid = true;
            string name = "";
            while (name == "")
            {
                string newName = GetRandomAlphaNumeric(15);
                foreach (Variable var in variables)
                {
                    if (var.newName == newName)
                    {
                        valid = false;
                    }
                }
                if (valid == true)
                {
                    if (newName.IndexOf('1') != -1)
                    {
                        if (newName.IndexOf('1') != 0)
                        {
                            name = newName;
                        }
                        else
                        {
                            return getVarName();
                        }
                    }
                }
                else
                {
                    return getVarName();
                }
            }
            return name;
        }

        public async Task<string> ObfuscateVariables(string input)
        {
            string returns = input;
            string separator = "//This is a BestSynapse separator";
            string[] lines = returns.Split(Environment.NewLine.ToCharArray());
            for (int i = 0; i < lines.GetLength(0); i++)
            {
                string line = lines[i];
                if (line.Contains("local"))
                {
                    var varName = Substring.GetStringBetween(line, "local", "=").Replace(" ", "");
                    Variable theVar;
                    theVar.oldName = varName;
                    theVar.newName = GetRandomAlphaNumeric(10);
                    variables.Add(theVar);
                }
                if (line.Contains("function"))
                {
                    var varName = Substring.GetStringBetween(line, "function", "(").Replace(" ", "");
                    Variable theVar;
                    theVar.oldName = varName;
                    theVar.newName = GetRandomAlphaNumeric(10);
                    variables.Add(theVar);
                }
            }
            for (int i = 0; i < variables.Count; i++)
            {
                string[] newList = lines;
                for (int j = 0; j < newList.GetLength(0); j++)
                {
                    Regex regex = new Regex(Regex.Escape(variables[i].oldName));
                    // lines[j] = lines[j].Replace(variables[i].oldName, variables[i].newName);
                    lines[j] = regex.Replace(lines[j], variables[i].newName, 1);
                }
                lines = newList;
            }
            return string.Join("\\n", lines);
        }
    }
}
