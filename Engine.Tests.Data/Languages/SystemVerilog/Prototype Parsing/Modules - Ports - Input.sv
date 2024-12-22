

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
	input realtime portL,

	input string portM,
	input event portN);
endmodule


// Module: StructsAndUnions
// Structs and unions are allowed to be defined inline in port declarations.
module StructsAndUnions (
	input struct { bit a1; } portA,
	input union { bit b1; bit b2; } portB,

	input struct tagged { bit c1; bit c2; } portC,
	input union tagged { bit d1; bit d2; } portD,

	input struct packed { bit e1; bit e2; } portE,
	input union packed { bit f1; bit f2; } portF,
	input struct packed signed { bit g1; bit g2; } portG,
	input union packed unsigned { bit h1; bit h2; } portH,

	input struct { bit i1; bit i2; } [1:0] portI,
	input union tagged { bit j1; bit j2; } [1:0] portJ,
	input struct packed { bit k1; bit k2; } [1:0] portK,
	input union packed unsigned { bit l1; bit l2; } [1:0] portL);
endmodule


// Module: Enums
// Enums are allowed to be defined inline in port declarations.
module Enums (
	input enum { a1, a2 } portA,

	input enum bit { b1, b2 } portB,
	input enum bit unsigned { c1, c2 } portC,
	input enum bit unsigned [7:0] { d1, d2 } portD,
	input enum bit [7:0] { e1, e2 } portE,

	input enum unsigned { f1, f2 } portF,
	input enum unsigned [7:0] { g1, g2 } portG,
	input enum [7:0] { h1, h2 } portH,

	input enum UserType { i1, i2 } portI,
	input enum UserType [7:0]{  j1, j2 } portJ,

	input enum { k1, k2 } [7:0] portK,

	input enum bit { l1, l2 } [7:0] portL,
	input enum bit unsigned { m1, m2 } [7:0] portM,
	input enum bit unsigned [7:0] { n1, n2 } [7:0] portN,
	input enum bit [7:0] { o1, o2 } [7:0] portO,

	input enum unsigned { p1, p2 } [7:0] portP,
	input enum unsigned [7:0] { q1, q2 } [7:0] portQ,
	input enum [7:0] { r1, r2 } [7:0] portR,

	input enum UserType { s1, s2 } [7:0] portS,
	input enum UserType [7:0] { t1, t2 } [7:0] portT,

	input enum { u1, u2 = 2, u3[1], u4[1:4] = 'b0100 } portU);
endmodule


// Module: QualifiedAndParameterizedTypes
module QualifiedAndParameterizedTypes (
	UserType portA,
	UserType [7:0] portB,

	UserPackage::UserType portC,
	UserPackage::UserType [7:0] portD,

	UserPackageA::UserPackageB::UserType portE,
	UserPackageA::UserPackageB::UserType [7:0] portF,

	$unit::UserType portG,
	$unit::UserPackage::UserType portH,

	UserPackage#(12) portI,
	UserPackage #(1, 2, 3) portJ,
	UserPackage #(, 2, 3) portK,
	UserPackage #(1, , 3) portL,
	UserPackage #(1, 2, ) portM,
	UserPackage #(1, , ) portN,
	UserPackage #(, 2, ) portO,
	UserPackage #(, , 3) portP,
	UserPackage #(, , ) portQ,

	UserPackage #(.r1, .r2(), .r3(12), .r4(name), .r5(name[7:0])) portR,
	UserPackage #(.s1, .s2 (), .s3 (12), .s4 (name), .s5 (name[7:0])) portS,

	UserPackage #(.*) portT,
	UserPackage #(.*, .u1, .u2(), .u3(12), .u4(name), .u5(name[7:0])) portU);
endmodule


// Module: DoubleParameterizedTypes
//
// Unfortunately Natural Docs doesn't have a good way to support parameterized qualifiers (UserPackageA#(12)::) so
// we won't worry about what the results are for Base Type or Link Candidates on those ones.  We only care that Name
// and Full Type are correct.
//
module DoubleParameterizedTypes (
	UserPackageA#(12)::UserPackageB portA,
	UserPackageA #(12)::UserPackageB portB,
	UserPackageA::UserPackageB#(13) portC,
	UserPackageA::UserPackageB #(13) portD,
	UserPackageA#(12)::UserPackageB#(13) portE,
	UserPackageA #(12)::UserPackageB #(13) portF,
	$unit::UserPackageA #(.*, .v1, .v2(), .v3(12))::UserPackageB#(, 12, , 4) portG);
endmodule



// Topic: Unsupported Types
//
// These data types cannot be used as module ports:
// - chandles
// - virtual interfaces
//



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
