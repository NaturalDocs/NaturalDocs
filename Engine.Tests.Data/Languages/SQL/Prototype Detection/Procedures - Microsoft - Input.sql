
-- Procedure: #Identifier1@
PROCEDURE #Identifier1@;

-- Procedure: ##Identifier2$
PROCEDURE ##Identifier2$;

-- Procedure: @Id_ent$ifi#er3
PROCEDURE @$schema#.@Id_ent$ifi#er3;



-- Procedure: Keyword_AS_Fakeout
PROCEDURE Keyword_AS_Fakeout;

-- Procedure: Keyword#AS@Fakeout
PROCEDURE Keyword#AS@Fakeout;

-- Procedure: Keyword_IS_Fakeout
PROCEDURE Keyword_IS_Fakeout;

-- Procedure: Keyword$IS#Fakeout
PROCEDURE Keyword$IS#Fakeout;



-- Procedure: DeclarationSyntax1_NoParams
PROCEDURE DeclarationSyntax1_NoParams;

-- Procedure: DeclarationSyntax2_NoParams
PROC DeclarationSyntax2_NoParams
AS
BEGIN
END

-- Procedure: DeclarationSyntax3_NoParams
CREATE PROCEDURE DeclarationSyntax3_NoParams;

-- Procedure: DeclarationSyntax4_NoParams
CREATE OR ALTER PROC DeclarationSyntax4_NoParams
AS
BEGIN
END

-- Procedure: DeclarationSyntax5_NoParams
ALTER PROC DeclarationSyntax5_NoParams;



-- Procedure: Params1
PROCEDURE Params1
	@Param1 INTEGER,
	@Param2 VARCHAR(100),
	@Param3 VARCHAR(40) = 'string',
	@Param4 AS SCHEMA.NUMBER(5,6) = +.2e-1,
	@Param5 AS VARCHAR(max) = 'escaped''apostrophe',
	@Param6 AS INTEGER = 0xdeadbeef;

-- Procedure: Params2
CREATE PROC Params2 (
	@Param1 VARYING INTEGER NULL,
	@Param2 INTEGER OUT,
	@Param3 AS INTEGER NOT NULL READONLY,
	@Param4 AS VARYING SCHEMA.NUMBER NOT NULL = 12 OUTPUT READONLY)
AS
BEGIN
END



-- Procedure: WithStatement
ALTER PROC WithStatement
	@Param1 INTEGER
WITH NATIVE_COMPILATION, SCHEMABINDING, INLINE = OFF, EXECUTE AS 'AS'
FOR REPLICATION
AS
BEGIN
END


-- Function: Fakeout
CREATE PROC Fakeout
	@AS INTEGER,
	@WITH INTEGER,
	@RETURNS INTEGER
AS
BEGIN
END
