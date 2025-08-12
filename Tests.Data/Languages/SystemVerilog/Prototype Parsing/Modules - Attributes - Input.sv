
// Module: NoValue
(* optimize_power *)
module NoValue ();
endmodule

// Module: WithValue
(* optimize_power=1 *)
module WithValue ();
endmodule

// Module: WithExpression
(* expr_value = (12 + 9) % 2 *)
module WithExpression ();
endmodule

// Module: MultipleAttributeBlock
(* optimize_power = 0, mode = "cla",
    no_value, expr_value = (12 + 9) % 2,
	cond_value = x ? 1 : 0 *)
module MultipleAttributeBlock ();
endmodule

// Module: MultipleAttributeStatements
(* optimize_power = 0 *)
(* mode = "cla" *)
(* no_value *)
(* expr_value = (12 + 9) % 2 *)
(* cond_value = x ? 1 : 0 *)
module MultipleAttributeStatements ();
endmodule
