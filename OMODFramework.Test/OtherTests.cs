﻿using System.Collections.Generic;
using OMODFramework.Scripting;
using Xunit;

namespace OMODFramework.Test
{
    public class OtherTests
    {
        [Fact]
        public void TestISet()
        {
            var testList = new Dictionary<int, List<string>>
            {
                {4, new List<string> {"-1", "+", "(", "5", "mod", "3", ")", "+", "(", "6", "-", "3", ")"}},
                {44102, new List<string>{"3","*","23","+","(","1","+","344","*","(","5","+","123",")",")"}}
            };

            testList.Do(pair =>
            {
                (var key, List<string> value) = pair;
                var result = OBMMScriptHandler.EvaluateIntExpression(value);
                Assert.Equal(key, result);
            });
        }

        [Fact]
        public void TestFSet()
        {
            var testList = new Dictionary<double, List<string>>
            {
                {1E+9, new List<string>{"1E+10","/","10"}},
                {1.0481943458718355E+30, new List<string>{"3.4","+","(","sin","1E+3",")","*","(","2","^","100",")"}}
            };

            testList.Do(pair =>
            {
                (var key, List<string> value) = pair;
                var result = OBMMScriptHandler.EvaluateFloatExpression(value);
                Assert.Equal(key, result);
            });
        }
    }
}