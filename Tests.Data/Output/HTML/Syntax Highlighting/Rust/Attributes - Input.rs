
/* Topic: Inner Attributes
	_____________________________________________

	--- Code
	#[test]
	fn test_foo() {
		}

	#[cfg(target_os = "linux")]
	mod bar {
		}

	#[allow(non_camel_case_types)]
	type int8_t = i8;
	---
*/


/* Topic: Outer Attributes
	_____________________________________________

	--- Code
	#![crate_type = "lib"]

	fn some_unused_variables() {
		#![allow(unused_variables)]
		}
	---
*/


/* Topic: Multiple Attributes
	_____________________________________________

	--- Code
	#[macro_attr1]
	#[doc = mac!()]
	#[macro_attr2]
	#[derive(MacroDerive1, MacroDerive2)]
	fn foo() {
		}
	---
*/

