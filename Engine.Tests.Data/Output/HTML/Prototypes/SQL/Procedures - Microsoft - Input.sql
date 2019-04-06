
-- Procedure: Params1
PROCEDURE Params1
	@Param1 INTEGER,
	@Param2 VARCHAR(100),
	@Param3 VARCHAR(40) = 'string',
	@Param4 SCHEMA.NUMBER(5,6) = +.2e-1,
	@Param5 VARCHAR(max) = 'escaped''apostrophe',
	@Param6 INTEGER = 0xdeadbeef;

-- Procedure: Params2
CREATE PROC Params2 ( 
	@Param1 VARYING INTEGER NULL,
	@Param2 INTEGER OUT,
	@Param3 INTEGER NOT NULL READONLY,
	@Param4 VARYING SCHEMA.NUMBER NOT NULL = 12 OUTPUT READONLY )
AS
BEGIN
END



-- Procedure: Complex
ALTER PROC Something.Complex
	@Param1 INTEGER
WITH NATIVE_COMPILATION, SCHEMABINDING, INLINE = OFF, EXECUTE AS 'AS'
FOR REPLICATION
AS
BEGIN
END



-- Function: Fakeout
CREATE PROCEDURE Fakeout
	@AS INTEGER,
	@WITH INTEGER,
	@RETURNS INTEGER
AS
BEGIN
END
