
// Group: Attributes

// Module: NoValue
(* optimize_power *)
module NoValue ();

// Module: WithValue
(* optimize_power=1 *)
module WithValue ();

// Module: ManyValues
(* optimize_power = 0, mode = "cla",
    no_value, expr_value = (12 + 9) % 2,
	cond_value = x ? 1 : 0 *)
module ManyValues ();

// Module: SeparateValues
(* optimize_power = 0 *)
(* mode = "cla" *)
(* no_value *)
(* expr_value = (12 + 9) % 2,
	cond_value = x ? 1 : 0 *)
module SeparateValues ();

// Module: Spacing
(*optimize_power=0,mode="cla"*)(*no_value*)module Spacing();


// Group: Extern

// Module: ExternAttributeMultipleLines
// "extern" actually appears before attributes.
extern
(* optimize_power=1 *)
module ExternAttributeMultipleLines ();

// Module: ExternAttributeSingleLine
// Should still format the same as ExternAttributeMultipleLines.
extern (* optimize_power=1 *) module ExternAttributeSingleLine ();

// Module: ExternOnly
// If there are no attributes "extern" shouldn't be on its own line.
extern module ExternOnly ();  

// Module: ExternSpacing
extern(*optimize_power=0,mode="cla"*)(*no_value*)module ExternSpacing();
