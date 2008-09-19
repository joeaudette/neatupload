using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web.UI;

// Information about this assembly is defined by the following
// attributes.
//
// change them to the information which is associated with the assembly
// you compile.

[assembly: AssemblyTitle("GreyBoxProgressBar")]
[assembly: AssemblyDescription("Extends NeatUpload with GreyBoxProgressBar control that displays progress in a GreyBox popup.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Dean Brettle")]
[assembly: AssemblyProduct("NeatUpload")]
[assembly: AssemblyCopyright("Copyright 2006-2008 Dean Brettle.  Licensed under the Lesser General Public License.")]
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

// The following attributes specify the key for the sign of your assembly. See the
// .NET Framework documentation for more information about signing.
// This is not required, if you don't want signing let these attributes like they're.
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile("")]

// This helps with VS designer support.
[assembly: TagPrefix("Brettle.Web.NeatUpload", "GreyBoxUpload")]

// This makes it easier to link with code that require CLS compliance.
[assembly: CLSCompliant(true)]

// To enable logging with log4net, add a reference to log4net and define USE_LOG4NET.
#if USE_LOG4NET
[assembly: log4net.Config.XmlConfigurator(ConfigFile="log4net.config", Watch=true)]
#endif

