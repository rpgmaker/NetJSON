﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("NetJSON")]
[assembly: AssemblyCompany("Phoenix Service Bus")]
[assembly: AssemblyProduct("NetJSON")]
[assembly: AssemblyCopyright("Copyright © Phoenix Service Bus 2016")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("c8619a98-1bd5-4bc9-af01-1e4a4f7bcdd3")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.2.1.9")]
[assembly: AssemblyFileVersion("1.2.1.9")]
#if !NET_35 && !NETSTANDARD1_6
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
#endif
[assembly: InternalsVisibleTo(NetJSON.NetJSON.NET_JSON_GENERATED_ASSEMBLY_NAME)]