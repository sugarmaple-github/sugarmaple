grammar WAXPath;
//parser

ap: filePath '/' rp # apSlash | filePath '//' rp # apTwoSlash;

filePath: 'doc' '(' fileName ')';

rp:
	tagName					# rpTagName
	| '*'					# rpChildren
	| '.'					# rpCurrent
	| '..'					# rpParent
	| 'text()'				# rpText
	| '@' attName			# rpAttName
	| '(' rp ')'			# rpBracket
	| rp '/' rp				# rpSlash
	| rp '//' rp			# rpDoubleSlash
	| rp '[' pathFilter ']'	# rpFilter
	| rp ',' rp				# rpComma;
pathFilter:
	rp								# pfRp
	| rp '=' rp						# pfEq
	| rp '==' rp					# pfIs
	| rp 'eq' rp					# pfEq
	| rp 'is' rp					# pfIs
	| '(' pathFilter ')'			# pfBracket
	| pathFilter 'and' pathFilter	# pfAnd
	| pathFilter 'or' pathFilter	# pfOr
	| 'not' pathFilter				# pfNot;

tagName: ID;
attName: ID;
fileName: StringCharacters;

//lexer
ID: [!]? [a-zA-Z0-9] [a-zA-Z0-9_!-]*;
StringCharacters: '"' ~('\r' | '\n' | '"')* '"';

WS: [ \t\r\n]+ -> skip;