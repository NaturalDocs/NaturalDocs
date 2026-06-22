
-- Function: ReturnTables
--
-- Types should be taken from the parameters over return table values of the same name.
--
-- Parameters:
--    @param1 - description
--    @param2 - description
--
FUNCTION ReturnTables
	@param1 INTEGER,
	@param2 VARCHAR(100)
RETURNS TABLE
	(
	param1 INT PRIMARY KEY NOT NULL,
	param2 NVARCHAR(255) DEFAULT 'string'
	)
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS 'AS', INLINE = OFF
AS
BEGIN
END
