
// Module: NoValue
(* optimize_power *)
module NoValue ();

// Module: WithValue
(* optimize_power=1 *)
module WithValue ();

// Module: WithExpression
(* expr_value = (12 + 9) % 2 *)
module WithExpression ();

// Module: MultipleAttributeBlock
(* optimize_power = 0, mode = "cla",
    no_value, expr_value = (12 + 9) % 2,
	cond_value = x ? 1 : 0 *)
module MultipleAttributeBlock ();

// Module: MultipleAttributeStatements
(* optimize_power = 0 *)
(* mode = "cla" *)
(* no_value *)
(* expr_value = (12 + 9) % 2 *)
(* cond_value = x ? 1 : 0 *)
module MultipleAttributeStatements ();
