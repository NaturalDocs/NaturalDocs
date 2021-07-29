CREATE OR REPLACE TYPE ObjectType AS OBJECT (

	-- Variable: Var1
	Var1 VARCHAR2(10 Byte),

	-- Function: Func1
	CONSTRUCTOR FUNCTION Func1 (
		x IN OUT NOCOPY ObjectType,
		y VARCHAR2
		) RETURN x AS RESULT,

	-- Function: Func2
	STATIC PROCEDURE Func2,

	-- Function: Func3
	MAP MEMBER FUNCTION Func3 RETURN NUMBER,

	-- Function: Func4
	ORDER MEMBER FUNCTION Func4 RETURN NUMBER,

	-- Function: Func5
	MEMBER FUNCTION Func5 (
		x PLS_INTEGER,
		y VARCHAR2
		) RETURN BOOLEAN,

	-- Function: Func6
	MEMBER PROCEDURE Func6
	);