

// Group: Empty Parentheses
// ___________________________________________________


// Module: EmptyA
module EmptyA ();
endmodule

// Module: EmptyB
module EmptyB ( );
endmodule



// Group: Port Definition Components
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


// Module: Directions
module Directions (
	input portA,
	output portB,
	inout portC,
	ref portD,
	input logic portAA,
	output logic portBB,
	inout logic portCC,
	ref logic portDD
	);
endmodule


// Module: NetTypes
//
// Modules may define a net type, data type, or both.  The data type may be implicit, meaning
// only some properties are defined like signing or packed dimensions.
//
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


// Module: UserDefinedNetTypes
//
// User-defined net types are allowed.  Fortunately net types and data types are lumped together
// as the parameter type so we don't need to distinguish between whether an unknown identifier
// is a user-defined net type or a user-defined data type.
//
module UserDefinedNetTypes (
	NetTypeA portA,
	NetTypeB logic portB,
	NetTypeC logic unsigned portC,
	NetTypeD logic unsigned [7:0] portD,
	NetTypeE logic [7:0] portE,
	NetTypeF [7:0] portF,
	NetTypeG unsigned portG,
	NetTypeH unsigned [7:0] portH);
endmodule


// Module: Interconnects
//
// According to the language specification "interconnect" may be followed by implicit_data_type, which
// means it could have signing and/or packed dimensions.  In practice none of the tested tools allowed
// that so we'll ignore it.
//
// - Aldec Riviera Pro 2023.04
// - Cadence Xcelium 23.09
// - Siemens Questa 2023.3
// - Synopsys VCS 2023.03
//
module Interconnects (
	interconnect portA);
endmodule


// Module: InterfacesAndModPorts
module InterfacesAndModPorts (
	interface portA,
	interface.ModPort portB,
	UserInterface portC,
	UserInterface.ModPort portD);
endmodule


// Module: VariableTypes
module VariableTypes (
	input bit portA,
	input logic unsigned portB,
	input reg [7:0] portC,

	input byte portD,
	input shortint portE,
	input int unsigned portF,
	input longint unsigned portG,
	input integer portH,
	input time portI,

	input shortreal portJ,
	input real portK,
	input realtime portL);
endmodule



// Group: Net Types and Data Types
// ___________________________________________________
//
// The parser should be able to handle any combination of net types and data types.  Either or both
// can be user-defined, but since they are both lumped together as the parameter type we don't need
// to worry about distinguishing between them when we find an unknown identifier.


// Module: NetTypesAndDataTypes
module NetTypesAndDataTypes (

	supply0 netTypeOnlyA,

	supply0 netTypeOnlyAA[2],

	logic dataTypeOnlyA,
	logic [7:0] dataTypeOnlyB,
	logic unsigned dataTypeOnlyC,
	logic unsigned [7:0] dataTypeOnlyD,

	logic dataTypeOnlyAA[2],
	logic [7:0] dataTypeOnlyBB[2],
	logic unsigned dataTypeOnlyCC[2],
	logic unsigned [7:0] dataTypeOnlyDD[2],

	[7:0] implicitDataTypeOnlyA,
	unsigned implicitDataTypeOnlyB,
	unsigned [7:0] implicitDataTypeOnlyC,

	[7:0] implicitDataTypeOnlyAA[2],
	unsigned implicitDataTypeOnlyBB[2],
	unsigned [7:0] implicitDataTypeOnlyCC[2],

	supply0 logic netTypeAndDataTypeA,
	supply0 logic [7:0] netTypeAndDataTypeB,
	supply0 logic unsigned netTypeAndDataTypeC,
	supply0 logic unsigned [7:0] netTypeAndDataTypeD,

	supply0 logic netTypeAndDataTypeAA[2],
	supply0 logic [7:0] netTypeAndDataTypeBB[2],
	supply0 logic unsigned netTypeAndDataTypeCC[2],
	supply0 logic unsigned [7:0] netTypeAndDataTypeDD[2],

	supply0 [7:0] netTypeAndImplicitDataTypeA,
	supply0 unsigned netTypeAndImplicitDataTypeB,
	supply0 unsigned [7:0] netTypeAndImplicitDataTypeC,

	supply0 [7:0] netTypeAndImplicitDataTypeAA[2],
	supply0 unsigned netTypeAndImplicitDataTypeBB[2],
	supply0 unsigned [7:0] netTypeAndImplicitDataTypeCC[2]);

