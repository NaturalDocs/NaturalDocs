
-- var: @Simple1
DECLARE @Simple1 INTEGER;

-- var: @Simple2
DECLARE @Simple2 AS INTEGER;

-- var: @DefaultValue1
DECLARE @DefaultValue1 INTEGER = 1;

-- var: @DefaultValue2
DECLARE @DefaultValue2 AS VARCHAR = "String";

-- var: @DefaultValue3
DECLARE @DefaultValue3 INTEGER = 6E4;

-- var: @DefaultValue4
DECLARE @DefaultValue4 AS Real = +1.3e6;

-- var: @DefaultValue5
DECLARE @DefaultValue5 real = -.5e-4;

-- var: @DefaultValue6
DECLARE @DefaultValue6 AS VarChar(12) = 'Str''ing';

-- var: @TypeParens1
DECLARE @TypeParens1 NUMERIC(6);

-- var: @TypeParens2
DECLARE @TypeParens2 AS NUMERIC(6, 8);

-- var: @AllCombined
DECLARE @AllCombined AS NUMERIC(3,4) = +3.5e2;

-- var: @Cursor
DECLARE @Cursor CURSOR;

-- var: @Table1
DECLARE @Table1 TABLE (
    Column1 INT PRIMARY KEY NOT NULL,  
    Column2 NVARCHAR(255) DEFAULT 'string'
	);

-- var: @Table2
DECLARE @Table2 AS TABLE(Column1 INT PRIMARY KEY NOT NULL,Column2 NVARCHAR(255) DEFAULT 'string');
