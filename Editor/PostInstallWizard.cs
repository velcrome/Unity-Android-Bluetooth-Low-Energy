using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace Android.BLE.UnityEditor 
{

    public class PostInstallWizard : EditorWindow
    {
        const string packageName = "com.velorexe.androidbluetoothlowenergy";
        static string assetsAndroidManifestPath;
        static string packageAndroidManifestPath = Path.GetFullPath($"Packages/{packageName}/Plugins/Android/AndroidManifest.xml");
        static bool hasExistingManifest;
        static string existingManifestContents;

        readonly string tickPath = $"Packages/{packageName}/icons/2705.png";
        readonly string crossPath = $"Packages/{packageName}/icons/274c.png";

        Texture2D m_tickImage;
        Texture2D m_crossImage;

        private class AndroidPermission {
            public Regex regex;
            public string description;
            public string line;

            public bool foldOutOpen;

            public AndroidSdkVersions minSdkVersion;

            public bool Exists(string manifest){
                return regex.IsMatch(manifest);
            }
        }

        AndroidPermission[] m_androidPermission = new AndroidPermission[]{
            new AndroidPermission(){
                regex = new Regex("uses-permission[^>]+android:name=\"android.permission.BLUETOOTH\""),
                description = "Legacy Bluetooth access ( pre API 31 )",
                line = "<uses-permission android:name=\"android.permission.BLUETOOTH\" android:maxSdkVersion=\"30\" />",            
            },
            new AndroidPermission(){
                regex = new Regex("uses-permission[^>]+android:name=\"android.permission.BLUETOOTH_ADMIN\""),
                description = "Legacy Bluetooth Admin access ( pre API 31 )",
                line = "<uses-permission android:name=\"android.permission.BLUETOOTH_ADMIN\" android:maxSdkVersion=\"30\" />",
            },
            new AndroidPermission(){
                regex = new Regex("uses-permission[^>]+android:name=\"android.permission.BLUETOOTH_SCAN\""),
                description = "Bluetooth Scan ( API 31 and above )",
                line = "<uses-permission android:name=\"android.permission.BLUETOOTH_SCAN\" android:usesPermissionFlags=\"neverForLocation\" />",
                minSdkVersion = AndroidSdkVersions.AndroidApiLevel31
            },
            new AndroidPermission(){
                regex = new Regex("uses-permission[^>]+android:name=\"android.permission.BLUETOOTH_CONNECT\""),
                description = "Bluetooth Connect ( API 31 and above )",
                line = "<uses-permission android:name=\"android.permission.BLUETOOTH_CONNECT\" />",
                minSdkVersion = AndroidSdkVersions.AndroidApiLevel31
            },
        };
        

        [MenuItem("Window/Android Bluetooth Low Energy Library")]
        public static void OpenWindow()
        {
            var window = GetWindow<PostInstallWizard>(false,"BLE Library Configuration Wizard");    
            window.Show();
        }

        public PostInstallWizard():base()
        {
            RefreshFiles();
        }

        
        public void Awake()
        {
            RefreshFiles();
            EditorApplication.Beep();

            Debug.Log(PlayerSettings.Android.targetSdkVersion);
            
            Debug.Log(PlayerSettings.Android.minSdkVersion.ToString());
            
            GUIHyperlinkClick();

            m_tickImage = AssetDatabase.LoadAssetAtPath<Texture2D>( tickPath );
            m_crossImage = AssetDatabase.LoadAssetAtPath<Texture2D>( crossPath );
        }

#if UNITY_2020_1_OR_NEWER
        void GUIHyperlinkClick()
        {
            EditorGUI.hyperLinkClicked += OnHyperLinkClicked;
        }

        void OnDestroy()
        {
            EditorGUI.hyperLinkClicked -= OnHyperLinkClicked;
        }

        private void OnHyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
        {
            if(window!=this){
                return;
            }
            Application.OpenURL( args.hyperLinkData["href"] );
        }
#else
        void GUIHyperlinkClick()
        {
            //not availble before 2020.1
        }
#endif

        public static void RefreshFiles(){
            assetsAndroidManifestPath = Path.Join(Application.dataPath,"Plugins/Android/AndroidManifest.xml");
            
            hasExistingManifest = File.Exists(assetsAndroidManifestPath);
            if(hasExistingManifest){
                existingManifestContents = File.ReadAllText(assetsAndroidManifestPath);
            }
            else {
                Debug.LogWarning($"File not found {assetsAndroidManifestPath}");
            }

        }

        public void OnGUI()
        {
            var height = EditorGUIUtility.singleLineHeight;
            GUIStyle style = new GUIStyle( GUI.skin.label ) { richText = true };
            GUIStyle bold = new GUIStyle(style){ fontStyle = FontStyle.Bold };
                    
            if( hasExistingManifest ){
                EditorGUILayout.LabelField("AndroidManifest.xml file found",bold);
                EditorGUILayout.LabelField("Manifest Bluetooth permissions:");
                foreach(var androidPermission in m_androidPermission){
                    if(PlayerSettings.Android.targetSdkVersion<androidPermission.minSdkVersion){
                        continue;
                    }
                    bool hasPerm = androidPermission.Exists(existingManifestContents);
                    EditorGUILayout.GetControlRect(GUILayout.Height(4));             
                    EditorGUILayout.BeginHorizontal();
                    //var rect = EditorGUILayout.GetControlRect(GUILayout.Width(height));
                    androidPermission.foldOutOpen = EditorGUILayout.Foldout(androidPermission.foldOutOpen, $"{androidPermission.description}");
                    //EditorGUILayout.LabelField($"{androidPermission.description} ",style);
                    var rect = EditorGUILayout.GetControlRect(GUILayout.Width(height-4));
                    rect.height = height-4;
                    rect.y+=2;
                    GUI.DrawTexture(rect,hasPerm ? m_tickImage : m_crossImage);
                    EditorGUILayout.EndHorizontal();
                    if(androidPermission.foldOutOpen){
                        EditorGUILayout.TextField(androidPermission.line);
                    }
                }
                
                //existingManifestContents 
            }
            else 
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("No existing AndroidManifest.xml file found",bold);
                
                if(GUILayout.Button("Use template AndroidManifest.xml")){
                    File.Copy(packageAndroidManifestPath,assetsAndroidManifestPath);
                    RefreshFiles();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.GetControlRect();
            EditorGUILayout.TextField("Click here <a href=\"https://developer.android.com/develop/connectivity/bluetooth/bt-permissions\">more information on bluetooth permissions</a>", style);

            

            
        }


    }
}
