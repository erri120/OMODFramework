using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace OMODFramework.Scripting.ScriptHandlers.OBMMScript
{
    internal static class NumericExpressionHandler
    {
        /// <summary>
        /// Simple recursive expression evaluation function you all did in CS-class. We look for nested-expressions
        /// in brackets, remove them from the original list, do a recursive call on them to get the result of the
        /// nested-expression and insert it back into the list.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="recursiveCall"></param>
        /// <returns></returns>
        private static List<string> RecursiveEvaluation(IEnumerable<string> enumerable,
            Func<IEnumerable<string>, string> recursiveCall)
        {
            var list = enumerable.ToList();

            var index = list.IndexOf("(");

            while (index != -1)
            {
                var newFunc = new List<string>();
                var count = 1;
                for (var i = index + 1; i < list.Count; i++)
                {
                    var current = list[i];
                    switch (current)
                    {
                        case "(":
                            count++;
                            break;
                        case ")":
                            count--;
                            break;
                    }
                    if (count == 0)
                    {
                        list.RemoveRange(index, i - index + 1);
                        list.Insert(index, recursiveCall(newFunc));
                        break;
                    }
                    
                    newFunc.Add(current);
                }
                
                index = list.IndexOf("(");
            }

            return list;
        }
        
        internal static int EvaluateIntExpression(IEnumerable<string> enumerable)
        {
            var list = RecursiveEvaluation(enumerable, (x) => EvaluateIntExpression(x).ToString());

            /*
             * The following code is my solution to the mess OBMM had. To be fair, it was rather repetitive than messy
             * but it was still not nice to look at and work with.
             * The solution for dealing with all those operators is to introduce a helper that does all the repetitive
             * things for us.
             */
            var calc = new Action<string, bool, Func<int, int, int>>((search, single, calcFunc) =>
            {
                /*
                 * Some infos on the parameters of this function:
                 * - search: the search string, eg: "mod"
                 * - single: only used by the not operation, this is set to true when the operation only has 1 operand
                 * - calcFunc: the function that will ultimately calculate the result
                 */
                
                var searchIndex = list.IndexOf(search);

                while (searchIndex != -1)
                {
                    var i1 = int.Parse(single ? list[searchIndex + 1] : list[searchIndex - 1]);
                    var i2 = single ? 0 : int.Parse(list[searchIndex + 1]);
                    var res = calcFunc(i1, i2);

                    list[searchIndex + 1] = res.ToString();
                    if (single)
                        list.RemoveAt(searchIndex);
                    else
                        list.RemoveRange(searchIndex - 1, 2);
                    
                    searchIndex = list.IndexOf(search);
                }
            });
            
            /*
             * We call the calc function for every operator and provide a function for calculating the result if the
             * operator is present.
             */

            calc("not", true, (o1, o2) => ~o1);
            calc("and", false, (o1, o2) => o1 & o2);
            calc("or", false, (o1, o2) => o1 | o2);
            calc("xor", false, (o1, o2) => o1 ^ o2);
            calc("mod", false, (o1, o2) => o1 % o2);
            calc("%", false, (o1, o2) => o1 % o2);
            calc("^", false, (o1, o2) => (int) Math.Pow(o1, o2));
            calc("/", false, (o1, o2) => o1 / o2);
            calc("*", false, (o1, o2) => o1 * o2);
            calc("+", false, (o1, o2) => o1 + o2);
            calc("-", false, (o1, o2) => o1 - o2);
            
            return int.Parse(list[0]);
        }

        /// <summary>
        /// Basically the same as <see cref="EvaluateIntExpression"/> but for floats and with a bit more operations.
        /// This also returns <see cref="double"/> and not <see cref="float"/>.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        internal static double EvaluateFloatExpression(IEnumerable<string> enumerable)
        {
            var list = RecursiveEvaluation(enumerable, (x) => EvaluateFloatExpression(x)
                .ToString(CultureInfo.InvariantCulture));
            
            var calc = new Action<string, bool, Func<double, double, double>>((search, single, calcFunc) =>
            {
                var searchIndex = list.IndexOf(search);

                while (searchIndex != -1)
                {
                    var i1 = double.Parse(single ? list[searchIndex + 1] : list[searchIndex - 1]);
                    var i2 = single ? 0 : double.Parse(list[searchIndex + 1]);
                    var res = calcFunc(i1, i2);

                    list[searchIndex + 1] = res.ToString(CultureInfo.InvariantCulture);
                    if (single)
                        list.RemoveAt(searchIndex);
                    else
                        list.RemoveRange(searchIndex - 1, 2);
                    
                    searchIndex = list.IndexOf(search);
                }
            });
            
            calc("sin", true, (f, f1) => Math.Sin(f));
            calc("cos", true, (f, f1) => Math.Cos(f));
            calc("tan", true, (f, f1) => Math.Tan(f));
            calc("sinh", true, (f, f1) => Math.Sinh(f));
            calc("cosh", true, (f, f1) => Math.Cosh(f));
            calc("tanh", true, (f, f1) => Math.Tanh(f));
            calc("exp", true, (f, f1) => Math.Exp(f));
            calc("log", true, (f, f1) => Math.Log10(f));
            calc("ln", true, (f, f1) => Math.Log(f));
            calc("mod", false, (f, f1) => f%f1);
            calc("%", false, (f, f1) => f%f1);
            calc("^", false, Math.Pow);
            calc("/", false, (f, f1) => f/f1);
            calc("*", false, (f, f1) => f*f1);
            calc("+", false, (f, f1) => f+f1);
            calc("-", false, (f, f1) => f-f1);

            return double.Parse(list[0]);
        }
    }
}
