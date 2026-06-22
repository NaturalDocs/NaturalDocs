
// Module: PortsOverAttributes
//
// Types should be taken from the ports over attributes of the same name.
//
// Parameters:
//    param1 - description
//    param2 - description
//
(* param1 = 12,
	param2 = "string" *)
module PortsOverAttributes (logic param1,
										reg param2);
endmodule


// Module: PortsOverParameterPorts
//
// Types should be taken from the ports over parameter ports of the same name.  This isn't valid syntax
// but we want it as a test nonetheless.
//
// Parameters:
//    param1 - description
//    param2 - description
//
module PortsOverParameterPorts #(parameter param1 = 1,
												 parameter param2 = 2)
												(logic param1,
												 reg param2);
endmodule


// Module: PortsOverAttributesAndParameterPorts
//
// Types should be taken from the ports over attributes and parameter ports of the same name.  Ports
// and parameter ports with the same name isn't valid syntax but we want it as a test nonetheless.
//
// Parameters:
//    param1 - description
//    param2 - description
//
(* param1 = 12,
	param2 = "string" *)
module PortsOverAttributesAndParameterPorts #(parameter param1 = 1,
																	parameter param2 = 2)
																   (logic param1,
																	reg param2);
endmodule


// Module: ParameterPortsOverAttributes
//
// Types should be taken from the parameter ports over attributes of the same name.
//
// Parameters:
//    param1 - description
//    param2 - description
//
(* param1 = 12,
	param2 = "string" *)
module ParameterPortsOverAttributes #(parameter param1 = 1,
														parameter param2 = 2) ( );
endmodule
