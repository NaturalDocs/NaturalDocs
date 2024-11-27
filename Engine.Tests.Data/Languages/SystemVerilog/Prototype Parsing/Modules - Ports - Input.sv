

// Group: Empty Parentheses
// ___________________________________________________

// Module: EmptyA
module EmptyA ();
endmodule

// Module: EmptyB
module EmptyB ( );
endmodule



// Group: Misc
// ___________________________________________________

// Module: Attributes
module Attributes (
	(*x*) logic portA,
	(* x *) logic portB,
	(*x,y*) logic portC,
	(* x, y *) logic portD,
	(*z=12*) logic portE,
	(* z = 12 *) logic portF,
	(*x,y,z=12*) logic portG,
	(* x, y, z = 12 *) logic portH,
	(*x*)(*y*)(*z=12*) logic portI,
	(* x *) (* y *) (* z = 12 *) logic portJ,
	(* x, y *)(* z=12 *) logic portK,
	(* x *) (* y, z=12 *) logic portL);
endmodule

// Module: NetTypes
module NetTypes (
	supply0 portA,
	supply1 logic portB,
	tri logic unsigned portC,
	triand logic unsigned [7:0] portD,
	trior logic [7:0] portE,
	tri0 [7:0] portF,
	tri1 unsigned portG,
	uwire unsigned [7:0] portH,
	wire portI,
	wand logic portJ,
	wor logic unsigned portK);
endmodule