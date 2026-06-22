
-- Function: ReturnTables
--
-- Parameters:
--    @param1 - description
--    @param2 - description
--    returnField1 - description
--    returnField2 - description
--
FUNCTION ReturnTables
	@param1 INTEGER,
	@param2 VARCHAR(100)
RETURNS TABLE
	(
	returnField1 INT PRIMARY KEY NOT NULL,
	returnField2 NVARCHAR(255) DEFAULT 'string'
	)
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS 'AS', INLINE = OFF
AS
BEGIN
END
