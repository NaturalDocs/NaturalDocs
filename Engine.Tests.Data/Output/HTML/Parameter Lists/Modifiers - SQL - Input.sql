
/* Function: MicrosoftFunction1
	Parameters:
	@Param1 - Description
	@Param2 - Description
	@Param3 - Description
	@Param4 - Description
*/
FUNCTION MicrosoftFunction1
	@Param1 INTEGER,
	@Param2 VARCHAR(100),
	@Param3 VARCHAR(40) = 'string',
	@Param4 SCHEMA.NUMBER(5,6) = +.2e-1
RETURNS INTEGER;

/* Function: MicrosoftFunction2
	Parameters:
	@Param1 - Description
	@Param2 - Description
	@Param3 - Description
	@Param4 - Description
*/
CREATE FUNCTION MicrosoftFunction2 (
	@Param1 VARYING INTEGER NULL,
	@Param2 INTEGER OUT,
	@Param3 AS INTEGER NOT NULL READONLY,
	@Param4 AS VARYING SCHEMA.NUMBER NOT NULL = 12 READONLY)
RETURNS INTEGER
AS
BEGIN
END


/* Function: OracleFunction1
	Parameters:
	@Param1 - Description
	@Param2 - Description
	@Param3 - Description
	@Param4 - Description
*/
FUNCTION OracleFunction1 (
	Param1 INTEGER,
	Param2 VARCHAR2(100),
	Param3 NUMBER(4) DEFAULT 5,
	Param4 NUMBER(5,6) := 2e-1
	)
RETURN NUMBER AS
	x NUMBER;
BEGIN
END

/* Function: OracleFunction2
	Parameters:
	@Param1 - Description
	@Param2 - Description
	@Param3 - Description
	@Param4 - Description
*/
CREATE FUNCTION OracleFunction2 (
	Param1 IN INTEGER,
	Param2 OUT INTEGER NOT NULL,
	Param3 IN OUT INTEGER,
	Param4 IN OUT NOCOPY NUMBER NOT NULL
	)
RETURN NUMBER IS
	x NUMBER(2,3) NOT NULL;
BEGIN
END

/* Function: OracleFunction3
	Parameters:
	@Param1 - Description
	@Param2 - Description
*/
CREATE OR REPLACE FUNCTION OracleFunction3 (
	Param1 CLASS.VARIABLE%TYPE,
	Param2 IN OUT NOCOPY NUMBER(5, 6) NOT NULL := +6E2
	)
RETURN NUMBER AS
BEGIN
END
