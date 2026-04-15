
// Function: Parameters
fn Parameters (a: i32,
					   b: &i32,
					   c: &mut i32,
					   d: [i32; 5],
					   e: &[i32; 5],
					   f: &mut [i32; 5]);

// Function: Lifetimes
fn Lifetimes<'a> (a: &'a i32,
						  b: &'a mut i32,
						  c: &'a [i32; 5],
						  d: &'a mut [i32; 5]);
