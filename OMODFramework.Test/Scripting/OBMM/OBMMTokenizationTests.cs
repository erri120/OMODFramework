using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using OMODFramework.Scripting.ScriptHandlers.OBMMScript;
using OMODFramework.Scripting.ScriptHandlers.OBMMScript.Tokenizer;
using Xunit;

namespace OMODFramework.Test.Scripting.OBMM
{
    public class OBMMTokenizationTests
    {
        public static IEnumerable<object[]> SingleLineTestData => new List<object[]>
        {
            new object[] { "If ScriptExtenderPresent", TokenType.If, typeof(IfToken) },
            new object[] { "IfNot ScriptExtenderPresent", TokenType.IfNot, typeof(IfToken) },
            new object[] { "Else", TokenType.Else, typeof(StartFlowToken) },
            new object[] { "EndIf", TokenType.EndIf, typeof(EndFlowToken) },
            new object[] { "FatalError", TokenType.FatalError, typeof(InstructionToken) },
            new object[] { @"Select ""Title"" ""Option1""", TokenType.Select, typeof(SelectToken) },
            new object[] { @"SelectMany ""Title"" ""Option1"" ""Option2""", TokenType.SelectMany, typeof(SelectToken) },
            new object[] { @"SelectWithPreview ""Title"" ""Option1"" ""Preview1""", TokenType.SelectWithPreview, typeof(SelectToken) },
            new object[] { @"SelectManyWithPreview ""Title"" ""Option1"" ""Preview1"" ""Option2"" ""Preview2""", TokenType.SelectManyWithPreview, typeof(SelectToken) },
            new object[] { @"SelectWithDescriptions ""Title"" ""Option1"" ""Description1""", TokenType.SelectWithDescriptions, typeof(SelectToken) },
            new object[] { @"SelectManyWithDescriptions ""Title"" ""Option1"" ""Description1"" ""Option2"" ""Description2""", TokenType.SelectManyWithDescriptions, typeof(SelectToken) },
            new object[] { @"SelectWithDescriptionsAndPreviews ""Title"" ""Option1"" ""Preview1"" ""Description1""", TokenType.SelectWithDescriptionsAndPreviews, typeof(SelectToken) },
            new object[] { @"SelectManyWithDescriptionsAndPreviews ""Title"" ""Option1"" ""Preview1"" ""Description1"" ""Option2"" ""Preview2"" ""Description2""", TokenType.SelectManyWithDescriptionsAndPreviews, typeof(SelectToken) },
            new object[] { @"Case ""Option1""", TokenType.Case, typeof(CaseToken) },
            new object[] { "Default", TokenType.Default, typeof(InstructionToken) },
            new object[] { "Break", TokenType.Break, typeof(EndFlowToken) },
            new object[] { "EndSelect", TokenType.EndSelect, typeof(EndFlowToken) },
            new object[] { "SelectVar myVar", TokenType.SelectVar, typeof(SelectVarToken) },
            new object[] { @"SelectString ""something""", TokenType.SelectString, typeof(SelectStringToken) },
            new object[] { "For Count myVar 1 10 2", TokenType.For, typeof(ForToken) },
            new object[] { "Continue", TokenType.Continue, typeof(InstructionToken) },
            new object[] { "Exit", TokenType.Exit, typeof(InstructionToken) },
            new object[] { "EndFor", TokenType.EndFor, typeof(EndFlowToken) },
            new object[] { "Goto myLabel", TokenType.Goto, typeof(GotoLabelToken) },
            new object[] { "Label myLabel", TokenType.Label, typeof(GotoLabelToken) },
            new object[] { "Return", TokenType.Return, typeof(InstructionToken) },
            new object[] { @"Message ""Message"" ""Title""", TokenType.Message, typeof(InstructionToken) },
            new object[] { @"DisplayImage ""Image.png"" ""Title""", TokenType.DisplayImage, typeof(InstructionToken) },
            new object[] { @"DisplayText ""Text.txt"" ""Title""", TokenType.DisplayText, typeof(InstructionToken) },
            new object[] { @"LoadBefore Plugin1 Plugin2", TokenType.LoadBefore, typeof(InstructionToken) },
            new object[] { @"LoadAfter Plugin1 Plugin2", TokenType.LoadAfter, typeof(InstructionToken) },
            new object[] { "UncheckESP plugin", TokenType.UncheckESP, typeof(InstructionToken) },
            new object[] { @"SetDeactivationWarning plugin ""Message""", TokenType.SetDeactivationWarning, typeof(InstructionToken) },
            new object[] { @"ConflictsWith ModName ""Comment"" Level", TokenType.ConflictsWith, typeof(InstructionToken) },
            new object[] { @"ConflictsWithRegex ModName ""Comment"" Level", TokenType.ConflictsWithRegex, typeof(InstructionToken) },
            new object[] { @"DependsOn ModName ""Comment""", TokenType.DependsOn, typeof(InstructionToken) },
            new object[] { @"DependsOnRegex ModName ""Comment""", TokenType.DependsOnRegex, typeof(InstructionToken) },
            new object[] { "iSet myVar -1 + ( 5 mod 3 ) + ( 6 - 3 )", TokenType.iSet, typeof(SetToken) },
            new object[] { "fSet myVar -1 + ( 5 mod 3 ) + ( 6 - 3 )", TokenType.fSet, typeof(SetToken) }
            //TODO: add remaining tokens (see http://timeslip.chorrol.com/obmmm/functionlist.htm)
        };
        
        [Theory]
        [MemberData(nameof(SingleLineTestData))]
        public void TestSingleLineTokenization(string text, TokenType expectedToken, Type expectedType)
        {
            var token = OBMMScriptHandler.TokenizeLine(text);
            Assert.Equal(expectedToken, token.Type);
            Assert.Equal(expectedType, token.GetType());
        }

        [Theory]
        [InlineData("OBMM-EVE_HGEC_BodyStockClothing", 137)]
        [InlineData("OBMM-HGECBodywithBBB", 142)]
        [InlineData("OBMM-NoMaaMBBBAnimation", 82)]
        [InlineData("OBMM-NoMaaMBreathingIdles", 32)]
        [InlineData("OBMM-RobertMaleBodyReplacer", 2017)]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public void TestScriptTokenization(string scriptPath, int count)
        {
            var file = Path.Combine("files", "scripts", scriptPath);
            Assert.True(File.Exists(file));

            var text = File.ReadAllText(file, Encoding.UTF8);
            var tokens = OBMMScriptHandler.TokenizeScript(text).ToList();
            Assert.NotEmpty(tokens);
            Assert.Equal(count, tokens.Count);
        }
    }
}
