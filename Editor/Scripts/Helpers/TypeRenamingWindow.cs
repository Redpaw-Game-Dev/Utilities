using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LazyRedpaw.Utilities.Editor
{
    public class TypeRenamingWindow : EditorWindow
    {
        private static Regex _nameRegex = new ("^[A-Za-z_][A-Za-z0-9_]*$");
        
        [MenuItem("LazyRedpaw/Type Renaming")]
        private static void OpenWindow()
        {
            GetWindow<TypeRenamingWindow>(nameof(TypeRenamingWindow));
        }

        private void CreateGUI()
        {
            var typeFullNames = new List<string>();
            var allTypeFullNames = new List<string>();
            var allTypes = new List<Type>();
            var allScripts = new List<MonoScript>();
            var allPaths = new List<string>();
            var currentIndex = -1;
            var currentAssemblyName = string.Empty;
            var currentNamespace = string.Empty;
            var currentTypeName = string.Empty;
            var currentTypeFullName = string.Empty;
            var newTypeName = string.Empty;
            
            var toolbarContainer = new VisualElement();
            toolbarContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            
            var newNameTextFieldContainer = new VisualElement();
            newNameTextFieldContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            
            var errorLabel = new Label("The type with this name is existing already.");
            errorLabel.style.color = Color.red;
            errorLabel.Hide();
            
            var renameButton = new Button(RenameButtonClickedCallback) { text = "Rename" };
            renameButton.SetEnabled(false);
            
            var newNameTextField = new TextField("New Name");
            newNameTextField.style.flexGrow = new StyleFloat(1f);
            newNameTextField.RegisterValueChangedCallback(NewNameTextFieldValueChangedCallback);
            
            var toolbarPopup = new ToolbarPopupSearchField();
            toolbarPopup.style.flexGrow = new StyleFloat(1f);
            toolbarPopup.placeholderText = "Select a class to rename";
            toolbarPopup.RegisterValueChangedCallback(ToolbarValueChangedCallback);
            
            rootVisualElement.Add(toolbarContainer);
            rootVisualElement.Add(newNameTextFieldContainer);
            rootVisualElement.Add(errorLabel);
            rootVisualElement.Add(renameButton);
            newNameTextFieldContainer.Add(newNameTextField);
            toolbarContainer.Add(toolbarPopup);
                        
            var scriptPaths = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains("/Editor/") && !p.Contains("\\Editor\\"))
                .ToArray();
            
            for (var i = 0; i < scriptPaths.Length; i++)
            {
                var path = scriptPaths[i];
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;
                
                var type = script.GetClass();
                if (type != null && !type.IsInterface && !type.IsAbstract && 
                    (type.IsSerializable || typeof(MonoBehaviour).IsAssignableFrom(type) || typeof(ScriptableObject).IsAssignableFrom(type)))
                {
                    allPaths.Add(path);
                    allScripts.Add(script);
                    allTypes.Add(type);
                    typeFullNames.Add(type.FullName);
                    allTypeFullNames.Add(type.FullName);
                    toolbarPopup.menu.AppendAction(type.FullName, DropdownMenuActionCallback);
                }
            }

            void ToolbarValueChangedCallback(ChangeEvent<string> evt)
            {
                renameButton.SetEnabled(false);
                errorLabel.Hide();
                var newValue = evt.newValue;
                for (int i = 0; i < allTypes.Count; i++)
                {
                    var type = allTypes[i];
                    if (type.FullName.Contains(newValue))
                    {
                        bool isAllowed = true;
                        for (int j = 0; j < typeFullNames.Count; j++)
                        {
                            if (typeFullNames[j].Equals(type.FullName))
                            {
                                isAllowed = false;
                                break;
                            }
                        }
                        if (isAllowed)
                        {
                            typeFullNames.Add(type.FullName);
                            typeFullNames.Sort();
                            toolbarPopup.menu.AppendAction(type.FullName, DropdownMenuActionCallback);
                        }
                    }
                    else if (!type.FullName.Contains(newValue))
                    {
                        for (int j = 0; j < typeFullNames.Count; j++)
                        {
                            if (typeFullNames[j].Equals(type.FullName))
                            {
                                typeFullNames.RemoveAt(j);
                                toolbarPopup.menu.RemoveItemAt(j);
                                break;
                            }
                        }
                    }
                }
            }
            
            void DropdownMenuActionCallback(DropdownMenuAction action)
            {
                var toolbarPopupValue = action.name;
                toolbarPopup.value = toolbarPopupValue;
                newNameTextField.SetValueWithoutNotify(toolbarPopupValue.Split('.')[^1]);
                for (int i = 0; i < allTypes.Count; i++)
                {
                    var type = allTypes[i];
                    if (type.FullName.Equals(toolbarPopupValue))
                    {
                        currentIndex = i;
                        currentAssemblyName = type.Assembly.FullName.Split(',')[0];
                        currentNamespace = type.Namespace;
                        currentTypeName = type.Name;
                        currentTypeFullName = type.FullName;
                    }
                }
            }
            
            void RenameButtonClickedCallback()
            {
                var path = allPaths[currentIndex];
                var fullPath = Path.Combine(Application.dataPath, path.Replace("Assets\\", string.Empty)).Replace('\\', '/');
                
                UpdateTypeInstances(allScripts, currentIndex, currentNamespace, currentAssemblyName, currentTypeName, newTypeName, fullPath);
                UpdateTypeUsages(currentTypeName, newTypeName);

                AssetDatabase.RenameAsset(path, newTypeName);
                AssetDatabase.SaveAssets();
            }
            
            void NewNameTextFieldValueChangedCallback(ChangeEvent<string> evt)
            {
                if (!string.IsNullOrEmpty(evt.newValue) && !_nameRegex.IsMatch(evt.newValue))
                {
                    newNameTextField.SetValueWithoutNotify(evt.previousValue);
                }
                else
                {
                    newTypeName = evt.newValue;
                    var fullName = $"{currentNamespace}.{evt.newValue}";
                    if (allTypeFullNames.Contains(fullName))
                    {
                        if (!currentTypeFullName.Equals(fullName))
                        {
                            errorLabel.Show();
                        }
                        renameButton.SetEnabled(false);
                    }
                    else
                    {
                        errorLabel.Hide();
                        renameButton.SetEnabled(true);
                    }
                }
            }
        }

        private static void UpdateTypeInstances(List<MonoScript> allScripts, int currentIndex, string currentNamespace,
            string currentAssemblyName, string currentTypeName, string newTypeName, string fullPath)
        {
            var script = allScripts[currentIndex];
            var text = script.text;
                
            var usingLine = "using UnityEngine.Scripting.APIUpdating;\n";
            var attributeLine = $"    [MovedFrom(true, sourceNamespace: \"{currentNamespace}\", sourceAssembly: \"{currentAssemblyName}\", sourceClassName: \"{currentTypeName}\")]\n";
            
            text = text.Insert(0, usingLine);
            var insertIndex = text.IndexOf("public class", StringComparison.Ordinal);
            text = text.Insert(insertIndex, attributeLine);// Insert attribute
            text = text.Replace(currentTypeName, newTypeName);
                
            File.WriteAllText(fullPath, text);
            
            var pathContentMap = new Dictionary<string, string>();
            string[] readableAssetPaths = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories);
            foreach (string path in readableAssetPaths)
            {
                if (path.EndsWith(".meta")) continue;
                if (path.EndsWith(".prefab") ||
                    path.EndsWith(".asset") ||
                    path.EndsWith(".unity"))
                {
                    var assetFileText = File.ReadAllText(path);
                    assetFileText = Regex.Replace(assetFileText, @$"\b{currentTypeName}\b", $"{newTypeName}");
                    if (path.EndsWith(".prefab"))
                    {
                        pathContentMap.Add(path, assetFileText);
                    }
                    File.WriteAllText(path, assetFileText);
                }
            }
            AssetDatabase.Refresh();
            
            var allAssetPaths = AssetDatabase.FindAssets("").Select(AssetDatabase.GUIDToAssetPath).ToArray();
            AssetDatabase.ForceReserializeAssets(allAssetPaths);

            AssetDatabase.StartAssetEditing();
            foreach (var pathContentPair in pathContentMap)
            {
                File.WriteAllText(pathContentPair.Key, pathContentPair.Value);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
                
            var newAttributeLine = $"    [MovedFrom(true, sourceNamespace: \"{currentNamespace}\", sourceAssembly: \"{currentAssemblyName}\", sourceClassName: \"{newTypeName}\")]\n";
            text = text.Replace(newAttributeLine, string.Empty);// Remove attribute
            text = text.Replace(usingLine, string.Empty);
            File.WriteAllText(fullPath, text);
        }

        private void UpdateTypeUsages(string currentTypeName, string newTypeName)
        {
            string[] files = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileText = File.ReadAllText(file);
                fileText = Regex.Replace(fileText, @$"\b{currentTypeName}\b", $"{newTypeName}");
                File.WriteAllText(file, fileText);
            }
            AssetDatabase.Refresh();
        }
    }
}