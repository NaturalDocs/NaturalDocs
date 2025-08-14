
// Function: Marker_Annotation1
@Preliminary public void Marker_Annotation1 () { }

// Function: Marker_Annotation2
@ Preliminary public void Marker_Annotation2 () { }

// Function: Marker_Annotation3
@Preliminary() public void Marker_Annotation3 () { }

// Function: Marker_Annotation4
@ Preliminary () public void Marker_Annotation4 () { }



// Function: Single_Element_Annotation1
@Copyright("Text String")
public void Single_Element_Annotation1 () { }

// Function: Single_Element_Annotation2
@Endorsers({"String 1", "String 2"})
public void Single_Element_Annotation2 () { }



// Function: Key_Value_Annotation1
@RequestForEnhancement(
	id = 2868724,
	engineer = "String",
	date = "4/1/2004"
	)
public static void Key_Value_Annotation1 () { }



// Function: Multiple_Annotations
@Preliminary
@Copyright("Text String")
@RequestForEnhancement(
	id = 2868724,
	engineer = "String",
	array = {"string 1", "string 2"}
	)
@Endorsers({"String 1", "String 2"})
public static void Multiple_Annotations () { }



// Function: JAXRS_Annotations
@GET
@Produces(MediaType.APPLICATION_JSON)
public void JAXRS_Annotations (
    @Context HttpServletRequest req,
    @QueryParam("token") String token,
    @QueryParam("start") int start, 
    @QueryParam("length") Integer length, 
    @QueryParam("valid_from") String validFrom, 
    @QueryParam("valid_to") String validTo, 
    @DefaultValue("id") @QueryParam("order_by") String orderBy,
    @DefaultValue("false") @QueryParam("order_direction") boolean orderDirection,
    @DefaultValue("true") @QueryParam("only_enabled") boolean onlyEnabled
	)
{  }
