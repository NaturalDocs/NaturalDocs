
/* Topic: Simple Attributes
	_____________________________________________

	--- Code
	[[AttributeA]]
	[[AttributeB()]]
	[[AttributeC(12, "string")]]
	---
*/


/* Topic: Attributes with Namespaces
	_____________________________________________

	--- Code
	[[Namespace::AttributeA]]
	[[Namespace::AttributeB()]]
	[[Namespace::AttributeC(12, "string")]]
	---
*/


/* Topic: Attributes in Lists
	_____________________________________________

	--- Code
	[[AttributeA, AttributeB(), AttributeC(12, "string")]]
	[[Namespace::AttributeA, Namespace::AttributeB(), Namespace::AttributeC(12, "string")]]
	---
*/


/* Topic: Attributes with Using
	_____________________________________________

	--- Code
	[[using Namespace: AttributeA]]
	[[using Namespace: AttributeA, AttributeB(), AttributeC(12, "string")]]
	---
*/
