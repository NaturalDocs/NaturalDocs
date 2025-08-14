
// Topic: Block Comments
//
//		--- Code
//		string // comment
//
//		string { comment } string
//
//		string (* comment *) string
//		---

// Topic: Nested Comments
//
//		--- Code
//		string { comment { comment } still comment } string
//
//		string (* comment (* comment *) still comment *) string
//
//		string { comment (* comment *) still comment } string
//
//		string (* comment { comment } still comment *) string
//
//		string { comment (* comment *) still comment *) still comment } string
//
//		string (* comment { comment } still comment } still comment *) string
//		---