endmodule


// Module: UserNetTypesAndDataTypes
module UserNetTypesAndDataTypes (

	UserNetType userNetTypeOnlyA,

	UserNetType userNetTypeOnlyAA[2],

	UserNetType logic userNetTypeAndDataTypeA,
	UserNetType logic [7:0] userNetTypeAndDataTypeB,
	UserNetType logic unsigned userNetTypeAndDataTypeC,
	UserNetType logic unsigned [7:0] userNetTypeAndDataTypeD,

	UserNetType logic userNetTypeAndDataTypeAA[2],
	UserNetType logic [7:0] userNetTypeAndDataTypeBB[2],
	UserNetType logic unsigned userNetTypeAndDataTypeCC[2],
	UserNetType logic unsigned [7:0] userNetTypeAndDataTypeDD[2],

	UserNetType [7:0] userNetTypeAndImplicitDataTypeA,
	UserNetType unsigned userNetTypeAndImplicitDataTypeB,
	UserNetType unsigned [7:0] userNetTypeAndImplicitDataTypeC,

	UserNetType [7:0] userNetTypeAndImplicitDataTypeAA[2],
	UserNetType unsigned userNetTypeAndImplicitDataTypeBB[2],
	UserNetType unsigned [7:0] userNetTypeAndImplicitDataTypeCC[2]);

endmodule


// Module: NetTypesAndUserDataTypes
module NetTypesAndUserDataTypes (

	UserDataType userDataTypeOnlyA,
	UserDataType [7:0] userDataTypeOnlyB,
	UserDataType unsigned userDataTypeOnlyC,
	UserDataType unsigned [7:0] userDataTypeOnlyD,

	UserDataType userDataTypeOnlyAA[2],
	UserDataType [7:0] userDataTypeOnlyBB[2],
	UserDataType unsigned userDataTypeOnlyCC[2],
	UserDataType unsigned [7:0] userDataTypeOnlyDD[2],

	supply0 UserDataType netTypeAndUserDataTypeA,
	supply0 UserDataType [7:0] netTypeAndUserDataTypeB,
	supply0 UserDataType unsigned netTypeAndUserDataTypeC,
	supply0 UserDataType unsigned [7:0] netTypeAndUserDataTypeD,

	supply0 UserDataType netTypeAndUserDataTypeAA[2],
	supply0 UserDataType [7:0] netTypeAndUserDataTypeBB[2],
	supply0 UserDataType unsigned netTypeAndUserDataTypeCC[2],
	supply0 UserDataType unsigned [7:0] netTypeAndUserDataTypeDD[2]);

endmodule


// Module: UserNetTypesAndUserDataTypes
module UserNetTypesAndUserDataTypes (

	UserNetType UserDataType userNetTypeAndUserDataTypeA,
	UserNetType UserDataType [7:0] userNetTypeAndUserDataTypeB,
	UserNetType UserDataType unsigned userNetTypeAndUserDataTypeC,
	UserNetType UserDataType unsigned [7:0] userNetTypeAndUserDataTypeD,

	UserNetType UserDataType userNetTypeAndUserDataTypeAA[2],
	UserNetType UserDataType [7:0] userNetTypeAndUserDataTypeBB[2],
	UserNetType UserDataType unsigned userNetTypeAndUserDataTypeCC[2],
	UserNetType UserDataType unsigned [7:0] userNetTypeAndUserDataTypeDD[2]);

endmodule
