
-- Proc: #Identifier1$
PROCEDURE #Identifier1$;

-- Proc: $Id_ent#ifi$er2
PROCEDURE #$package.$Id_ent#ifi$er2;


-- Proc: NoParams1
PROCEDURE NoParams1;

-- Proc: NoParams2
PROCEDURE NoParams2
IS
BEGIN
END

-- Proc: NoParams3
PROCEDURE NoParams3
AS
BEGIN
END


-- Proc: Params1
PROCEDURE Params1 (
	Param1 INTEGER,
	Param2 VARCHAR2(100),
	Param3 NUMBER(4) DEFAULT 5,
	Param4 NUMBER(5,6) := 2e-1
	);

-- Proc: Params2
CREATE PROCEDURE Params2 (
	Param1 IN INTEGER,
	Param2 OUT INTEGER NOT NULL,
	Param3 IN OUT INTEGER,
	Param4 IN OUT NOCOPY NUMBER NOT NULL
	)
IS
BEGIN
END

-- Proc: Params3
CREATE OR REPLACE PROCEDURE Params3 (
	Param1 CLASS.VARIABLE%TYPE,
	Param2$ IN OUT NOCOPY NUMBER(5, 6) NOT NULL := +6E2
	)
AS
BEGIN
END


-- Proc: ExtraKeywords
CREATE OR REPLACE EDITIONABLE RANDOM_WORD PROCEDURE ExtraKeywords (
	Param1 INTEGER
	)
AS
BEGIN
END


-- Func: Modifiers1
CREATE PROCEDURE Modifiers1
SHARING = NONE AUTHID DEFINER ACCESSIBLE BY (FUNCTION Func1, PACKAGE A.B)
AS
BEGIN
END

-- Func: Modifiers2
CREATE PROCEDURE Modifiers2
SHARING = METADATA AUTHID CURRENT_USER DEFAULT COLLATION USING_NLS_COMP
IS
BEGIN
END
