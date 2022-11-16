
/* Topic: Attributes

	--- code ---

	(* optimize_power *)
	module NoValue ();

	(* optimize_power=1 *)
	module WithValue ();

	(* expr_value = (12 + 9) % 2 *)
	module WithExpression ();

	(* optimize_power = 0 *)
	(* mode = "cla" *)
	(* no_value *)
	(* expr_value = (12 + 9) % 2 *)
	(* cond_value = x ? 1 : 0 *)
	module MultipleAttributeStatements ();

	(*optimize_power = 0*)
	(*mode = "cla"*)
	(*no_value*)
	(*expr_value = (12 + 9) % 2*)
	(*cond_value = x ? 1 : 0*)
	module MultipleTightAttributeStatements ();

	---
*/



/* Topic: Traps

	--- code ---

	PLI_VEXTERN PLI_DLLESPEC void (*vlog_startup_routines[])( void );

	PLI_INT32 (*cb_rtn)(struct t_cb_data *);

	---
*/