
/* Function: Matching1
 *
 * Parameters:
 *    param1 - description
 *    param2 - description
 *    param3 - description
 */
@RequestForEnhancement(
	param1 = 12,
	param2 = "String",
	param3 = {"string 1", "string 2"}
	)
public static void Matching1 (String param1, Array param2, int param3) { }


/* Function: Matching2
 *
 * Parameters:
 *    param1 - description
 *    param2 - description
 *    param3 - description
 */
@RequestForEnhancement()
public static void Matching2 (String param1, Array param2, int param3) { }


/* Function: Matching3
 *
 * Parameters:
 *    param1 - description
 *    param2 - description
 *    param3 - description
 */
@RequestForEnhancement(
	param1 = 12,
	param2 = "String",
	param3 = {"string 1", "string 2"}
	)
public static void Matching3 () { }


/* Function: LongTypes
 *
 * Parameters:
 *    req - description
 *    length - description
 *    orderDirection - description
 *    onlyEnabled - description
 */
public void LongTypes (
    @Context HttpServletRequest req,
    @QueryParam("length") Integer length, 
    @DefaultValue("false") @QueryParam("order_direction") boolean orderDirection,
    @DefaultValue("true") @QueryParam("only_enabled") boolean onlyEnabled
	)
{  }
