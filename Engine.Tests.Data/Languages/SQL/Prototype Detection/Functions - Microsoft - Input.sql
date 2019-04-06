
-- Function: #Identifier1@
FUNCTION #Identifier1@;

-- Function: ##Identifier2$
FUNCTION ##Identifier2$;

-- Function: @Id_ent$if#er3
FUNCTION @$schema#.@Id_ent$if#er3;



-- Function: Keyword_AS_Fakeout
FUNCTION Keyword_AS_Fakeout;

-- Function: Keyword#AS@Fakeout
FUNCTION Keyword#AS@Fakeout;

-- Function: Keyword_IS_Fakeout
FUNCTION Keyword_IS_Fakeout;

-- Function: Keyword$IS#Fakeout
FUNCTION Keyword$IS#Fakeout;



-- Function: DeclarationSyntax1_NoParams
FUNCTION DeclarationSyntax1_NoParams
RETURNS INTEGER;

-- Function: DeclarationSyntax2_NoParams
CREATE FUNCTION DeclarationSyntax2_NoParams
RETURNS INTEGER
AS
BEGIN
END

-- Function: DeclarationSyntax3_NoParams
CREATE OR ALTER FUNCTION DeclarationSyntax3_NoParams
RETURNS TABLE
BEGIN
END

-- Function: DeclarationSyntax4_NoParams
ALTER FUNCTION DeclarationSyntax4_NoParams
RETURNS TABLE
	(
	Column1 INT PRIMARY KEY NOT NULL,  
	Column2 NVARCHAR(255) DEFAULT 'string'
	)
AS
BEGIN
END



-- Function: Params1
FUNCTION Params1
	@Param1 INTEGER,
	@Param2 VARCHAR(100),
	@Param3 VARCHAR(40) = 'string',
	@Param4 AS SCHEMA.NUMBER(5,6) = +.2e-1,
	@Param5 AS VARCHAR(max) = 'escaped''apostrophe',
	@Param6 AS INTEGER = 0xdeadbeef
RETURNS INTEGER;

-- Function: @Params2
CREATE FUNCTION @Params2 (
	@Param1 VARYING INTEGER NULL,
	@Param2 INTEGER OUT,
	@Param3 AS INTEGER NOT NULL READONLY,
	@Param4 AS VARYING SCHEMA.NUMBER NOT NULL = 12 OUTPUT READONLY)
RETURNS INTEGER
AS
BEGIN
END



-- Function: With1
FUNCTION With1
RETURNS INTEGER
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS 'Name'
AS
BEGIN
END

-- Function: With2
FUNCTION With2
RETURNS INTEGER
WITH EXEC AS 'RETURNS', INLINE = OFF, RETURNS NULL ON NULL INPUT
BEGIN
END

-- Function: With3
CREATE OR ALTER FUNCTION With3
RETURNS TABLE
WITH EXECUTE AS 'BEGIN'
RETURN SELECT * FROM x;


-- Function: Fakeout
CREATE FUNCTION Fakeout
	@AS INTEGER,
	@WITH INTEGER,
	@RETURNS INTEGER
RETURNS @WITH TABLE
	(
	Column1 INT
	)
AS
RETURN (SELECT * FROM x);
