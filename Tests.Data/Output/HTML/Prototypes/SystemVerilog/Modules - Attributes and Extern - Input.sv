
// Group: Attributes

// Module: NoValue
(* optimize_power *)
module NoValue ();
endmodule

// Module: WithValue
(* optimize_power=1 *)
module WithValue ();
endmodule

// Module: ManyValues
(* optimize_power = 0, mode = "cla",
    no_value, expr_value = (12 + 9) % 2,
	cond_value = x ? 1 : 0 *)
module ManyValues ();
endmodule

// Module: SeparateValues
(* optimize_power = 0 *)
(* mode = "cla" *)
(* no_value *)
(* expr_value = (12 + 9) % 2,
	cond_value = x ? 1 : 0 *)
module SeparateValues ();
endmodule

// Module: Spacing
(*optimize_power=0,mode="cla"*)(*no_value*)module Spacing();
endmodule


// Group: Extern

// Module: ExternAttributeMultipleLines
// "extern" actually appears before attributes.
extern
(* optimize_power=1 *)
module ExternAttributeMultipleLines ();
endmodule

// Module: ExternAttributeSingleLine
// Should still format the same as ExternAttributeMultipleLines.
extern (* optimize_power=1 *) module ExternAttributeSingleLine ();
endmodule

// Module: ExternOnly
// If there are no attributes "extern" shouldn't be on its own line.
extern module ExternOnly ();
endmodule

// Module: ExternSpacing
extern(*optimize_power=0,mode="cla"*)(*no_value*)module ExternSpacing();
endmodule
