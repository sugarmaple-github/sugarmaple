//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.13.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from WAXPath.g4 by ANTLR 4.13.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using System;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.1")]
[System.CLSCompliant(false)]
public partial class WAXPathLexer : Lexer {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		T__0=1, T__1=2, T__2=3, T__3=4, T__4=5, T__5=6, T__6=7, T__7=8, T__8=9, 
		T__9=10, T__10=11, T__11=12, T__12=13, T__13=14, T__14=15, T__15=16, T__16=17, 
		T__17=18, T__18=19, T__19=20, ID=21, StringCharacters=22, WS=23;
	public static string[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static string[] modeNames = {
		"DEFAULT_MODE"
	};

	public static readonly string[] ruleNames = {
		"T__0", "T__1", "T__2", "T__3", "T__4", "T__5", "T__6", "T__7", "T__8", 
		"T__9", "T__10", "T__11", "T__12", "T__13", "T__14", "T__15", "T__16", 
		"T__17", "T__18", "T__19", "ID", "StringCharacters", "WS"
	};


	public WAXPathLexer(ICharStream input)
	: this(input, Console.Out, Console.Error) { }

	public WAXPathLexer(ICharStream input, TextWriter output, TextWriter errorOutput)
	: base(input, output, errorOutput)
	{
		Interpreter = new LexerATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	private static readonly string[] _LiteralNames = {
		null, "'/'", "'//'", "'doc'", "'('", "')'", "'*'", "'.'", "'..'", "'text()'", 
		"'@'", "'['", "']'", "','", "'='", "'=='", "'eq'", "'is'", "'and'", "'or'", 
		"'not'"
	};
	private static readonly string[] _SymbolicNames = {
		null, null, null, null, null, null, null, null, null, null, null, null, 
		null, null, null, null, null, null, null, null, null, "ID", "StringCharacters", 
		"WS"
	};
	public static readonly IVocabulary DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);

	[NotNull]
	public override IVocabulary Vocabulary
	{
		get
		{
			return DefaultVocabulary;
		}
	}

	public override string GrammarFileName { get { return "WAXPath.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override string[] ChannelNames { get { return channelNames; } }

	public override string[] ModeNames { get { return modeNames; } }

	public override int[] SerializedAtn { get { return _serializedATN; } }

	static WAXPathLexer() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}
	private static int[] _serializedATN = {
		4,0,23,130,6,-1,2,0,7,0,2,1,7,1,2,2,7,2,2,3,7,3,2,4,7,4,2,5,7,5,2,6,7,
		6,2,7,7,7,2,8,7,8,2,9,7,9,2,10,7,10,2,11,7,11,2,12,7,12,2,13,7,13,2,14,
		7,14,2,15,7,15,2,16,7,16,2,17,7,17,2,18,7,18,2,19,7,19,2,20,7,20,2,21,
		7,21,2,22,7,22,1,0,1,0,1,1,1,1,1,1,1,2,1,2,1,2,1,2,1,3,1,3,1,4,1,4,1,5,
		1,5,1,6,1,6,1,7,1,7,1,7,1,8,1,8,1,8,1,8,1,8,1,8,1,8,1,9,1,9,1,10,1,10,
		1,11,1,11,1,12,1,12,1,13,1,13,1,14,1,14,1,14,1,15,1,15,1,15,1,16,1,16,
		1,16,1,17,1,17,1,17,1,17,1,18,1,18,1,18,1,19,1,19,1,19,1,19,1,20,3,20,
		106,8,20,1,20,1,20,5,20,110,8,20,10,20,12,20,113,9,20,1,21,1,21,5,21,117,
		8,21,10,21,12,21,120,9,21,1,21,1,21,1,22,4,22,125,8,22,11,22,12,22,126,
		1,22,1,22,0,0,23,1,1,3,2,5,3,7,4,9,5,11,6,13,7,15,8,17,9,19,10,21,11,23,
		12,25,13,27,14,29,15,31,16,33,17,35,18,37,19,39,20,41,21,43,22,45,23,1,
		0,5,1,0,33,33,3,0,48,57,65,90,97,122,6,0,33,33,45,45,48,57,65,90,95,95,
		97,122,3,0,10,10,13,13,34,34,3,0,9,10,13,13,32,32,133,0,1,1,0,0,0,0,3,
		1,0,0,0,0,5,1,0,0,0,0,7,1,0,0,0,0,9,1,0,0,0,0,11,1,0,0,0,0,13,1,0,0,0,
		0,15,1,0,0,0,0,17,1,0,0,0,0,19,1,0,0,0,0,21,1,0,0,0,0,23,1,0,0,0,0,25,
		1,0,0,0,0,27,1,0,0,0,0,29,1,0,0,0,0,31,1,0,0,0,0,33,1,0,0,0,0,35,1,0,0,
		0,0,37,1,0,0,0,0,39,1,0,0,0,0,41,1,0,0,0,0,43,1,0,0,0,0,45,1,0,0,0,1,47,
		1,0,0,0,3,49,1,0,0,0,5,52,1,0,0,0,7,56,1,0,0,0,9,58,1,0,0,0,11,60,1,0,
		0,0,13,62,1,0,0,0,15,64,1,0,0,0,17,67,1,0,0,0,19,74,1,0,0,0,21,76,1,0,
		0,0,23,78,1,0,0,0,25,80,1,0,0,0,27,82,1,0,0,0,29,84,1,0,0,0,31,87,1,0,
		0,0,33,90,1,0,0,0,35,93,1,0,0,0,37,97,1,0,0,0,39,100,1,0,0,0,41,105,1,
		0,0,0,43,114,1,0,0,0,45,124,1,0,0,0,47,48,5,47,0,0,48,2,1,0,0,0,49,50,
		5,47,0,0,50,51,5,47,0,0,51,4,1,0,0,0,52,53,5,100,0,0,53,54,5,111,0,0,54,
		55,5,99,0,0,55,6,1,0,0,0,56,57,5,40,0,0,57,8,1,0,0,0,58,59,5,41,0,0,59,
		10,1,0,0,0,60,61,5,42,0,0,61,12,1,0,0,0,62,63,5,46,0,0,63,14,1,0,0,0,64,
		65,5,46,0,0,65,66,5,46,0,0,66,16,1,0,0,0,67,68,5,116,0,0,68,69,5,101,0,
		0,69,70,5,120,0,0,70,71,5,116,0,0,71,72,5,40,0,0,72,73,5,41,0,0,73,18,
		1,0,0,0,74,75,5,64,0,0,75,20,1,0,0,0,76,77,5,91,0,0,77,22,1,0,0,0,78,79,
		5,93,0,0,79,24,1,0,0,0,80,81,5,44,0,0,81,26,1,0,0,0,82,83,5,61,0,0,83,
		28,1,0,0,0,84,85,5,61,0,0,85,86,5,61,0,0,86,30,1,0,0,0,87,88,5,101,0,0,
		88,89,5,113,0,0,89,32,1,0,0,0,90,91,5,105,0,0,91,92,5,115,0,0,92,34,1,
		0,0,0,93,94,5,97,0,0,94,95,5,110,0,0,95,96,5,100,0,0,96,36,1,0,0,0,97,
		98,5,111,0,0,98,99,5,114,0,0,99,38,1,0,0,0,100,101,5,110,0,0,101,102,5,
		111,0,0,102,103,5,116,0,0,103,40,1,0,0,0,104,106,7,0,0,0,105,104,1,0,0,
		0,105,106,1,0,0,0,106,107,1,0,0,0,107,111,7,1,0,0,108,110,7,2,0,0,109,
		108,1,0,0,0,110,113,1,0,0,0,111,109,1,0,0,0,111,112,1,0,0,0,112,42,1,0,
		0,0,113,111,1,0,0,0,114,118,5,34,0,0,115,117,8,3,0,0,116,115,1,0,0,0,117,
		120,1,0,0,0,118,116,1,0,0,0,118,119,1,0,0,0,119,121,1,0,0,0,120,118,1,
		0,0,0,121,122,5,34,0,0,122,44,1,0,0,0,123,125,7,4,0,0,124,123,1,0,0,0,
		125,126,1,0,0,0,126,124,1,0,0,0,126,127,1,0,0,0,127,128,1,0,0,0,128,129,
		6,22,0,0,129,46,1,0,0,0,5,0,105,111,118,126,1,6,0,0
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
