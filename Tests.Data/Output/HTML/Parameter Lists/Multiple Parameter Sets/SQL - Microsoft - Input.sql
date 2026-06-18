
-- Function: Matching_AllThree
--
-- Parameters:
--    param1 - description
--    param2 - description
--    @param1 - description
--    @param2 - description
--
FUNCTION Matching_AllThree
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


-- Function: Matching_ParamsOnly
--
-- Parameters:
--    param1 - description
--    param2 - description
--    @param1 - description
--    @param2 - description
--
FUNCTION Matching_ParamsOnly
	@param1 INTEGER,
	@param2 VARCHAR(100)
RETURNS INT
AS
BEGIN
END


-- Function: Matching_ReturnTableOnly
--
-- Parameters:
--    param1 - description
--    param2 - description
--    @param1 - description
--    @param2 - description
--
FUNCTION Matching_ReturnTableOnly
RETURNS TABLE
	(
	param1 INT PRIMARY KEY NOT NULL,
	param2 NVARCHAR(255) DEFAULT 'string'
	)
AS
BEGIN
END


-- Function: Matching_WithOnly
--
-- Parameters:
--    param1 - description
--    param2 - description
--    @param1 - description
--    @param2 - description
--
FUNCTION Matching_WithOnly
RETURNS INT
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS 'AS', INLINE = OFF
AS
BEGIN
END


-- Function: Matching_NoParams
--
-- Parameters:
--    param1 - description
--    param2 - description
--    @param1 - description
--    @param2 - description
--
FUNCTION Matching_NoParams
RETURNS TABLE
	(
	param1 INT PRIMARY KEY NOT NULL,
	param2 NVARCHAR(255) DEFAULT 'string'
	)
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS 'AS', INLINE = OFF
AS
BEGIN
END


-- Function: Matching_NoReturnTable
--
-- Parameters:
--    param1 - description
--    param2 - description
--    @param1 - description
--    @param2 - description
--
FUNCTION Matching_NoReturnTable
	@param1 INTEGER,
	@param2 VARCHAR(100)
RETURNS INT
WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS 'AS', INLINE = OFF
AS
BEGIN
END


-- Function: Matching_NoWith
--
-- Parameters:
--    param1 - description
--    param2 - description
--    @param1 - description
--    @param2 - description
--
FUNCTION Matching_NoWith
	@param1 INTEGER,
	@param2 VARCHAR(100)
RETURNS TABLE
	(
	param1 INT PRIMARY KEY NOT NULL,
	param2 NVARCHAR(255) DEFAULT 'string'
	)
AS
BEGIN
END

