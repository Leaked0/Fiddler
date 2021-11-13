using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[assembly: AssemblyVersion("5.0.1.0")]
[assembly: AssemblyTitle("FiddlerCore")]
[assembly: AssemblyDescription("Core HTTP(S) Proxy Engine")]
[assembly: AssemblyConfiguration("")]
[assembly: ComVisible(false)]
[assembly: InternalsVisibleTo("Fiddler.WebUi, PublicKey=002400000480000094000000060200000024000052534131000400000100010015b642a00317adef3dc33ab49f5b57e32b52e2ec3bd6c1d50ef7ce099ec1ee224afc32054cd3745cd663d9805cbd73982c6c70ad75c291b254b64b29ad8991bfae62321a1797bba5bbe8c68132292e519b025ae404354f79e180c4ba58561d676cb204a5da7fa1a96707ee081868a5b011d84f548e24045ae5e7f337b331e3ad")]
[assembly: Obfuscation(Feature = "ignore InternalsVisibleToAttribute", Exclude = false)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.Proxy: apply to member RefreshUpstreamGatewayInformation: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.CONFIG: apply to member UpstreamGateway: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.Session: apply to member SessionFieldChanged: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.Session: apply to member Execute: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.Session: apply to member SetBitFlag: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.Session: apply to member propagateProcessInfo: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.Session: apply to member ExecuteAsync: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.ResponderRule: apply to member IsEnabled: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.ResponderRule: apply to member _oResponseHeaders: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.ResponderRule: apply to member _arrResponseBodyBytes: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.ResponderRule: apply to member sMatch: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.ResponderRule: apply to member sAction: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.AutoResponder: apply to member PromoteRule: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.AutoResponder: apply to member DemoteRule: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type FiddlerCore.PlatformExtensions.PlatformExtensionsFactory: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type FiddlerCore.PlatformExtensions.PlatformExtensionsFactory: apply to member CreatePlatformExtensions: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type FiddlerCore.PlatformExtensions.API.IPlatformExtensions: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "Apply to type Fiddler.Utilities: apply to member HasHeaders: renaming", Exclude = true)]
[assembly: Obfuscation(Feature = "3. code control flow obfuscation", Exclude = true)]
[assembly: Obfuscation(Feature = "3. string encryption", Exclude = true)]
[assembly: Obfuscation(Feature = "3. debug", Exclude = false)]
[assembly: AssemblyProduct("Progress® Telerik® FiddlerCore")]
[assembly: AssemblyCompany("Progress Software Corporation")]
[assembly: AssemblyCopyright("Copyright ©  2010 - 2021 Progress Software Corporation and/or one of its subsidiaries or affiliates. All rights reserved.")]
