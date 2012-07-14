using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HollyServer
{
    public enum EValType
    {
        JString, JInt, JArray //more to do
    }

    public class JItem
    {
        public JItem(string name)
        {
            Name = name;
        }
        public string Name;
        public string ValString;
        public int ValInt;
        public List<JItem> ValArray;
        public EValType TypeOfVal;
        public void SetVal(string s)
        {
            int i;
            if (s.Length > 0 && s[0] == '\"'
                && s[s.Length - 1] == '\"')
            {
                TypeOfVal = EValType.JString;
                ValString = s.Substring(1, s.Length - 2);
            }
            else
            {
                Int32.TryParse(s, out i);
                TypeOfVal = EValType.JInt;
                ValInt = i;
            }
        }

        public void Add(string name, string val)
        {
            JItem ji = new JItem(name);
            ji.SetVal(val);
            ValArray.Add(ji);
        }
    }


    public class JSON
    {


        public static JItem Parse(string str)
        {
            int idx = 0;
            JItem root = new JItem("__root");
            root.TypeOfVal = EValType.JArray;
            root.ValArray = new List<JItem>();

            string name = "__root";
            int depth = 0;
            bool inStr = false;
            bool inEsc = false;
            string tempStr = "";
            JItem tempArray = root;
            bool justFinishedArray = false;

            Dictionary<int, JItem> items = new Dictionary<int, JItem>();
            items.Add(0, root);
            for (idx = 0; idx < str.Length; idx++)
            {
                switch (str[idx])
                {
                    case '{':
                        {
                            depth++;
                            JItem ji = new JItem(name);
                            ji.TypeOfVal = EValType.JArray;
                            ji.ValArray = new List<JItem>();
                            items[depth - 1].ValArray.Add(ji);
                            if (items.ContainsKey(depth))
                            {
                                items[depth] = ji;
                            }
                            else
                            {
                                items.Add(depth, ji);
                            }
                        }
                        break;
                    case '}':
                        {
                            if (!justFinishedArray) items[depth].Add(name, tempStr);
                            else justFinishedArray = false;
                            tempStr = "";
                            depth--;
                            justFinishedArray = true;
                        }
                        break;
                    case '\\':
                        inEsc = !inEsc;
                        if (!inEsc) tempStr += '\\';
                        break;
                    case '"':
                        tempStr += '"';
                        if (!inEsc)
                        {
                            inStr = !inStr;
                            //if (!inStr && isName) name = tempStr;
                        }
                        break;
                    case ':':
                        {
                            if (inStr) tempStr += str[idx];
                            else
                            {
                                name = tempStr;
                                tempStr = "";
                            }

                        }
                        break;
                    case ',':
                        {
                            if (inStr) tempStr += str[idx]; //skip if within " " block
                            else
                            {
                                if (!justFinishedArray) items[depth].Add(name, tempStr);
                                else justFinishedArray = false;
                                tempStr = "";
                            }
                        }
                        break;
                    default:
                        tempStr += str[idx];
                        break;
                }


            }



            return items[1];
        }
        

    }


}
