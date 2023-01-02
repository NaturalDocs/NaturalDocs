
// Module: ImportPackage
module ImportPackage
	import PackageName::*;
	();

// Module: ImportMember
module ImportMember
	import PackageName::MemberName;
	();

// Module: ImportMultiple1
module ImportMultiple1
	import PackageA::*, PackageB::MemberName;
	();

// Module: ImportMultiple2
module ImportMultiple2
	import PackageA::*;
	import PackageB::MemberName;
	();

// Module: ImportMultiple3
module ImportMultiple3
	import PackageA::*, PackageB::MemberName;
	import PackageC::*;
	();
