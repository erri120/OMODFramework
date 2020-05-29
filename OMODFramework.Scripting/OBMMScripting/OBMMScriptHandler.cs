using System.Collections.Generic;

namespace OMODFramework.Scripting
{
    public partial class OBMMScriptHandler : AScriptHandler
    {
        private OMOD _omod = null!;
        private HashSet<Token> _tokens = new HashSet<Token>();
        private Dictionary<string, string> _variables = new Dictionary<string, string>();
        private Stack<Token> _stack = new Stack<Token>();

        internal override ScriptReturnData Execute(OMOD omod, string script, IScriptSettings settings)
        {
            _omod = omod;
            var srd = new ScriptReturnData();

            TokenizeScript(script);

            return srd;
        }
    }
}
