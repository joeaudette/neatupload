using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web.UI;
using System.IO;

// Information about this assembly is defined by the following
// attributes.
//
// change them to the information which is associated with the assembly
// you compile.

[assembly: AssemblyTitle("NeatUpload")]
[assembly: AssemblyDescription("NeatUpload allows ASP.NET developers to stream uploaded files to disk and allows users to monitor upload progress")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Dean Brettle")]
[assembly: AssemblyProduct("NeatUpload")]
[assembly: AssemblyCopyright("Copyright 2005, 2006 Dean Brettle.  Licensed under the Lesser General Public License.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// The assembly version has following format :
//
// Major.Minor.Build.Revision
//
// You can specify all values by your own or you can build default build and revision
// numbers with the '*' character (the default):

[assembly: AssemblyVersion("1.3.*")]

[assembly: AssemblyInformationalVersion("trunk")]

// This helps with VS designer support.
[assembly: TagPrefix("Brettle.Web.NeatUpload", "Upload")]

// This makes it easier to link with code that require CLS compliance.
[assembly: CLSCompliant(true)]

[assembly: System.Security.AllowPartiallyTrustedCallers]

// To enable logging with log4net, add a reference to log4net and define USE_LOG4NET.
#if USE_LOG4NET
[assembly: log4net.Config.XmlConfigurator(ConfigFile="log4net.config", Watch=true)]
#endif

// Works around a Mono bug which fails to set this flag by default, causing the control
// to not work in a VB.NET Web Application project.
[assembly: AssemblyFlags(1 /* AssemblyNameFlags.PublicKey only exists in .NET 2.0 */)]