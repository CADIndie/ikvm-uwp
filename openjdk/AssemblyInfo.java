/*
  Copyright (C) 2006-2014 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/

@cli.System.Reflection.AssemblyCopyrightAttribute.Annotation(
    "This software is licensed under the GNU General Public License version 2 + \"Classpath\" exception.\r\n" +
    "See http://www.gnu.org/software/classpath/license.html for details.\r\n")

@cli.System.Reflection.AssemblyTitleAttribute.Annotation("IKVM.NET OpenJDK Library for .NET")
@cli.System.Reflection.AssemblyProductAttribute.Annotation("IKVM.NET")
@cli.System.Reflection.AssemblyCompanyAttribute.Annotation("Jeroen Frijters")
@cli.System.Reflection.AssemblyInformationalVersionAttribute.Annotation("8.0.5449.0")

@cli.System.Runtime.CompilerServices.InternalsVisibleToAttribute.Annotation.__Multiple({
    @cli.System.Runtime.CompilerServices.InternalsVisibleToAttribute.Annotation("IKVM.Runtime"),
    @cli.System.Runtime.CompilerServices.InternalsVisibleToAttribute.Annotation("IKVM.AWT.WinForms")
})

@cli.System.Security.AllowPartiallyTrustedCallersAttribute.Annotation

// type to collect Assembly attributes applicable to all core library assemblies
interface commonAttributes {}

/////////////////////////////////////////////////////////////////////////////

// Roslyn requires that assemblies that contain types that define extension methods have the attribute on the assembly as well
@cli.System.Runtime.CompilerServices.ExtensionAttribute.Annotation

// type to collect Assembly attributes specific to IKVM.OpenJDK.Core
interface coreAttributes {}
