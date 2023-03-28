
// Group: Empty
// ____________________________________________________________________________


// Module: Empty1
module Empty1 #() ();

// Module: Empty2
module Empty2 #( ) ( );



// Group: Parameter Assignments
// ____________________________________________________________________________


// Module: ParamAssignments
module ParamAssignments #(a = 12, b[7:0] = 15) ();



// Group: Parameter Ports
// ____________________________________________________________________________


// Module: BareParams
module BareParams #(int a, b, int c = 12) ();

// Module: KeywordParams
module KeywordParams #(parameter int a,
									 localparam int b = 12);

// Module: MixedParams
module MixedParams #(int a, b, parameter int c, d = 12, localparam int e = 9) ();



// Group: Type Assignments
// ____________________________________________________________________________


// Module: BareTypeAssignments
module BareTypeAssignments #(type a, b = int) ();

// Module: KeywordTypeAssignments
module KeywordTypeAssignments #(parameter type a = int, 
												  localparam type b = byte) ();

// Module: MixedTypeAssignments
module MixedTypeAssignments #(type a, b = int, parameter type c, d = byte, localparam type e = bit) ();



// Group: Everything
// ____________________________________________________________________________


// Module: Everything
module Everything #(a = 12,
							  b[7:0] = 15,
							  int c,
							  int d = 12,
							  parameter int e,
							  localparam int f = 12,
							  type g,
							  type h = int,
							  parameter type i = byte,
							  localparam	 type j = bit) ();
