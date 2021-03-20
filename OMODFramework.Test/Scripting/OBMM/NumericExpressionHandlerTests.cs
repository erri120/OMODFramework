using OMODFramework.Scripting.ScriptHandlers.OBMMScript;
using Xunit;

namespace OMODFramework.Test.Scripting.OBMM
{
    public class NumericExpressionHandlerTests
    {
        [Theory]
        [InlineData("-1 + ( 5 mod 3 ) + ( 6 - 3 )", 4)]
        [InlineData("5 * ( 12 ^ 3 ) - not 20", 8661)]
        [InlineData("( 10 and 3 ) or 5", 7)]
        [InlineData("( ( 10 / 2 ) % 3 ) xor 1234", 1232)]
        public void TestIntExpressions(string expression, int output)
        {
            var list = expression.Split(' ');
            var result = NumericExpressionHandler.EvaluateIntExpression(list);
            Assert.Equal(output, result);
        }

        [Theory]
        [InlineData("1E+10 / 10", 1E+9)]
        [InlineData("3.4 + ( sin 1E+3 ) * ( 2 ^ 100 )", 1.0481943458718355E+30)]
        [InlineData("( log 1 ) - 1", -1.0)]
        [InlineData("ln 1", 0.0)]
        [InlineData("sin 12 + sinh 12", 81376.85913351185)]
        [InlineData("cos 34 + cosh 34", 291730871263726.56)]
        [InlineData("tan 2 + tanh 2", -1.221012283185702)]
        [InlineData("1E+2 exp 2", 100.0)]
        [InlineData("( 1E+2 mod 10 ) + 2.3 % 5.3", 2.3)]
        public void TestFloatExpressions(string expression, double output)
        {
            var list = expression.Split(' ');
            var result = NumericExpressionHandler.EvaluateFloatExpression(list);
            Assert.Equal(output, result);
        }
    }
}
