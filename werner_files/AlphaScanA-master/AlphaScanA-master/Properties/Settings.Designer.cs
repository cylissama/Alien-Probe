﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AlphaScan.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.8.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Permitzones.xml")]
        public string Filename {
            get {
                return ((string)(this["Filename"]));
            }
            set {
                this["Filename"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string BlacklistFile {
            get {
                return ((string)(this["BlacklistFile"]));
            }
            set {
                this["BlacklistFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string IgnoreListFile {
            get {
                return ((string)(this["IgnoreListFile"]));
            }
            set {
                this["IgnoreListFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("tmr://pts-ticket-1.dyn.wku.edu")]
        public string ReaderURIs {
            get {
                return ((string)(this["ReaderURIs"]));
            }
            set {
                this["ReaderURIs"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("tmr://pts-ticket-1.dyn.wku.edu")]
        public string ReaderURI {
            get {
                return ((string)(this["ReaderURI"]));
            }
            set {
                this["ReaderURI"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string CamSettingsList {
            get {
                return ((string)(this["CamSettingsList"]));
            }
            set {
                this["CamSettingsList"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool NoReaderMode {
            get {
                return ((bool)(this["NoReaderMode"]));
            }
            set {
                this["NoReaderMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("333")]
        public int AlienServerPort {
            get {
                return ((int)(this["AlienServerPort"]));
            }
            set {
                this["AlienServerPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("${TAGID},${DATE1},${TIME1},${MSEC2}${DATE2},${TIME2},${MSEC2},${COUNT},${TX},${RX" +
            "},${PROTO#},${PROTO},${PCWORD},${RSSI},${RSSI_MAX},${NAME},${HOST},${SPEED},${SP" +
            "EED_MIN},${SPEED_MAXs},${SPEED_TOP},${DIR}")]
        public string TagFormat {
            get {
                return ((string)(this["TagFormat"]));
            }
            set {
                this["TagFormat"] = value;
            }
        }
    }
}
