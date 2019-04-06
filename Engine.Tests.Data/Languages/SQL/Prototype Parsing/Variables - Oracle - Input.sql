
-- var: Simple
Simple INTEGER;

-- var: $Identifier_Symbols#
$Identifier_Symbols# INTEGER;

-- var: Attributes
Attributes CONSTANT INTEGER NOT NULL;

-- var: DefaultValue1
DefaultValue1 INTEGER DEFAULT 1;

-- var: DefaultValue2
DefaultValue2 VARCHAR := "String";

-- var: DefaultValue3
DefaultValue3 INTEGER default 6E4;

-- var: DefaultValue4
DefaultValue4 Real := +1.3e6f;

-- var: DefaultValue5
DefaultValue5 real Default -.5e-4d;

-- var: DefaultValue6
DefaultValue6 VarChar(12) := 'Str''ing';

-- var: TypeParens1
TypeParens1 INTEGER(6);

-- var: TypeParens2
TypeParens2 INTEGER(6, 8);

-- var: IndirectType1
IndirectType1 SomethingElse%TYPE;

-- var: IndirectType2
IndirectType2 MyTable.Column%TYPE;

-- var: IndirectType3
IndirectType3 MyTable%ROWTYPE;

-- var: AllCombined1
AllCombined1 CONSTANT INTEGER(1,2) NOT NULL := 3;

-- var: AllCombined2
AllCombined2 CONSTANT Package.SomethingElse%TYPE NOT NULL DEFAULT 'String';
