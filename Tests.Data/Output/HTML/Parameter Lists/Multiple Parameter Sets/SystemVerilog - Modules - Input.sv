
// Module: Matching_AllThree
//
// Parameters:
//    param1 - description
//    param2 - description
//
(* param1 = 12,
	param2 = "string" *)
module Matching_AllThree #(parameter param1 = 1,
										   parameter param2 = 2)
										  (logic param1,
										   reg param2);
endmodule


// Module: Matching_AttributesOnly
//
// Parameters:
//    param1 - description
//    param2 - description
//
(* param1 = 12,
	param2 = "string" *)
module Matching_AttributesOnly #() ();
endmodule


// Module: Matching_ParameterPortsOnly
//
// Parameters:
//    param1 - description
//    param2 - description
//
module Matching_ParameterPortsOnly #(parameter param1 = 1,
															  parameter param2 = 2) ( );
endmodule


// Module: Matching_PortsOnly
//
// Parameters:
//    param1 - description
//    param2 - description
//
module Matching_PortsOnly #( ) (logic param1,
												   reg param2);
endmodule


// Module: Matching_NoAttributes
//
// Parameters:
//    param1 - description
//    param2 - description
//
module Matching_NoAttributes #(parameter param1 = 1,
												   parameter param2 = 2)
												 (logic param1,
												  reg param2);
endmodule


// Module: Matching_NoParameterPorts
//
// Parameters:
//    param1 - description
//    param2 - description
//
(* param1 = 12,
	param2 = "string" *)
module Matching_NoParameterPorts #( ) (logic param1,
																reg param2);
endmodule


// Module: Matching_NoPorts
//
// Parameters:
//    param1 - description
//    param2 - description
//
(* param1 = 12,
	param2 = "string" *)
module Matching_NoPorts #(parameter param1 = 1,
											parameter param2 = 2) ( );
endmodule
